using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Npgsql;
using Techtoon.Core.Data;
using Techtoon.UI.Services;

namespace Techtoon.UI.Views
{
    /// <summary>P5: الاعتراف بالإيراد المؤجل — "يُعترف بالإيراد عند البيع ويبقى بانتظار سند القبض".</summary>
    public partial class RevenueRecognitionView : UserControl
    {
        public RevenueRecognitionView()
        {
            InitializeComponent();
            DataContext = this;
            _ = LoadAsync();
            BtnRecognize.Click += async (s, e) => await RecognizeAsync();
            BtnReceipt.Click += async (s, e) => await CreateReceiptAsync();
        }

        private readonly ObservableCollection<RevenueRecognitionService.RecognitionRow> _rows = new();
        private int _selectedInvoiceId;

        private async Task LoadAsync()
        {
            try
            {
                // الحفاظ على التحديد عبر إعادة التحميل
                var keep = Grid.SelectedItem is RevenueRecognitionService.RecognitionRow kr ? kr.InvoiceId : _selectedInvoiceId;
                var search = SearchBox.Text;
                var list = await Task.Run(() => RevenueRecognitionService.GetRecognitions(search));
                _rows.Clear();
                foreach (var r in list) _rows.Add(r);
                Grid.ItemsSource = _rows;
                var selected = _rows.FirstOrDefault(r => r.InvoiceId == keep);
                if (selected != null) Grid.SelectedItem = selected;
                Status.Text = "تم التحميل: " + _rows.Count + " فاتورة بيع";
            }
            catch (Exception ex) { Status.Text = "خطأ: " + ex.Message; }
        }

        private async Task RecognizeAsync()
        {
            if (Grid.SelectedItem is not RevenueRecognitionService.RecognitionRow row) { Status.Text = "اختر فاتورة أولاً."; return; }
            _selectedInvoiceId = row.InvoiceId;
            try
            {
                await RevenueRecognitionService.EnsureRecognizedAsync(row.InvoiceId);
                Status.Text = "تم الاعتراف بالإيراد لفاتورة " + row.InvoiceNumber;
                await LoadAsync();
            }
            catch (Exception ex) { Status.Text = "خطأ: " + ex.Message; }
        }

        /// <summary>تحويل الفاتورة إلى سند قبض (التحصيل) — يغلق حلقة الاعتراف.</summary>
        private async Task CreateReceiptAsync()
        {
            if (Grid.SelectedItem is not RevenueRecognitionService.RecognitionRow row) { Status.Text = "اختر فاتورة أولاً."; return; }
            _selectedInvoiceId = row.InvoiceId;
            if (row.Remaining <= 0) { Status.Text = "الفاتورة مسددة بالكامل."; return; }
            var win = new RevenueReceiptWindow(row.InvoiceId, row.InvoiceNumber, row.Remaining);
            if (win.ShowDialog() == true)
            {
                Status.Text = "تم إنشاء سند القبض";
                await LoadAsync();
            }
        }

        private void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
    }

    // ============================================================
    // نافذة سند القبض (من الفاتورة) — تسجيل تحصيل حقيقي
    // ============================================================
    public class RevenueReceiptWindow : Window
    {
        private readonly int _invoiceId;
        private readonly decimal _remaining;
        private readonly TextBox _txtAmount = new() { Margin = new Thickness(4) };
        private readonly DatePicker _dpDate = new() { Margin = new Thickness(4), SelectedDate = DateTime.Today };
        private readonly ComboBox _cmbCB = new() { DisplayMemberPath = "Name", Margin = new Thickness(4) };
        private readonly ComboBox _cmbMethod = new() { Margin = new Thickness(4) };
        private readonly TextBox _txtRef = new() { Margin = new Thickness(4) };
        private readonly TextBlock _lblStatus = new() { Foreground = System.Windows.Media.Brushes.Red, TextWrapping = TextWrapping.Wrap };

