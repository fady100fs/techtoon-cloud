using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;
using Techtoon.Core.Banking;
using Techtoon.UI.Services;

namespace Techtoon.UI.Views
{
    // ============================================================
    // استيراد كشوف البنك (PDF) — تحميل الملف داخل البرنامج
    // ============================================================
    public partial class BankStatementImportView : UserControl
    {
        private string _pdfPath = "";
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
                CashBoxCombo.Items.Clear();
                using var conn = new Npgsql.NpgsqlConnection(SecureConnectionManager.GetConnectionString());
                conn.Open();
                using var cmd = new Npgsql.NpgsqlCommand("SELECT id, name FROM cash_boxes WHERE COALESCE(is_active,TRUE)=TRUE ORDER BY name", conn);
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
                // استخراج + تحليل خارج خيط الواجهة (قراءة PDF قد تكون بطيئة)
                var lines = await Task.Run(() => BankStatementPdfReader.ReadAndParse(_pdfPath));
                _lines.Clear();
                foreach (var l in lines) _lines.Add(l);
                Status.Text = _lines.Count > 0
                    ? "تم استخراج " + _lines.Count + " حركة — راجع المعاينة ثم اضغط «استيراد»"
                    : "لم يتم التعرف على أي حركة في هذا الملف. تأكد أنه كشف حساب بنكي بصيغة نصية.";
            }
            catch (Exception ex) { Status.Text = "فشل قراءة الملف: " + ex.Message; }
        }

        private async Task ImportAsync()
        {
            if (_lines.Count == 0) { Status.Text = "لا توجد حركات للاستيراد."; return; }
            if (CashBoxCombo.SelectedItem is not ComboItem cb) { Status.Text = "اختر الخزينة/الحساب البنكي (إلزامي)."; return; }
            if (!decimal.TryParse(OpeningBox.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var opening)) opening = 0;
            if (!decimal.TryParse(ClosingBox.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var closing)) closing = 0;

            Status.Text = "جارٍ الاستيراد...";
            try
            {
                var service = BankStatementImportService.Create();
                var result = await service.ImportAsync(new System.Collections.Generic.List<BankStatementLine>(_lines),
                    cb.Id, opening, closing, _pdfPath);

                if (result.Success)
                {
                    Status.Text = result.Message + " (رقم الكشف " + result.StatementId + ")";
                }
                else
                {
                    Status.Text = result.Message;
                }
            }
            catch (Exception ex) { Status.Text = "فشل الاستيراد: " + ex.Message; }
        }

        /// <summary>ربط تلقائي: مطابقة التاريخ والمبلغ والاتجاه مع سندات القبض/الدفع.</summary>
        private async Task AutoMatchAsync()
        {
            if (_lines.Count == 0) { Status.Text = "لا توجد حركات للربط."; return; }
            if (CashBoxCombo.SelectedItem is not ComboItem cb) { Status.Text = "اختر الخزينة/الحساب البنكي (إلزامي)."; return; }

            Status.Text = "جارٍ الربط التلقائي...";
            try
            {
                var service = BankStatementImportService.Create();
                var result = await service.AutoMatchAsync(new System.Collections.Generic.List<BankStatementLine>(_lines), cb.Id);
                Status.Text = result.Success ? result.Message : result.Message;
                if (result.Success)
                {
                    // أعِد تحميل حالة الربط من قاعدة البيانات لتحديث الجدول
                    await Task.Run(() =>
                    {
                        using var conn = new Npgsql.NpgsqlConnection(SecureConnectionManager.GetConnectionString());
                        conn.Open();
                        using var cmd = new Npgsql.NpgsqlCommand(@"
                            SELECT bl.line_number, bl.match_status, bl.matched_payment_id
                            FROM bank_statement_lines bl
                            JOIN bank_statements bs ON bs.id = bl.statement_id
                            WHERE bs.cash_box_id = @cb
                            ORDER BY bl.id DESC LIMIT @lim", conn);
                        cmd.Parameters.AddWithValue("@cb", cb.Id);
                        cmd.Parameters.AddWithValue("@lim", _lines.Count);
                        using var r = cmd.ExecuteReader();
                        var map = new System.Collections.Generic.Dictionary<int, (string status, int? pid)>();
                        while (r.Read())
                            map[r.GetInt32(0)] = (r.GetString(1), r.IsDBNull(2) ? (int?)null : r.GetInt32(2));
                        foreach (var l in _lines)
                            if (map.TryGetValue(l.LineNumber, out var v)) { l.MatchStatus = v.status; l.MatchedPaymentId = v.pid; }
                    });
                    LinesGrid.Items.Refresh();
                }
            }
            catch (Exception ex) { Status.Text = "فشل الربط: " + ex.Message; }
        }
    }
}
