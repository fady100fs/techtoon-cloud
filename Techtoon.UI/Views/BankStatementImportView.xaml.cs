using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Npgsql;
using UglyToad.PdfPig;
using Techtoon.Core.Data;
using Techtoon.UI.Services;

namespace Techtoon.UI.Views
{
    // ============================================================
    // استيراد كشوف البنك (PDF) — تحميل الملف داخل البرنامج
    // ============================================================
    public class BankStatementLine
    {
        public int LineNumber { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Description { get; set; } = "";
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Balance { get; set; }
        public string MatchStatus { get; set; } = "unmatched";
        public int? MatchedPaymentId { get; set; }
        public string DateDisplay => TransactionDate.ToString("yyyy-MM-dd");
        public string DebitDisplay => Debit.ToString("N2");
        public string CreditDisplay => Credit.ToString("N2");
        public string BalanceDisplay => Balance.ToString("N2");
        public string MatchDisplay => MatchStatus switch
        {
            "matched" => "مُربط",
            "partial" => "مرشّح",
            _ => "غير مُربط"
        };
    }

    public class BankStatementImportParser
    {
        /// <summary>يستخرج النص من كل صفحات ملف PDF.</summary>
        public static string ExtractText(string path)
        {
            var sb = new StringBuilder();
            using var doc = PdfDocument.Open(path);
            foreach (var page in doc.GetPages())
                sb.AppendLine(page.Text);
            return sb.ToString();
        }

        private static readonly string[] DatePatterns =
        {
            "dd/MM/yyyy", "dd-MM-yyyy", "yyyy/MM/dd", "yyyy-MM-dd", "dd.MM.yyyy", "dd/MM/yy"
        };

        /// <summary>
        /// يحوّل أسطر نص الكشف إلى حركات (تفحص صيغ تواريخ + مبالغ).
        /// يتعامل مع أعمدة مدين/دائن أو مبلغ موقّع.
        /// </summary>
        public static List<BankStatementLine> Parse(string text)
        {
            var list = new List<BankStatementLine>();
            int lineNo = 0;
            foreach (var raw in (text ?? "").Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length < 8) continue;
                lineNo++;
                var date = FindDate(line);
                if (date == null) continue;
                var numbers = FindNumbers(line);
                if (numbers.Count == 0) continue;

                decimal debit = 0, credit = 0, balance = 0;
                if (numbers.Count == 1)
                {
                    var v = numbers[0];
                    if (v < 0) { debit = Math.Abs(v); }
                    else credit = v;
                }
                else if (numbers.Count >= 3)
                {
                    // الأعمدة الثلاثة الأخيرة: مدين / دائن / رصيد
                    debit = numbers[numbers.Count - 3];
                    credit = numbers[numbers.Count - 2];
                    balance = numbers[numbers.Count - 1];
                    if (debit < 0) debit = 0;
                    if (credit < 0) credit = 0;
                }
                else
                {
                    debit = numbers[0];
                    credit = numbers[1];
                }
                if (debit == 0 && credit == 0) continue;

                var desc = RemoveDateAndNumbers(line);
                list.Add(new BankStatementLine
                {
                    LineNumber = lineNo,
                    TransactionDate = date.Value,
                    Description = desc.Length > 180 ? desc[..180] : desc,
                    Debit = debit,
                    Credit = credit,
                    Balance = balance
                });
            }
            return list;
        }

