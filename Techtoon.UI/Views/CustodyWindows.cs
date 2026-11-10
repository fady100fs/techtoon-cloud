using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Npgsql;
using Techtoon.Core.Data;
using Techtoon.UI.Services;

namespace Techtoon.UI.Views
{
    // ============================================================
    // مصروف على العهدة — يغلق حلقة petty_cash_expenses
    // ============================================================
    public class CustodyExpenseWindow : Window
    {
        private readonly int _custodyId;
        private readonly TextBox _txtAmount = new() { Margin = new Thickness(4) };
        private readonly TextBox _txtDesc = new() { Margin = new Thickness(4), Height = 45, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };
        private readonly TextBox _txtVendor = new() { Margin = new Thickness(4) };
        private readonly DatePicker _dpDate = new() { Margin = new Thickness(4), SelectedDate = DateTime.Today };
        private readonly ComboBox _cmbCat = new() { DisplayMemberPath = "Name", Margin = new Thickness(4) };
        private readonly TextBlock _lblStatus = new() { Foreground = System.Windows.Media.Brushes.Red, TextWrapping = TextWrapping.Wrap };
        private decimal _custodyAmount, _spentAmount;

        public CustodyExpenseWindow(int custodyId, string custodyNumber, decimal custodyAmount, decimal spentAmount)
        {
            _custodyId = custodyId; _custodyAmount = custodyAmount; _spentAmount = spentAmount;
            Title = "مصروف على العهدة - " + custodyNumber;
            Width = 440; Height = 440; WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FlowDirection = FlowDirection.RightToLeft;

            var grid = new Grid { Margin = new Thickness(12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int i = 0; i < 8; i++) grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            Row(grid, 0, "المبلغ:"); grid.Children.Add(_txtAmount); Grid.SetRow(_txtAmount, 0); Grid.SetColumn(_txtAmount, 1);
            Row(grid, 1, "التاريخ:"); grid.Children.Add(_dpDate); Grid.SetRow(_dpDate, 1); Grid.SetColumn(_dpDate, 1);
            Row(grid, 2, "الوصف:"); grid.Children.Add(_txtDesc); Grid.SetRow(_txtDesc, 2); Grid.SetColumn(_txtDesc, 1);
            Row(grid, 3, "المورد/الجهة:"); grid.Children.Add(_txtVendor); Grid.SetRow(_txtVendor, 3); Grid.SetColumn(_txtVendor, 1);
            Row(grid, 4, "التصنيف:"); grid.Children.Add(_cmbCat); Grid.SetRow(_cmbCat, 4); Grid.SetColumn(_cmbCat, 1);
            var info = new TextBlock
            {
                Text = "رصيد العهدة: " + custodyAmount.ToString("N2") + " — المصروف: " + spentAmount.ToString("N2") +
                        " — المتبقي: " + (custodyAmount - spentAmount).ToString("N2"),
                Foreground = System.Windows.Media.Brushes.DarkSlateGray, Margin = new Thickness(4), TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(info, 5); grid.Children.Add(info); Grid.SetColumn(info, 1);
            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
            var save = new Button { Content = "تسجيل المصروف", Padding = new Thickness(20, 6, 20, 6), Margin = new Thickness(4) };
            var cancel = new Button { Content = "إلغاء", Padding = new Thickness(20, 6, 20, 6), Margin = new Thickness(4) };
            save.Click += Save_Click; cancel.Click += (s, e) => Close();
            btns.Children.Add(save); btns.Children.Add(cancel);
            Grid.SetRow(btns, 6); grid.Children.Add(btns); Grid.SetColumn(btns, 1);
            Grid.SetRow(_lblStatus, 7); grid.Children.Add(_lblStatus); Grid.SetColumn(_lblStatus, 1);
            Content = grid;
            LoadCategories();
        }

        private static void Row(Grid g, int i, string label)
        {
            g.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold });
            Grid.SetRow(g.Children[g.Children.Count - 1], i); Grid.SetColumn(g.Children[g.Children.Count - 1], 0);
        }

