using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Npgsql;

namespace Techtoon.UI.Pages
{
    public class PaymentRow
    {
        public long Id { get; set; }
        public string Voucher { get; set; }
        public string DateStr { get; set; }
        public DateTime? DateVal { get; set; }
        public string Party { get; set; }
        public string Type { get; set; }
        public decimal? AmountVal { get; set; }
        public string AmountStr { get; set; }
        public string Currency { get; set; }
        public string Method { get; set; }
        public long? TreasuryId { get; set; }
        public string Treasury { get; set; }
        public string Notes { get; set; }
    }

    
    public class InvoiceOption
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; }
        public decimal Total { get; set; }
        public decimal Paid { get; set; }
        public decimal Remaining { get { return Total - Paid; } }
        public string PartyName { get; set; }
        public string Type { get; set; }
        public string Display
        {
            get
            {
                string t = (Type == "sales") ? "مبيعات" : (Type == "purchase" ? "مشتريات" : Type);
                return InvoiceNumber + " | " + t + " | " + PartyName + " | متبقي: " + Remaining.ToString("N2");
            }
        }
    }

public partial class PaymentsPage : UserControl
    {
        private string _tbl;
        private List<string> _cols = new List<string>();
        private string _treasuryCol;
        private string _fkTable;
        private string _fkNameCol;
        private Dictionary<long, string> _names = new Dictionary<long, string>();
        private List<PaymentRow> _rows = new List<PaymentRow>();

        private static readonly string[] TableCandidates = new[] { "payments", "payments_old", "old_payments", "payments_backup", "payment" };
        private static readonly string[] TreasuryCandidates = new[] { "account_id", "accountid", "cash_box_id", "cashbox_id", "treasury_id", "safe_id", "box_id", "cash_account_id", "account", "treasury", "cash_box", "box" };

        private const string CS_FALLBACK = "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=npg_K9nHVkY5OjRC;Database=neondb;SSL Mode=Require;Trust Server Certificate=true;Pooling=true;Timeout=15;CommandTimeout=60";
        private static string _cs;

        public PaymentsPage()
        {
            InitializeComponent();
            TypeBox.SelectedIndex = 0;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try { await ReloadAsync(); }
            catch (Exception ex) { ShowError("تحميل الصفحة", ex); }
        }

        private async Task ReloadAsync()
        {
            SchemaText.Foreground = Brushes.DimGray;
            SchemaText.Text = "جارٍ التحميل من قاعدة البيانات...";
            var res = await Task.Run(() => LoadCore());
            _tbl = res.Table;
            _cols = res.Cols ?? new List<string>();
            _treasuryCol = res.TreasuryCol;
            _fkTable = res.FkTable;
            _fkNameCol = res.FkNameCol;
            _names = res.Names ?? new Dictionary<long, string>();
            _rows = res.Rows ?? new List<PaymentRow>();
            ApplyFilter();
            SchemaText.Text = "الجدول: " + _tbl + "  •  الأعمدة: " + _cols.Count + "  •  عمود الخزينة: " + (_treasuryCol ?? "غير موجود") + (_fkTable != null ? "  •  جدول الخزينة: " + _fkTable + " (" + (_fkNameCol ?? "?") + ")" : "") + "  •  عدد السجلات: " + _rows.Count;
        }

        private static LoadResult LoadCore()
        {
            var res = new LoadResult();
            using (var con = new NpgsqlConnection(GetConnectionString()))
            {
                con.Open();
                res.Table = FindTable(con);
                if (res.Table == null)
                    throw new InvalidOperationException("لم يتم العثور على جدول للمدفوعات (جربت: " + string.Join(", ", TableCandidates) + ")");
                res.Cols = GetColumns(con, res.Table);
                if (res.Cols.Count == 0)
                    throw new InvalidOperationException("الجدول " + res.Table + " بلا أعمدة");
                res.TreasuryCol = Pick(res.Cols, TreasuryCandidates);
                if (res.TreasuryCol != null)
                {
                    var fk = LoadFk(con, res.Table, res.TreasuryCol);
                    res.FkTable = fk.Item1;
                    res.FkKeyCol = fk.Item2;
                    if (res.FkTable != null)
                    {
                        var lkCols = GetColumns(con, res.FkTable);
                        res.FkNameCol = Pick(lkCols, "name", "box_name", "account_name", "treasury_name", "cashbox_name", "cash_box_name", "safe_name", "title", "label", "display_name", "name_ar", "arabic_name", "description");
                    }
                }
                var names = new Dictionary<long, string>();
                if (res.FkTable != null && res.FkKeyCol != null && res.FkNameCol != null)
                {
                    using (var c = new NpgsqlCommand("select " + Q(res.FkKeyCol) + ", " + Q(res.FkNameCol) + " from " + Q(res.FkTable), con))
                    using (var r = c.ExecuteReader())
                        while (r.Read())
                        {
                            if (r.IsDBNull(0)) continue;
                            var k = ToLong(Convert.ToString(r.GetValue(0), CultureInfo.InvariantCulture));
                            if (!k.HasValue || names.ContainsKey(k.Value)) continue;
                            names[k.Value] = r.IsDBNull(1) ? ("#" + k.Value) : Convert.ToString(r.GetValue(1), CultureInfo.InvariantCulture);
                        }
                }
                res.Names = names;
                res.Rows = new List<PaymentRow>();
                using (var c = new NpgsqlCommand("select to_jsonb(t)::text from " + Q(res.Table) + " t", con))
                using (var r = c.ExecuteReader())
                    while (r.Read())
                    {
                        if (r.IsDBNull(0)) continue;
                        var json = r.GetString(0);
                        if (string.IsNullOrWhiteSpace(json)) continue;
                        try
                        {
                            using (var doc = JsonDocument.Parse(json))
                                res.Rows.Add(MapRow(doc.RootElement, res));
                        }
                        catch { }
                    }
            }
            return res;
        }

        private class LoadResult
        {
            public string Table;
            public List<string> Cols;
            public string TreasuryCol;
            public string FkTable;
            public string FkKeyCol;
            public string FkNameCol;
            public Dictionary<long, string> Names;
            public List<PaymentRow> Rows;
        }

        private static string FindTable(NpgsqlConnection con)
        {
            foreach (var t in TableCandidates)
                if (TableExists(con, t)) return t;
            using (var c = new NpgsqlCommand("select table_name from information_schema.tables where table_schema = 'public' and lower(table_name) like '%pay%' order by case when lower(table_name) like '%payment%' then 0 else 1 end, table_name", con))
            using (var r = c.ExecuteReader())
                if (r.Read() && !r.IsDBNull(0)) return r.GetString(0);
            return null;
        }

        private static bool TableExists(NpgsqlConnection con, string t)
        {
            using (var c = new NpgsqlCommand("select count(*)::int from information_schema.tables where table_schema = 'public' and lower(table_name) = lower(@t)", con))
            {
                c.Parameters.AddWithValue("@t", t);
                return Convert.ToInt64(c.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
            }
        }

        private static List<string> GetColumns(NpgsqlConnection con, string t)
        {
            var l = new List<string>();
            using (var c = new NpgsqlCommand("select column_name from information_schema.columns where table_schema = 'public' and lower(table_name) = lower(@t) order by ordinal_position", con))
            {
                c.Parameters.AddWithValue("@t", t);
                using (var r = c.ExecuteReader())
                    while (r.Read()) l.Add(r.GetString(0));
            }
            return l;
        }

        private static Tuple<string, string> LoadFk(NpgsqlConnection con, string table, string col)
        {
            var sql = "select ccu.table_name, ccu.column_name from information_schema.table_constraints tc join information_schema.key_column_usage kcu on tc.constraint_name = kcu.constraint_name and tc.table_schema = kcu.table_schema join information_schema.constraint_column_usage ccu on ccu.constraint_name = tc.constraint_name and ccu.table_schema = tc.table_schema where tc.constraint_type = 'FOREIGN KEY' and tc.table_schema = 'public' and lower(tc.table_name) = lower(@t) and kcu.column_name = @c limit 1";
            using (var c = new NpgsqlCommand(sql, con))
            {
                c.Parameters.AddWithValue("@t", table);
                c.Parameters.AddWithValue("@c", col);
                using (var r = c.ExecuteReader())
                    if (r.Read() && !r.IsDBNull(0)) return Tuple.Create(r.GetString(0), r.GetString(1));
            }
            return Tuple.Create<string, string>(null, null);
        }

        private static string Pick(List<string> cols, params string[] cands)
        {
            if (cols == null) return null;
            foreach (var cd in cands)
            {
                var hit = cols.FirstOrDefault(c => string.Equals(c, cd, StringComparison.OrdinalIgnoreCase));
                if (hit != null) return hit;
            }
            return null;
        }

        private static PaymentRow MapRow(JsonElement e, LoadResult res)
        {
            var row = new PaymentRow();
            row.Id = ToLong(GetF(e, "id", "payment_id", "paymentid", "sid", "sandid")) ?? 0;
            row.Voucher = GetF(e, "voucher_no", "voucher", "voucherno", "receipt_no", "receiptno", "document_no", "doc_no", "no", "number", "code");
            row.DateVal = ToDate(GetF(e, "date", "payment_date", "paymentdate", "doc_date", "docdate", "created_at", "createdat", "trx_date", "date_paid"));
            row.DateStr = row.DateVal.HasValue ? row.DateVal.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : null;
            row.Party = GetF(e, "party_name", "party", "customer_name", "customer", "client_name", "client", "beneficiary", "person_name", "person", "supplier", "supplier_name", "employee_name", "name", "description");
            row.Type = NormalizeType(GetF(e, "type", "payment_type", "paymenttype", "kind", "direction", "trx_type", "transaction_type"));
            row.AmountVal = ToDec(GetF(e, "amount", "amount_paid", "amountpaid", "payment_amount", "paid", "value", "total", "total_amount"));
            row.AmountStr = row.AmountVal.HasValue ? row.AmountVal.Value.ToString("N2", CultureInfo.InvariantCulture) : null;
            row.Currency = GetF(e, "currency", "currency_code", "currency_name", "cur");
            row.Method = GetF(e, "method", "payment_method", "paymentmethod", "pay_method", "way", "mode");
            var tRaw = res.TreasuryCol == null ? null : GetF(e, res.TreasuryCol);
            row.TreasuryId = ToLong(tRaw);
            if (row.TreasuryId.HasValue && res.Names != null && res.Names.ContainsKey(row.TreasuryId.Value))
                row.Treasury = res.Names[row.TreasuryId.Value];
            else if (row.TreasuryId.HasValue)
                row.Treasury = "#" + row.TreasuryId.Value;
            else
                row.Treasury = GetF(e, "treasury_name", "account_name", "cash_box_name", "cashbox_name", "box_name", "safe_name", "treasury", "account");
            row.Notes = GetF(e, "notes", "note", "comment", "comments", "remarks", "memo", "desc");
            return row;
        }

        private static string GetF(JsonElement e, params string[] names)
        {
            if (e.ValueKind != JsonValueKind.Object) return null;
            foreach (var nm in names)
            {
                if (string.IsNullOrEmpty(nm)) continue;
                foreach (var p in e.EnumerateObject())
                {
                    if (string.Equals(p.Name, nm, StringComparison.OrdinalIgnoreCase))
                    {
                        if (p.Value.ValueKind == JsonValueKind.Null || p.Value.ValueKind == JsonValueKind.Undefined) return null;
                        if (p.Value.ValueKind == JsonValueKind.String) return p.Value.GetString();
                        return p.Value.GetRawText();
                    }
                }
            }
            return null;
        }

        private static string NormalizeType(string v)
        {
            if (string.IsNullOrWhiteSpace(v)) return null;
            var s = v.Trim();
            var l = s.ToLowerInvariant();
            if (l == "1" || l == "in" || l == "incoming" || l == "receipt" || l == "receive" || s.Contains("قبض")) return "قبض";
            if (l == "2" || l == "out" || l == "outgoing" || l == "pay" || l == "payment" || s.Contains("صرف") || s.Contains("دفع")) return "صرف";
            return s;
        }

        private static decimal? ToDec(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            s = s.Trim().Replace(",", "").Replace("\u066B", ".").Replace("\u066C", "");
            var sb = new StringBuilder();
            foreach (var ch in s)
            {
                if (ch >= '\u0660' && ch <= '\u0669') sb.Append((char)('0' + (ch - '\u0660')));
                else sb.Append(ch);
            }
            decimal d;
            if (decimal.TryParse(sb.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out d)) return d;
            return null;
        }

        private static long? ToLong(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            decimal d;
            if (decimal.TryParse(s.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out d)) return (long)Math.Round(d);
            return null;
        }

        private static DateTime? ToDate(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            DateTime d;
            if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out d)) return d;
            var part = s.Trim().Substring(0, Math.Min(10, s.Trim().Length));
            if (DateTime.TryParseExact(part, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out d)) return d;
            return null;
        }

        private static string Q(string id)
        {
            return "\"" + (id ?? "").Replace("\"", "") + "\"";
        }

        private static string GetConnectionString()
        {
            if (!string.IsNullOrWhiteSpace(_cs)) return _cs;
            var env = Environment.GetEnvironmentVariable("TECHTOON_PG_CONNECTION");
            if (!string.IsNullOrWhiteSpace(env)) { _cs = env; return _cs; }
            try { var a = CsFromDbContexts(); if (!string.IsNullOrWhiteSpace(a)) { _cs = a; return _cs; } } catch { }
            if (!string.IsNullOrWhiteSpace(CS_FALLBACK)) { _cs = CS_FALLBACK; return _cs; }
            try { var b = CsFromFiles(); if (!string.IsNullOrWhiteSpace(b)) { _cs = b; return _cs; } } catch { }
            throw new InvalidOperationException("لم يتم العثور على سلسلة الاتصال بقاعدة البيانات");
        }

        private static string CsFromDbContexts()
        {
            try { System.Reflection.Assembly.Load("Techtoon.Data"); } catch { }
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var an = asm.GetName().Name;
                if (an == null || !an.StartsWith("Techtoon", StringComparison.OrdinalIgnoreCase)) continue;
                Type[] types;
                try { types = asm.GetTypes(); } catch { continue; }
                foreach (var t in types)
                {
                    if (t.IsAbstract || t.IsInterface) continue;
                    var isCtx = false;
                    for (var b = t.BaseType; b != null; b = b.BaseType)
                        if (b.FullName == "Microsoft.EntityFrameworkCore.DbContext") { isCtx = true; break; }
                    if (!isCtx) continue;
                    try
                    {
                        var inst = Activator.CreateInstance(t);
                        var dbv = t.GetProperty("Database");
                        if (dbv == null) continue;
                        var db = dbv.GetValue(inst, null);
                        if (db == null) continue;
                        var mi = db.GetType().GetMethod("GetConnectionString", Type.EmptyTypes);
                        if (mi == null) continue;
                        var cs = mi.Invoke(db, null) as string;
                        if (!string.IsNullOrWhiteSpace(cs)) return cs;
                    }
                    catch { }
                }
            }
            return null;
        }

        private static string CsFromFiles()
        {
            var dirs = new List<string>();
            try { dirs.Add(AppDomain.CurrentDomain.BaseDirectory); } catch { }
            try
            {
                var d = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
                for (int i = 0; i < 4 && d != null; i++) { d = d.Parent; if (d != null) dirs.Add(d.FullName); }
            }
            catch { }
            foreach (var dir in dirs)
            {
                if (dir == null || !Directory.Exists(dir)) continue;
                string[] files;
                try { files = Directory.GetFiles(dir, "*.*", SearchOption.TopDirectoryOnly); } catch { continue; }
                foreach (var f in files)
                {
                    var ext = Path.GetExtension(f).ToLowerInvariant();
                    if (ext != ".json" && ext != ".config" && ext != ".txt" && ext != ".ini" && ext != ".env") continue;
                    string txt;
                    try { txt = File.ReadAllText(f); } catch { continue; }
                    var m = Regex.Match(txt, "Host=[^;\\r\\n\"]{8,300}");
                    if (m.Success && m.Value.IndexOf("Password=", StringComparison.OrdinalIgnoreCase) >= 0) return m.Value;
                    m = Regex.Match(txt, "postgres(ql)?://[^\\s\\\"'<>]{10,300}");
                    if (m.Success) return m.Value;
                }
            }
            return null;
        }

        private void ApplyFilter()
        {
            if (Dg == null || _rows == null) return;
            IEnumerable<PaymentRow> list = _rows;
            var q = (SearchBox.Text ?? "").Trim();
            if (q.Length > 0)
            {
                list = list.Where(x =>
                    (x.Voucher != null && x.Voucher.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (x.Party != null && x.Party.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (x.Notes != null && x.Notes.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (x.Treasury != null && x.Treasury.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (x.Currency != null && x.Currency.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (x.Method != null && x.Method.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    x.Id.ToString(CultureInfo.InvariantCulture).Contains(q));
            }
            var tsel = TypeBox == null ? null : (TypeBox.SelectedItem as ComboBoxItem);
            var tname = tsel == null ? null : (tsel.Content as string);
            if (tname == "قبض" || tname == "صرف") list = list.Where(x => x.Type == tname);
            var fin = list.OrderByDescending(x => x.DateVal.HasValue ? x.DateVal.Value.Ticks : 0).ThenByDescending(x => x.Id).ToList();
            Dg.ItemsSource = fin;
            UpdateTotals(fin);
        }

        private void UpdateTotals(List<PaymentRow> list)
        {
            var groups = list.GroupBy(x => string.IsNullOrEmpty(x.Type) ? "أخرى" : x.Type)
                .Select(g => g.Key + ": " + g.Sum(y => y.AmountVal ?? 0m).ToString("N2", CultureInfo.InvariantCulture))
                .ToList();
            TotalsText.Text = "عدد السندات: " + list.Count() + (list.Count > 0 ? "   |   الإجمالي: " + string.Join("   |   ", groups) : "");
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) { ApplyFilter(); }
        private void TypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e) { ApplyFilter(); }

        private async void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            try { await ReloadAsync(); }
            catch (Exception ex) { ShowError("تحديث", ex); }
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e) { OpenEditor(null); }

        private void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!(Dg.SelectedItem is PaymentRow row)) { MessageBox.Show("اختر سنداً من الجدول أولاً", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            OpenEditor(row);
        }

        private async void DelBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!(Dg.SelectedItem is PaymentRow row)) { MessageBox.Show("اختر سنداً من الجدول أولاً", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (MessageBox.Show("هل تريد حذف السند رقم " + row.Id + "؟", "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            try
            {
                var table = _tbl;
                var cols = new List<string>(_cols);
                var id = row.Id;
                await Task.Run(() =>
                {
                    using (var con = new NpgsqlConnection(GetConnectionString()))
                    {
                        con.Open();
                        var kc = KeyCol(cols, table);
                        if (kc == null) throw new InvalidOperationException("لا يوجد عمود مفتاح في جدول " + table);
                        using (var c = new NpgsqlCommand("delete from " + Q(table) + " where " + Q(kc) + " = @k", con))
                        {
                            c.Parameters.AddWithValue("@k", id);
                            c.ExecuteNonQuery();
                        }
                    }
                });
                await ReloadAsync();
            }
            catch (Exception ex) { ShowError("حذف السند", ex); }
        }

        private static string KeyCol(List<string> cols, string table)
        {
            var cands = new[] { "id", "payment_id", table + "_id" };
            foreach (var cand in cands)
            {
                var hit = cols.FirstOrDefault(x => string.Equals(x, cand, StringComparison.OrdinalIgnoreCase));
                if (hit != null) return hit;
            }
            return null;
        }

        private static void InsertRow(NpgsqlConnection con, string table, Dictionary<string, object> vals)
        {
            var cols = vals.Keys.ToList();
            var names = new StringBuilder();
            var pars = new StringBuilder();
            var c = new NpgsqlCommand();
            for (int i = 0; i < cols.Count; i++)
            {
                if (i > 0) { names.Append(", "); pars.Append(", "); }
                names.Append(Q(cols[i]));
                pars.Append("@p").Append(i);
                c.Parameters.AddWithValue("@p" + i, vals[cols[i]] ?? (object)DBNull.Value);
            }
            c.Connection = con;
            c.CommandText = "insert into " + Q(table) + " (" + names + ") values (" + pars + ")";
            c.ExecuteNonQuery();
            c.Dispose();
        }

        private static void UpdateRow(NpgsqlConnection con, string table, long id, Dictionary<string, object> vals, List<string> allCols)
        {
            var kc = KeyCol(allCols, table);
            if (kc == null) throw new InvalidOperationException("لا يوجد عمود مفتاح (id) في جدول " + table + " - لا يمكن التعديل");
            var sets = new StringBuilder();
            var c = new NpgsqlCommand();
            int i = 0;
            foreach (var k in vals.Keys)
            {
                if (i > 0) sets.Append(", ");
                sets.Append(Q(k)).Append(" = @p").Append(i);
                c.Parameters.AddWithValue("@p" + i, vals[k] ?? (object)DBNull.Value);
                i++;
            }
            c.Connection = con;
            c.CommandText = "update " + Q(table) + " set " + sets + " where " + Q(kc) + " = @key";
            c.Parameters.AddWithValue("@key", id);
            c.ExecuteNonQuery();
            c.Dispose();
        }

        private void OpenEditor(PaymentRow row)
        {
            var win = new Window
            {
                Title = row == null ? "سند جديد" : "تعديل السند رقم " + row.Id,
                Width = 440,
                SizeToContent = SizeToContent.Height,
                FlowDirection = FlowDirection.RightToLeft,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };
            var g = new Grid { Margin = new Thickness(14) };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            int r = 0;
            var tbVoucher = AddField(g, r++, "رقم السند", row == null ? "" : (row.Voucher ?? ""));
            var dpDate = AddDate(g, r++, "التاريخ", row == null || !row.DateVal.HasValue ? DateTime.Today : row.DateVal.Value);
            var tbParty = AddField(g, r++, "الطرف", row == null ? "" : (row.Party ?? ""));
            var cmbType = AddCombo(g, r++, "النوع", new[] { "قبض", "صرف" }, row == null ? null : row.Type);
            var tbAmount = AddField(g, r++, "المبلغ", row == null || !row.AmountVal.HasValue ? "" : row.AmountVal.Value.ToString(CultureInfo.InvariantCulture));
            var tbCurrency = AddField(g, r++, "العملة", row == null ? "" : (row.Currency ?? ""));
            var tbMethod = AddField(g, r++, "الطريقة", row == null ? "" : (row.Method ?? ""));
            var cmbTreasury = AddTreasury(g, r++, "الخزينة", row == null ? (long?)null : row.TreasuryId);
            var tbNotes = AddField(g, r++, "ملاحظات", row == null ? "" : (row.Notes ?? ""));

            
            // حقل اختيار الفاتورة المرتبطة
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var lbInv = new TextBlock { Text = "فاتورة مرتبطة:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 5, 0, 5) };
            Grid.SetRow(lbInv, r);
            Grid.SetColumn(lbInv, 0);
            g.Children.Add(lbInv);
            var cmbInvoice = new ComboBox { MinHeight = 30, VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 3, 10, 3) };
            Grid.SetRow(cmbInvoice, r);
            Grid.SetColumn(cmbInvoice, 1);
            g.Children.Add(cmbInvoice);
            r++;

            // قائمة الفواتير غير المدفوعة
            var unpaidInvoices = new List<InvoiceOption>();
            try
            {
                using (var con = new NpgsqlConnection(GetConnectionString()))
                {
                    con.Open();
                    string sql = @"SELECT i.id, i.invoice_number, i.total, i.paid_amount, COALESCE(p.name, '') AS party_name, i.type
                        FROM invoices i LEFT JOIN parties p ON i.party_id = p.id
                        WHERE i.paid_amount < i.total ORDER BY i.id DESC LIMIT 200";
                    using (var cmd = new NpgsqlCommand(sql, con))
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            var opt = new InvoiceOption
                            {
                                Id = rdr.IsDBNull(0) ? 0 : rdr.GetInt32(0),
                                InvoiceNumber = rdr.IsDBNull(1) ? "" : rdr.GetString(1),
                                Total = rdr.IsDBNull(2) ? 0 : Convert.ToDecimal(rdr.GetValue(2)),
                                Paid = rdr.IsDBNull(3) ? 0 : Convert.ToDecimal(rdr.GetValue(3)),
                                PartyName = rdr.IsDBNull(4) ? "" : rdr.GetString(4),
                                Type = rdr.IsDBNull(5) ? "" : rdr.GetString(5)
                            };
                            unpaidInvoices.Add(opt);
                            cmbInvoice.Items.Add(new ComboBoxItem { Content = opt.Display, Tag = opt });
                        }
                    }
                }
            }
            catch { }

            // عند اختيار فاتورة، يُملأ المبلغ والطرف تلقائياً
            cmbInvoice.SelectionChanged += (s3, e3) =>
            {
                try
                {
                    if (cmbInvoice.SelectedItem is ComboBoxItem item && item.Tag is InvoiceOption inv)
                    {
                        if (string.IsNullOrWhiteSpace(tbAmount.Text) || tbAmount.Text == "0")
                            tbAmount.Text = inv.Remaining.ToString(CultureInfo.InvariantCulture);
                        if (string.IsNullOrWhiteSpace(tbParty.Text))
                            tbParty.Text = inv.PartyName;
                        if (inv.Type == "sales") cmbType.SelectedIndex = 0;
                        else if (inv.Type == "purchase") cmbType.SelectedIndex = 1;
                    }
                }
                catch { }
            };

            var sp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 14, 0, 0) };
            var ok = new Button { Content = row == null ? "إضافة" : "حفظ التعديل", MinWidth = 110, MinHeight = 32, Padding = new Thickness(10, 2, 10, 2) };
            var cancel = new Button { Content = "إلغاء", MinWidth = 90, MinHeight = 32, Padding = new Thickness(10, 2, 10, 2), Margin = new Thickness(8, 0, 0, 0) };
            sp.Children.Add(ok);
            sp.Children.Add(cancel);
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetRow(sp, r);
            g.Children.Add(sp);
            win.Content = g;
            cancel.Click += (s2, e2) => win.Close();
            ok.Click += async (s2, e2) =>
            {
                try
                {
                    var vals = new Dictionary<string, object>();
                    var vc = Pick(_cols, "voucher_no", "voucher", "voucherno", "receipt_no", "receiptno", "document_no", "doc_no", "no", "number", "code");
                    if (vc != null && !string.IsNullOrWhiteSpace(tbVoucher.Text)) vals[vc] = tbVoucher.Text.Trim();
                    var dc = Pick(_cols, "date", "payment_date", "paymentdate", "doc_date", "docdate", "trx_date", "date_paid", "created_at", "createdat");
                    if (dc != null) vals[dc] = dpDate.SelectedDate ?? DateTime.Today;
                    var pc = Pick(_cols, "party_name", "party", "customer_name", "customer", "client_name", "client", "beneficiary", "person_name", "person", "supplier", "supplier_name", "name");
                    if (pc != null && !string.IsNullOrWhiteSpace(tbParty.Text)) vals[pc] = tbParty.Text.Trim();
                    var tc = Pick(_cols, "type", "payment_type", "paymenttype", "kind", "direction", "trx_type", "transaction_type");
                    if (tc != null && cmbType.SelectedItem != null) vals[tc] = NormalizeType(cmbType.SelectedItem.ToString());
                    var ac = Pick(_cols, "amount", "amount_paid", "amountpaid", "payment_amount", "paid", "value", "total", "total_amount");
                    if (ac != null)
                    {
                        decimal amt;
                        if (decimal.TryParse((tbAmount.Text ?? "").Trim().Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out amt)) vals[ac] = amt;
                    }
                    var cc = Pick(_cols, "currency", "currency_code", "currency_name", "cur");
                    if (cc != null && !string.IsNullOrWhiteSpace(tbCurrency.Text)) vals[cc] = tbCurrency.Text.Trim();
                    var mc = Pick(_cols, "method", "payment_method", "paymentmethod", "pay_method", "way", "mode");
                    if (mc != null && !string.IsNullOrWhiteSpace(tbMethod.Text)) vals[mc] = tbMethod.Text.Trim();
                    if (_treasuryCol != null && cmbTreasury.SelectedValue is long tid) vals[_treasuryCol] = tid;
                    var nc = Pick(_cols, "notes", "note", "comment", "comments", "remarks", "memo", "desc");
                    if (nc != null && !string.IsNullOrWhiteSpace(tbNotes.Text)) vals[nc] = tbNotes.Text.Trim();

                    // ربط الفاتورة المرتبطة
                    InvoiceOption selectedInv = null;
                    if (cmbInvoice.SelectedItem is ComboBoxItem item && item.Tag is InvoiceOption inv)
                    {
                        selectedInv = inv;
                        var ic = Pick(_cols, "invoice_id", "invoiceid", "doc_id", "docid");
                        if (ic != null) vals[ic] = inv.Id;
                    }


                    if (vals.Count == 0) { MessageBox.Show("لا توجد أعمدة قابلة للكتابة في الجدول", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

                    ok.IsEnabled = false;
                    var snapshot = new Dictionary<string, object>(vals);
                    var isInsert = row == null;
                    var editId = row == null ? 0 : row.Id;
                    var table = _tbl;
                    var colsSnapshot = new List<string>(_cols);
                    decimal payAmt = 0;
                    decimal.TryParse((tbAmount.Text ?? "").Trim().Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out payAmt);
                    await Task.Run(() =>
                    {
                        using (var con = new NpgsqlConnection(GetConnectionString()))
                        {
                            con.Open();
                            if (isInsert) InsertRow(con, table, snapshot);
                            else UpdateRow(con, table, editId, snapshot, colsSnapshot);

                            // تحديث paid_amount للفاتورة المرتبطة
                            if (selectedInv != null && payAmt > 0)
                            {
                                try
                                {
                                    string updSql = "UPDATE invoices SET paid_amount = paid_amount + @amt WHERE id = @pid";
                                    using (var upCmd = new NpgsqlCommand(updSql, con))
                                    {
                                        upCmd.Parameters.AddWithValue("amt", payAmt);
                                        upCmd.Parameters.AddWithValue("pid", selectedInv.Id);
                                        upCmd.ExecuteNonQuery();
                                    }
                                }
                                catch { }
                            }

                        }
                    });
                    win.Close();
                    await ReloadAsync();
                }
                catch (Exception ex)
                {
                    ShowError(row == null ? "إضافة سند" : "تعديل السند " + row.Id, ex);
                    ok.IsEnabled = true;
                }
            };
            win.ShowDialog();
        }

        private static TextBox AddField(Grid g, int r, string label, string val)
        {
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var lb = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 5, 0, 5) };
            Grid.SetRow(lb, r);
            Grid.SetColumn(lb, 0);
            g.Children.Add(lb);
            var tb = new TextBox { Text = val ?? "", MinHeight = 30, VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 3, 10, 3) };
            Grid.SetRow(tb, r);
            Grid.SetColumn(tb, 1);
            g.Children.Add(tb);
            return tb;
        }

        private static DatePicker AddDate(Grid g, int r, string label, DateTime val)
        {
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var lb = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 5, 0, 5) };
            Grid.SetRow(lb, r);
            Grid.SetColumn(lb, 0);
            g.Children.Add(lb);
            var dp = new DatePicker { SelectedDate = val, Margin = new Thickness(10, 3, 10, 3) };
            Grid.SetRow(dp, r);
            Grid.SetColumn(dp, 1);
            g.Children.Add(dp);
            return dp;
        }

        private static ComboBox AddCombo(Grid g, int r, string label, string[] items, string sel)
        {
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var lb = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 5, 0, 5) };
            Grid.SetRow(lb, r);
            Grid.SetColumn(lb, 0);
            g.Children.Add(lb);
            var cb = new ComboBox { ItemsSource = items, MinHeight = 30, Margin = new Thickness(10, 3, 10, 3) };
            if (sel != null)
            {
                var norm = NormalizeType(sel);
                foreach (var it in items)
                    if (string.Equals(it, sel, StringComparison.OrdinalIgnoreCase) || string.Equals(it, norm, StringComparison.Ordinal))
                    { cb.SelectedItem = it; break; }
            }
            if (cb.SelectedItem == null && items.Length > 0) cb.SelectedIndex = 0;
            Grid.SetRow(cb, r);
            Grid.SetColumn(cb, 1);
            g.Children.Add(cb);
            return cb;
        }

        private ComboBox AddTreasury(Grid g, int r, string label, long? selId)
        {
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var lb = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 5, 0, 5) };
            Grid.SetRow(lb, r);
            Grid.SetColumn(lb, 0);
            g.Children.Add(lb);
            var cb = new ComboBox { MinHeight = 30, Margin = new Thickness(10, 3, 10, 3), DisplayMemberPath = "Value", SelectedValuePath = "Key" };
            if (_names.Count > 0)
            {
                cb.ItemsSource = _names;
                if (selId.HasValue) cb.SelectedValue = selId.Value;
            }
            else
            {
                cb.IsEnabled = false;
                if (selId.HasValue) { cb.Items.Add("#" + selId.Value); cb.SelectedIndex = 0; }
            }
            Grid.SetRow(cb, r);
            Grid.SetColumn(cb, 1);
            g.Children.Add(cb);
            return cb;
        }

        private void ShowError(string ctx, Exception ex)
        {
            var msg = Full(ctx, ex);
            try { SchemaText.Foreground = Brushes.Red; SchemaText.Text = "خطأ: " + msg; } catch { }
            MessageBox.Show(msg, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private static string Full(string ctx, Exception ex)
        {
            var sb = new StringBuilder();
            sb.Append(ctx).Append(" - ").Append(ex.GetType().Name).Append(": ").Append(ex.Message);
            var pg = ex as PostgresException;
            if (pg == null && ex.InnerException is PostgresException) pg = (PostgresException)ex.InnerException;
            if (pg != null) sb.Append(" [SQLSTATE ").Append(pg.SqlState).Append("]");
            var inn = ex.InnerException;
            int n = 0;
            while (inn != null && n < 6)
            {
                sb.Append(" <= ").Append(inn.GetType().Name).Append(": ").Append(inn.Message);
                inn = inn.InnerException;
                n++;
            }
            return sb.ToString();
        }
    }
}