        private static DateTime? FindDate(string line)
        {
            foreach (var pat in DatePatterns)
            {
                int idx = 0;
                while (idx < line.Length)
                {
                    int start = line.IndexOfAny(new[] { '/', '-', '.' }, idx);
                    if (start < 0) break;
                    // نبحث عن أرقام محاطة بفواصل التاريخ
                    var m = System.Text.RegularExpressions.Regex.Match(line.Substring(idx), @"\d{1,4}[./-]\d{1,2}[./-]\d{1,4}");
                    if (m.Success)
                    {
                        var s = m.Value;
                        foreach (var p in DatePatterns)
                            if (DateTime.TryParseExact(s, p, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                                return dt;
                        idx += m.Index + m.Length;
                        continue;
                    }
                    break;
                }
                break;
            }
            return null;
        }

        private static List<decimal> FindNumbers(string line)
        {
            var list = new List<decimal>();
            var matches = System.Text.RegularExpressions.Regex.Matches(line, @"-?\d{1,3}(?:[,\s]\d{3})*(?:\.\d{1,2})?");
            foreach (System.Text.RegularExpressions.Match m in matches)
            {
                var s = m.Value.Replace(" ", "").Replace(",", "");
                if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                    if (Math.Abs(v) >= 0.01m) list.Add(v);
            }
            return list;
        }

        private static string RemoveDateAndNumbers(string line)
        {
            var s = System.Text.RegularExpressions.Regex.Replace(line, @"\d{1,4}[./-]\d{1,2}[./-]\d{1,4}", " ");
            s = System.Text.RegularExpressions.Regex.Replace(s, @"-?\d{1,3}(?:[,\s]\d{3})*(?:\.\d{1,2})?", " ");
            s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ").Trim();
            return s;
        }
    }

    /// <summary>صفحة استيراد كشف البنك: رفع PDF → معاينة → استيراد → ربط تلقائي.</summary>
    public partial class BankStatementImportView : UserControl
    {
        private string _pdfPath = "";
        private string _statementNumber = "";
        private readonly ObservableCollection<BankStatementLine> _lines = new();

        public BankStatementImportView()
        {
            InitializeComponent();
            DataContext = this;
            LinesGrid.ItemsSource = _lines;
            BtnBrowse.Click += BtnBrowse_Click;
            BtnImport.Click += async (s, e) => await ImportAsync();
            BtnMatch.Click += async (s, e) => await AutoMatchAsync();
            LoadCashBoxes();
        }

        private void LoadCashBoxes()
        {
            try
            {
                using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
                conn.Open();
                CashBoxCombo.Items.Clear();
                using var cmd = new NpgsqlCommand("SELECT id, name FROM cash_boxes WHERE COALESCE(is_active,TRUE)=TRUE ORDER BY name", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read()) CashBoxCombo.Items.Add(new ComboItem { Id = r.GetInt32(0), Name = r.GetString(1) });
                if (CashBoxCombo.Items.Count > 0) CashBoxCombo.SelectedIndex = 0;
            }
            catch { }
        }

        private async void BtnBrowse_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "PDF files (*.pdf)|*.pdf", Title = "اختر كشف حساب البنك" };
            if (dlg.ShowDialog() != true) return;
            _pdfPath = dlg.FileName;
            FileLabel.Text = Path.GetFileName(_pdfPath);
            Status.Text = "جارٍ استخراج النص من الملف...";
            try
            {
                var text = await Task.Run(() => BankStatementImportParser.ExtractText(_pdfPath));
                _lines.Clear();
                foreach (var l in await Task.Run(() => BankStatementImportParser.Parse(text))) _lines.Add(l);
                Status.Text = "تم استخراج " + _lines.Count + " حركة — راجع المعاينة ثم اضغط «استيراد»";
                if (string.IsNullOrEmpty(_statementNumber))
                    _statementNumber = "STMT-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            }
            catch (Exception ex) { Status.Text = "فشل قراءة الملف: " + ex.Message; }
        }

        private async Task ImportAsync()
        {
            if (_lines.Count == 0) { Status.Text = "لا توجد حركات للاستيراد."; return; }
            if (CashBoxCombo.SelectedItem is not ComboItem cb) { Status.Text = "اختر الخزينة/الحساب البنكي (إلزامي)."; return; }
            if (!decimal.TryParse(OpeningBox.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var opening)) opening = 0;
            if (!decimal.TryParse(ClosingBox.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var closing)) closing = 0;
            try
            {
                Status.Text = "جارٍ الاستيراد...";
                await Task.Run(() =>
                {
                    var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TechtoonAccounting", "BankStatements");
                    Directory.CreateDirectory(dir);
                    var saved = Path.Combine(dir, Path.GetFileName(_pdfPath));
                    File.Copy(_pdfPath, saved, true);
                    var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(saved))).ToLowerInvariant();

                    using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
                    conn.Open();
                    using var tx = conn.BeginTransaction();
                    int statementId;
                    using (var ins = new NpgsqlCommand(@"INSERT INTO bank_statements
                        (statement_number, cash_box_id, statement_date, period_start, period_end, opening_balance, closing_balance,
                         currency, source_file_path, source_file_format, source_file_size, file_hash, imported_by, imported_at, status)
                        VALUES (@num, @cb, CAST(@sd AS date), CAST(@ps AS date), CAST(@pe AS date), @ob, @cb2, 'EGP', @path, 'pdf', @size, @hash, 1, NOW(), 'imported') RETURNING id", conn, tx))
                    {
                        ins.Parameters.AddWithValue("@num", _statementNumber);
                        ins.Parameters.AddWithValue("@cb", cb.Id);
                        ins.Parameters.AddWithValue("@sd", _lines.Max(l => l.TransactionDate));
                        ins.Parameters.AddWithValue("@ps", _lines.Min(l => l.TransactionDate));
                        ins.Parameters.AddWithValue("@pe", _lines.Max(l => l.TransactionDate));
                        ins.Parameters.AddWithValue("@ob", opening);
                        ins.Parameters.AddWithValue("@cb2", closing);
                        ins.Parameters.AddWithValue("@path", saved);
                        ins.Parameters.AddWithValue("@size", new FileInfo(saved).Length);
                        ins.Parameters.AddWithValue("@hash", hash);
                        statementId = Convert.ToInt32(ins.ExecuteScalar());
                    }
                    int n = 0;
                    foreach (var l in _lines)
                    {
                        using var ins = new NpgsqlCommand(@"INSERT INTO bank_statement_lines
                            (statement_id, line_number, transaction_date, value_date, description, debit, credit, balance, match_status)
                            VALUES (@sid, @ln, CAST(@d AS date), CAST(@vd AS date), @desc, @dr, @cr, @bal, 'unmatched')", conn, tx);
                        ins.Parameters.AddWithValue("@sid", statementId);
                        ins.Parameters.AddWithValue("@ln", l.LineNumber);
                        ins.Parameters.AddWithValue("@d", l.TransactionDate);
                        ins.Parameters.AddWithValue("@vd", l.TransactionDate);
                        ins.Parameters.AddWithValue("@desc", l.Description);
                        ins.Parameters.AddWithValue("@dr", l.Debit);
                        ins.Parameters.AddWithValue("@cr", l.Credit);
                        ins.Parameters.AddWithValue("@bal", l.Balance);
                        ins.ExecuteNonQuery();
                        n++;
                    }
                    tx.Commit();
                    AuditService.Log("IMPORT", "BankStatement", statementId, $"Imported {n} lines from {Path.GetFileName(_pdfPath)}");
                });
                Status.Text = "تم استيراد " + _lines.Count + " حركة وحفظ الملف داخل البرنامج";
            }
            catch (Exception ex) { Status.Text = "فشل الاستيراد: " + ex.Message; }
        }

        /// <summary>ربط تلقائي: مطابقة المبلغ والتاريخ مع سندات القبض/الدفع.</summary>
        private async Task AutoMatchAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
                    conn.Open();
                    int matched = 0;
                    foreach (var l in _lines)
                    {
                        var amount = l.Debit > 0 ? l.Debit : l.Credit;
                        var paymentType = l.Debit > 0 ? "payment" : "receipt";
                        using var cmd = new NpgsqlCommand(@"
                            UPDATE bank_statement_lines SET match_status='matched', matched_payment_id=@pid,
                                matched_at=NOW(), match_confidence=90
                            WHERE id = (SELECT bl.id FROM bank_statement_lines bl
                                        JOIN bank_statements bs ON bs.id = bl.statement_id
                                        JOIN payments p ON p.amount = @amt
                                        WHERE bs.cash_box_id = @cb AND bl.transaction_date = CAST(@d AS date)
                                          AND ((bl.credit > 0 AND p.payment_type='receipt') OR (bl.debit > 0 AND p.payment_type='payment'))
                                          AND bl.match_status='unmatched'
                                        ORDER BY p.id DESC LIMIT 1)", conn);
                        cmd.Parameters.AddWithValue("@pid", 0);
                        cmd.Parameters.AddWithValue("@amt", amount);
                        cmd.Parameters.AddWithValue("@d", l.TransactionDate);
                        cmd.Parameters.AddWithValue("@cb", (CashBoxCombo.SelectedItem as ComboItem)?.Id ?? 0);
                        // نحدّث أولاً بمعرّف السند المطابَق عبر استعلام فرعي
                        cmd.CommandText = @"
                            UPDATE bank_statement_lines bl SET match_status='matched', matched_payment_id=p.id, matched_at=NOW(), match_confidence=90
                            FROM payments p
                            JOIN bank_statements bs ON bs.id = bl.statement_id
                            WHERE bs.cash_box_id = @cb AND bl.transaction_date = CAST(@d AS date)
                              AND p.amount = @amt
                              AND ((bl.credit > 0 AND p.payment_type='receipt') OR (bl.debit > 0 AND p.payment_type='payment'))
                              AND bl.match_status='unmatched'
                              AND p.id = (SELECT p2.id FROM payments p2 WHERE p2.amount=@amt AND p2.cash_box_id=@cb
                                          AND ((p2.payment_type='receipt' AND bl.credit>0) OR (p2.payment_type='payment' AND bl.debit>0))
                                          ORDER BY p2.id DESC LIMIT 1)";
                        cmd.ExecuteNonQuery();
                    }
                });
                Status.Text = "تم تنفيذ الربط التلقائي (طابق المبلغ والتاريخ)";
            }
            catch (Exception ex) { Status.Text = "فشل الربط: " + ex.Message; }
        }
    }
}