        private void LoadCategories()
        {
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                _cmbCat.Items.Clear();
                _cmbCat.Items.Add(new ComboItem { Id = 0, Name = "— بدون تصنيف —" });
                using var cmd = new NpgsqlCommand("SELECT id, name FROM expense_categories ORDER BY name", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read()) _cmbCat.Items.Add(new ComboItem { Id = r.GetInt32(0), Name = r.GetString(1) });
                _cmbCat.SelectedIndex = 0;
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ: " + ex.Message; }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(_txtAmount.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var amount) || amount <= 0) { _lblStatus.Text = "المبلغ غير صحيح."; return; }
            if (string.IsNullOrWhiteSpace(_txtDesc.Text)) { _lblStatus.Text = "وصف المصروف مطلوب."; return; }
            var remaining = _custodyAmount - _spentAmount;
            if (amount > remaining + 0.005m) { _lblStatus.Text = "المبلغ أكبر من رصيد العهدة المتبقي (" + remaining.ToString("N2") + ")."; return; }
            var catId = (_cmbCat.SelectedItem as ComboItem)?.Id ?? 0;
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var tx = conn.BeginTransaction();
                using (var ins = new NpgsqlCommand(@"INSERT INTO petty_cash_expenses
                    (expense_number, custody_id, expense_date, amount, expense_category_id, description, vendor_name, status, recorded_by, recorded_at)
                    VALUES (@num, @cid, @d, @a, @cat, @desc, @v, 'approved', 1, NOW())", conn, tx))
                {
                    ins.Parameters.AddWithValue("@num", "CE-" + DateTime.Now.ToString("yyyyMMddHHmmss"));
                    ins.Parameters.AddWithValue("@cid", _custodyId);
                    ins.Parameters.AddWithValue("@d", DbValue.AsDateTime(_dpDate.SelectedDate ?? DateTime.Today));
                    ins.Parameters.AddWithValue("@a", amount);
                    ins.Parameters.AddWithValue("@cat", catId == 0 ? DBNull.Value : (object)catId);
                    ins.Parameters.AddWithValue("@desc", _txtDesc.Text.Trim());
                    ins.Parameters.AddWithValue("@v", string.IsNullOrWhiteSpace(_txtVendor.Text) ? DBNull.Value : (object)_txtVendor.Text.Trim());
                    ins.ExecuteNonQuery();
                }
                // تحديث حالة العهدة
                using (var up = new NpgsqlCommand(@"UPDATE petty_cash_custodies SET status = CASE
                    WHEN (COALESCE(amount,0) - (SELECT COALESCE(SUM(amount),0) FROM petty_cash_expenses WHERE custody_id=@cid)) <= 0.005
                    THEN 'settled' ELSE 'partially_settled' END WHERE id=@cid", conn, tx))
                {
                    up.Parameters.AddWithValue("@cid", _custodyId);
                    up.ExecuteNonQuery();
                }
                tx.Commit();
                AuditService.Log("CREATE", "PettyCashExpense", _custodyId, "Custody expense " + amount + " - " + _txtDesc.Text.Trim());
                DialogResult = true;
                Close();
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ: " + ex.Message; }
        }

        public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();
    }

    // ============================================================
    // تسوية العهدة — تغلق حلقة petty_cash_settlements (+ حركة نقدية حقيقية)
    // ============================================================
    public class CustodySettlementWindow : Window
    {
        private readonly int _custodyId;
        private readonly decimal _custodyAmount, _spentAmount;
        private readonly TextBox _txtActual = new() { Margin = new Thickness(4) };
        private readonly DatePicker _dpDate = new() { Margin = new Thickness(4), SelectedDate = DateTime.Today };
        private readonly ComboBox _cmbCB = new() { DisplayMemberPath = "Name", Margin = new Thickness(4) };
        private readonly TextBox _txtNotes = new() { Margin = new Thickness(4), Height = 40, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };
        private readonly TextBlock _lblStatus = new() { Foreground = System.Windows.Media.Brushes.Red, TextWrapping = TextWrapping.Wrap };

        public CustodySettlementWindow(int custodyId, string custodyNumber, decimal custodyAmount, decimal spentAmount, int defaultCashBoxId)
        {
            _custodyId = custodyId; _custodyAmount = custodyAmount; _spentAmount = spentAmount;
            Title = "تسوية العهدة - " + custodyNumber;
            Width = 440; Height = 430; WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FlowDirection = FlowDirection.RightToLeft;
            _txtActual.Text = Math.Max(0, custodyAmount - spentAmount).ToString("0.##");

            var grid = new Grid { Margin = new Thickness(12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int i = 0; i < 8; i++) grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            Row(grid, 0, "النقد المُعاد فعليًا:"); grid.Children.Add(_txtActual); Grid.SetRow(_txtActual, 0); Grid.SetColumn(_txtActual, 1);
            Row(grid, 1, "التاريخ:"); grid.Children.Add(_dpDate); Grid.SetRow(_dpDate, 1); Grid.SetColumn(_dpDate, 1);
            Row(grid, 2, "الخزينة:"); grid.Children.Add(_cmbCB); Grid.SetRow(_cmbCB, 2); Grid.SetColumn(_cmbCB, 1);
            Row(grid, 3, "ملاحظات:"); grid.Children.Add(_txtNotes); Grid.SetRow(_txtNotes, 3); Grid.SetColumn(_txtNotes, 1);
            var info = new TextBlock
            {
                Text = "إجمالي العهدة: " + custodyAmount.ToString("N2") + " — المصروف: " + spentAmount.ToString("N2") +
                        " — المتوقع المُعاد: " + Math.Max(0, custodyAmount - spentAmount).ToString("N2"),
                Foreground = System.Windows.Media.Brushes.DarkSlateGray, Margin = new Thickness(4), TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(info, 4); grid.Children.Add(info); Grid.SetColumn(info, 1);
            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
            var save = new Button { Content = "اعتماد التسوية", Padding = new Thickness(20, 6, 20, 6), Margin = new Thickness(4) };
            var cancel = new Button { Content = "إلغاء", Padding = new Thickness(20, 6, 20, 6), Margin = new Thickness(4) };
            save.Click += Save_Click; cancel.Click += (s, e) => Close();
            btns.Children.Add(save); btns.Children.Add(cancel);
            Grid.SetRow(btns, 5); grid.Children.Add(btns); Grid.SetColumn(btns, 1);
            Grid.SetRow(_lblStatus, 6); grid.Children.Add(_lblStatus); Grid.SetColumn(_lblStatus, 1);
            Content = grid;
            LoadCashBoxes(defaultCashBoxId);
        }

        private static void Row(Grid g, int i, string label)
        {
            g.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold });
            Grid.SetRow(g.Children[g.Children.Count - 1], i); Grid.SetColumn(g.Children[g.Children.Count - 1], 0);
        }

        private void LoadCashBoxes(int defaultId)
        {
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                _cmbCB.Items.Clear();
                using var cmd = new NpgsqlCommand("SELECT id, name FROM cash_boxes WHERE COALESCE(is_active,TRUE)=TRUE ORDER BY name", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read()) _cmbCB.Items.Add(new ComboItem { Id = r.GetInt32(0), Name = r.GetString(1) });
                for (int i = 0; i < _cmbCB.Items.Count; i++)
                    if (((ComboItem)_cmbCB.Items[i]).Id == defaultId) { _cmbCB.SelectedIndex = i; break; }
                if (_cmbCB.SelectedIndex < 0 && _cmbCB.Items.Count > 0) _cmbCB.SelectedIndex = 0;
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ: " + ex.Message; }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(_txtActual.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var actual) || actual < 0) { _lblStatus.Text = "المبلغ المُعاد غير صحيح."; return; }
            if (_cmbCB.SelectedItem is not ComboItem cb) { _lblStatus.Text = "اختر الخزينة (إلزامي)."; return; }
            var expected = Math.Max(0, _custodyAmount - _spentAmount);
            var shortage = expected - actual;
            if (shortage < 0) shortage = 0;
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var tx = conn.BeginTransaction();

                // حركة نقدية: النقد المُعاد يدخل الخزينة (receipt)
                if (actual > 0)
                {
                    using var pay = new NpgsqlCommand(@"INSERT INTO payments (date, amount, payment_type, payment_method, reference_number, notes, cash_box_id, currency_id)
                        VALUES (@d, @a, 'receipt', 'cash', @ref, @n, @cb, @cur) RETURNING id", conn, tx);
                    pay.Parameters.AddWithValue("@d", DbValue.AsDateTime(_dpDate.SelectedDate ?? DateTime.Today));
                    pay.Parameters.AddWithValue("@a", actual);
                    pay.Parameters.AddWithValue("@ref", "CUSTODY-SETTLE:" + _custodyId);
                    pay.Parameters.AddWithValue("@n", "تسوية عهدة #" + _custodyId + (string.IsNullOrWhiteSpace(_txtNotes.Text) ? "" : " - " + _txtNotes.Text));
                    pay.Parameters.AddWithValue("@cb", cb.Id);
                    pay.Parameters.AddWithValue("@cur", DbDefaults.GetDefaultCurrencyId(conn));
                    pay.ExecuteScalar();
                }

                using (var ins = new NpgsqlCommand(@"INSERT INTO petty_cash_settlements
                    (settlement_number, custody_id, settlement_date, total_expenses, remaining_amount, returned_amount, shortage_amount, cash_box_id, status, notes, created_at)
                    VALUES (@num, @cid, @d, @exp, @rem, @ret, @sh, @cb, 'approved', @n, NOW())", conn, tx))
                {
                    ins.Parameters.AddWithValue("@num", "CS-" + DateTime.Now.ToString("yyyyMMddHHmmss"));
                    ins.Parameters.AddWithValue("@cid", _custodyId);
                    ins.Parameters.AddWithValue("@d", DbValue.AsDateTime(_dpDate.SelectedDate ?? DateTime.Today));
                    ins.Parameters.AddWithValue("@exp", _spentAmount);
                    ins.Parameters.AddWithValue("@rem", expected);
                    ins.Parameters.AddWithValue("@ret", actual);
                    ins.Parameters.AddWithValue("@sh", shortage);
                    ins.Parameters.AddWithValue("@cb", cb.Id);
                    ins.Parameters.AddWithValue("@n", string.IsNullOrWhiteSpace(_txtNotes.Text) ? DBNull.Value : (object)_txtNotes.Text);
                    ins.ExecuteNonQuery();
                }

                using (var up = new NpgsqlCommand(@"UPDATE petty_cash_custodies SET status='settled', settled_at=NOW(), settled_by=1 WHERE id=@cid", conn, tx))
                { up.Parameters.AddWithValue("@cid", _custodyId); up.ExecuteNonQuery(); }

                tx.Commit();
                AuditService.Log("SETTLE", "PettyCash", _custodyId, "Custody settled: returned=" + actual + ", expenses=" + _spentAmount + ", shortage=" + shortage);
                DialogResult = true;
                Close();
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ: " + ex.Message; }
        }

        public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();
    }

    // ============================================================
    // طلب زيادة عهدة — يغلق حلقة custody_increase_requests
    // ============================================================
    public class CustodyIncreaseRequestWindow : Window
    {
        private readonly int _custodyId;
        private readonly decimal _currentAmount;
        private readonly TextBox _txtAmount = new() { Margin = new Thickness(4) };
        private readonly TextBox _txtReason = new() { Margin = new Thickness(4), Height = 60, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };
        private readonly TextBlock _lblStatus = new() { Foreground = System.Windows.Media.Brushes.Red, TextWrapping = TextWrapping.Wrap };

        public CustodyIncreaseRequestWindow(int custodyId, string custodyNumber, decimal currentAmount)
        {
            _custodyId = custodyId; _currentAmount = currentAmount;
            Title = "طلب زيادة عهدة - " + custodyNumber;
            Width = 420; Height = 360; WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FlowDirection = FlowDirection.RightToLeft;

            var grid = new Grid { Margin = new Thickness(12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int i = 0; i < 6; i++) grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            Row(grid, 0, "المبلغ المطلوب:"); grid.Children.Add(_txtAmount); Grid.SetRow(_txtAmount, 0); Grid.SetColumn(_txtAmount, 1);
            Row(grid, 1, "السبب:"); grid.Children.Add(_txtReason); Grid.SetRow(_txtReason, 1); Grid.SetColumn(_txtReason, 1);
            var info = new TextBlock { Text = "إجمالي العهدة الحالي: " + currentAmount.ToString("N2"), Foreground = System.Windows.Media.Brushes.DarkSlateGray, Margin = new Thickness(4) };
            Grid.SetRow(info, 2); grid.Children.Add(info); Grid.SetColumn(info, 1);
            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
            var save = new Button { Content = "إرسال الطلب", Padding = new Thickness(20, 6, 20, 6), Margin = new Thickness(4) };
            var cancel = new Button { Content = "إلغاء", Padding = new Thickness(20, 6, 20, 6), Margin = new Thickness(4) };
            save.Click += Save_Click; cancel.Click += (s, e) => Close();
            btns.Children.Add(save); btns.Children.Add(cancel);
            Grid.SetRow(btns, 3); grid.Children.Add(btns); Grid.SetColumn(btns, 1);
            Grid.SetRow(_lblStatus, 4); grid.Children.Add(_lblStatus); Grid.SetColumn(_lblStatus, 1);
            Content = grid;
        }

        private static void Row(Grid g, int i, string label)
        {
            g.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold });
            Grid.SetRow(g.Children[g.Children.Count - 1], i); Grid.SetColumn(g.Children[g.Children.Count - 1], 0);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(_txtAmount.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var amount) || amount <= 0) { _lblStatus.Text = "المبلغ غير صحيح."; return; }
            if (string.IsNullOrWhiteSpace(_txtReason.Text)) { _lblStatus.Text = "سبب الطلب مطلوب."; return; }
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var ins = new NpgsqlCommand(@"INSERT INTO custody_increase_requests
                    (request_number, custody_id, current_amount, requested_amount, new_total_amount, reason, status, requested_by, requested_at)
                    VALUES (@num, @cid, @cur, @req, @new, @reason, 'pending', 1, NOW())", conn);
                ins.Parameters.AddWithValue("@num", "CIR-" + DateTime.Now.ToString("yyyyMMddHHmmss"));
                ins.Parameters.AddWithValue("@cid", _custodyId);
                ins.Parameters.AddWithValue("@cur", _currentAmount);
                ins.Parameters.AddWithValue("@req", amount);
                ins.Parameters.AddWithValue("@new", _currentAmount + amount);
                ins.Parameters.AddWithValue("@reason", _txtReason.Text.Trim());
                ins.ExecuteNonQuery();
                AuditService.Log("REQUEST", "PettyCash", _custodyId, "Custody increase requested: " + amount + " - " + _txtReason.Text.Trim());
                DialogResult = true;
                Close();
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ: " + ex.Message; }
        }

        public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();
    }
}