        public RevenueReceiptWindow(int invoiceId, string invoiceNumber, decimal remaining)
        {
            _invoiceId = invoiceId; _remaining = remaining;
            Title = "تحويل إلى سند قبض — " + invoiceNumber;
            Width = 440; Height = 400; WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FlowDirection = FlowDirection.RightToLeft;
            _txtAmount.Text = remaining.ToString("0.##");
            _cmbMethod.Items.Add("نقدي"); _cmbMethod.Items.Add("تحويل بنكي"); _cmbMethod.SelectedIndex = 0;

            var grid = new Grid { Margin = new Thickness(12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int i = 0; i < 7; i++) grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            Row(grid, 0, "المبلغ:"); grid.Children.Add(_txtAmount); Grid.SetRow(_txtAmount, 0); Grid.SetColumn(_txtAmount, 1);
            Row(grid, 1, "التاريخ:"); grid.Children.Add(_dpDate); Grid.SetRow(_dpDate, 1); Grid.SetColumn(_dpDate, 1);
            Row(grid, 2, "الخزينة:"); grid.Children.Add(_cmbCB); Grid.SetRow(_cmbCB, 2); Grid.SetColumn(_cmbCB, 1);
            Row(grid, 3, "الطريقة:"); grid.Children.Add(_cmbMethod); Grid.SetRow(_cmbMethod, 3); Grid.SetColumn(_cmbMethod, 1);
            Row(grid, 4, "المرجع:"); grid.Children.Add(_txtRef); Grid.SetRow(_txtRef, 4); Grid.SetColumn(_txtRef, 1);
            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
            var save = new Button { Content = "تسجيل سند القبض", Padding = new Thickness(20, 6, 20, 6), Margin = new Thickness(4) };
            var cancel = new Button { Content = "إلغاء", Padding = new Thickness(20, 6, 20, 6), Margin = new Thickness(4) };
            save.Click += Save_Click; cancel.Click += (s, e) => Close();
            btns.Children.Add(save); btns.Children.Add(cancel);
            Grid.SetRow(btns, 5); grid.Children.Add(btns); Grid.SetColumn(btns, 1);
            Grid.SetRow(_lblStatus, 6); grid.Children.Add(_lblStatus); Grid.SetColumn(_lblStatus, 1);
            Content = grid;
            LoadCashBoxes();
        }

        private static void Row(Grid g, int i, string label)
        {
            g.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold });
            Grid.SetRow(g.Children[g.Children.Count - 1], i); Grid.SetColumn(g.Children[g.Children.Count - 1], 0);
        }

        private void LoadCashBoxes()
        {
            try
            {
                using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
                conn.Open();
                _cmbCB.Items.Clear();
                using var cmd = new NpgsqlCommand("SELECT id, name FROM cash_boxes WHERE COALESCE(is_active,TRUE)=TRUE ORDER BY name", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read()) _cmbCB.Items.Add(new ComboItem { Id = r.GetInt32(0), Name = r.GetString(1) });
                if (_cmbCB.Items.Count > 0) _cmbCB.SelectedIndex = 0;
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ: " + ex.Message; }
        }

        private void Save_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!decimal.TryParse(_txtAmount.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var amount) || amount <= 0) { _lblStatus.Text = "المبلغ غير صحيح."; return; }
            if (amount > _remaining + 0.005m) { _lblStatus.Text = "المبلغ أكبر من المتبقي (" + _remaining.ToString("N2") + ")."; return; }
            if (_cmbCB.SelectedItem is not ComboItem cb) { _lblStatus.Text = "اختر الخزينة (إلزامي)."; return; }
            var method = _cmbMethod.SelectedIndex == 1 ? "bank_transfer" : "cash";
            var date = _dpDate.SelectedDate ?? DateTime.Today;
            try
            {
                using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
                conn.Open();
                using var tx = conn.BeginTransaction();
                int paymentId;
                using (var pay = new NpgsqlCommand(@"INSERT INTO payments (date, amount, payment_type, payment_method, reference_number, notes, cash_box_id, currency_id)
                    VALUES (@d, @a, 'receipt', @m, @r, @n, @cb, @cur) RETURNING id", conn, tx))
                {
                    pay.Parameters.AddWithValue("@d", date);
                    pay.Parameters.AddWithValue("@a", amount);
                    pay.Parameters.AddWithValue("@m", method);
                    pay.Parameters.AddWithValue("@r", "INV-RECEIPT:" + _invoiceId);
                    pay.Parameters.AddWithValue("@n", "تحويل فاتورة إلى سند قبض");
                    pay.Parameters.AddWithValue("@cb", cb.Id);
                    pay.Parameters.AddWithValue("@cur", DbDefaults.GetDefaultCurrencyId(conn));
                    paymentId = Convert.ToInt32(pay.ExecuteScalar());
                }
                using (var ins = new NpgsqlCommand(@"INSERT INTO invoice_payments (invoice_id, method, amount, reference, paid_at, created_at)
                    VALUES (@i, @m, @a, @r, @d, NOW())", conn, tx))
                {
                    ins.Parameters.AddWithValue("@i", _invoiceId);
                    ins.Parameters.AddWithValue("@m", method);
                    ins.Parameters.AddWithValue("@a", amount);
                    ins.Parameters.AddWithValue("@r", "INV-RECEIPT:" + _invoiceId);
                    ins.Parameters.AddWithValue("@d", date);
                    ins.ExecuteNonQuery();
                }
                using (var up = new NpgsqlCommand("UPDATE invoices SET paid_amount = COALESCE(paid_amount,0) + @a WHERE id=@i", conn, tx))
                {
                    up.Parameters.AddWithValue("@a", amount);
                    up.Parameters.AddWithValue("@i", _invoiceId);
                    up.ExecuteNonQuery();
                }
                // ربط سجل الاعتراف بالسند (تحصيل)
                RevenueRecognitionService.MarkCollected(_invoiceId, paymentId);
                tx.Commit();
                AuditService.Log("PAYMENT", "Invoice", _invoiceId, "Invoice converted to receipt: " + amount.ToString("N2") + " (cash row " + paymentId + ")");
                DialogResult = true;
                Close();
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ: " + ex.Message; }
        }
    }
}
