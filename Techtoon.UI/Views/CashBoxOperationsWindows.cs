using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Npgsql;
using Techtoon.UI.Services;

namespace Techtoon.UI.Views
{
    // ============================================================
    // تحويل بين الخزائن — يغلق حلقة cash_transfers
    // ============================================================
    public class CashTransferWindow : Window
    {
        private readonly ComboBox _cmbFrom = new() { DisplayMemberPath = "Name", Margin = new Thickness(4) };
        private readonly ComboBox _cmbTo = new() { DisplayMemberPath = "Name", Margin = new Thickness(4) };
        private readonly TextBox _txtAmount = new() { Margin = new Thickness(4), Text = "0" };
        private readonly TextBox _txtNotes = new() { Margin = new Thickness(4), Height = 40, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };
        private readonly TextBlock _lblStatus = new() { Foreground = System.Windows.Media.Brushes.Red, TextWrapping = TextWrapping.Wrap };
        private readonly TextBlock _lblBal = new() { Foreground = System.Windows.Media.Brushes.DarkSlateGray, Margin = new Thickness(4) };

        public CashTransferWindow()
        {
            Title = "تحويل بين الخزائن";
            Width = 440; Height = 430; WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FlowDirection = FlowDirection.RightToLeft;

            var grid = new Grid { Margin = new Thickness(12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int i = 0; i < 7; i++) grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            Row(grid, 0, "من خزينة:"); grid.Children.Add(_cmbFrom); Grid.SetRow(_cmbFrom, 0); Grid.SetColumn(_cmbFrom, 1);
            Row(grid, 1, "إلى خزينة:"); grid.Children.Add(_cmbTo); Grid.SetRow(_cmbTo, 1); Grid.SetColumn(_cmbTo, 1);
            Row(grid, 2, "المبلغ:"); grid.Children.Add(_txtAmount); Grid.SetRow(_txtAmount, 2); Grid.SetColumn(_txtAmount, 1);
            Row(grid, 3, "ملاحظات:"); grid.Children.Add(_txtNotes); Grid.SetRow(_txtNotes, 3); Grid.SetColumn(_txtNotes, 1);
            Grid.SetRow(_lblBal, 4); grid.Children.Add(_lblBal); Grid.SetColumn(_lblBal, 1);
            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
            var save = new Button { Content = "تنفيذ التحويل", Padding = new Thickness(20, 6, 20, 6), Margin = new Thickness(4) };
            var cancel = new Button { Content = "إلغاء", Padding = new Thickness(20, 6, 20, 6), Margin = new Thickness(4) };
            save.Click += Save_Click; cancel.Click += (s, e) => Close();
            btns.Children.Add(save); btns.Children.Add(cancel);
            Grid.SetRow(btns, 5); grid.Children.Add(btns); Grid.SetColumn(btns, 1);
            Grid.SetRow(_lblStatus, 6); grid.Children.Add(_lblStatus); Grid.SetColumn(_lblStatus, 1);
            Content = grid;

            LoadBoxes();
            _cmbFrom.SelectionChanged += async (s, e) => await ShowBalanceAsync();
        }

        private static void Row(Grid g, int i, string label)
        {
            g.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold });
            Grid.SetRow(g.Children[g.Children.Count - 1], i); Grid.SetColumn(g.Children[g.Children.Count - 1], 0);
        }

        private void LoadBoxes()
        {
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                _cmbFrom.Items.Clear(); _cmbTo.Items.Clear();
                using var cmd = new NpgsqlCommand("SELECT id, name FROM cash_boxes WHERE COALESCE(is_active,TRUE)=TRUE ORDER BY name", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    _cmbFrom.Items.Add(new ComboItem { Id = r.GetInt32(0), Name = r.GetString(1) });
                    _cmbTo.Items.Add(new ComboItem { Id = r.GetInt32(0), Name = r.GetString(1) });
                }
                if (_cmbFrom.Items.Count > 0) _cmbFrom.SelectedIndex = 0;
                if (_cmbTo.Items.Count > 1) _cmbTo.SelectedIndex = 1;
                if (_cmbFrom.Items.Count < 2) _lblStatus.Text = "يلزم خزينة واحدة على الأقل للتحويل.";
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ: " + ex.Message; }
        }

        private async Task ShowBalanceAsync()
        {
            try
            {
                if (_cmbFrom.SelectedItem is not ComboItem c) { _lblBal.Text = ""; return; }
                var bal = await FinancialService.GetCashBoxBalanceAsync(c.Id);
                _lblBal.Text = "رصيد الخزينة الحالية: " + bal.CurrentBalance.ToString("N2");
            }
            catch (Exception ex) { _lblBal.Text = "خطأ: " + ex.Message; }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_cmbFrom.SelectedItem is not ComboItem from) { _lblStatus.Text = "اختر الخزينة المصدر."; return; }
            if (_cmbTo.SelectedItem is not ComboItem to) { _lblStatus.Text = "اختر الخزينة المستقبِلة."; return; }
            if (from.Id == to.Id) { _lblStatus.Text = "لا يمكن التحويل إلى نفس الخزينة."; return; }
            if (!decimal.TryParse(_txtAmount.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var amount) || amount <= 0) { _lblStatus.Text = "المبلغ غير صحيح."; return; }
            try
            {
                bool ok = await FinancialService.TransferBetweenCashBoxesAsync(from.Id, to.Id, amount, _txtNotes.Text);
                if (!ok) { _lblStatus.Text = "تعذّر التحويل (رصيد غير كافٍ)."; return; }
                AuditService.Log("TRANSFER", "CashBox", from.Id, "Transfer to cash box " + to.Id + " amount " + amount);
                DialogResult = true;
                Close();
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ: " + ex.Message; }
        }

        public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();
    }

    // ============================================================
    // إغلاق الوردية — يغلق حلقة shift_closings
    // ============================================================
    public class CashCloseShiftWindow : Window
    {
        private readonly ComboBox _cmbCB = new() { DisplayMemberPath = "Name", Margin = new Thickness(4) };
        private readonly TextBox _txtActual = new() { Margin = new Thickness(4), Text = "0" };
        private readonly TextBox _txtNotes = new() { Margin = new Thickness(4), Height = 40, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };
        private readonly TextBlock _lblStatus = new() { Foreground = System.Windows.Media.Brushes.Red, TextWrapping = TextWrapping.Wrap };
        private readonly TextBlock _lblBal = new() { Foreground = System.Windows.Media.Brushes.DarkSlateGray, Margin = new Thickness(4) };

        public CashCloseShiftWindow()
        {
            Title = "إغلاق الوردية / تسوية الخزينة";
            Width = 440; Height = 420; WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FlowDirection = FlowDirection.RightToLeft;

            var grid = new Grid { Margin = new Thickness(12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int i = 0; i < 7; i++) grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            Row(grid, 0, "الخزينة:"); grid.Children.Add(_cmbCB); Grid.SetRow(_cmbCB, 0); Grid.SetColumn(_cmbCB, 1);
            Row(grid, 1, "الرصيد الفعلي عند الإغلاق:"); grid.Children.Add(_txtActual); Grid.SetRow(_txtActual, 1); Grid.SetColumn(_txtActual, 1);
            Row(grid, 2, "ملاحظات:"); grid.Children.Add(_txtNotes); Grid.SetRow(_txtNotes, 2); Grid.SetColumn(_txtNotes, 1);
            Grid.SetRow(_lblBal, 3); grid.Children.Add(_lblBal); Grid.SetColumn(_lblBal, 1);
            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
            var save = new Button { Content = "إغلاق الوردية", Padding = new Thickness(20, 6, 20, 6), Margin = new Thickness(4) };
            var cancel = new Button { Content = "إلغاء", Padding = new Thickness(20, 6, 20, 6), Margin = new Thickness(4) };
            save.Click += Save_Click; cancel.Click += (s, e) => Close();
            btns.Children.Add(save); btns.Children.Add(cancel);
            Grid.SetRow(btns, 4); grid.Children.Add(btns); Grid.SetColumn(btns, 1);
            Grid.SetRow(_lblStatus, 5); grid.Children.Add(_lblStatus); Grid.SetColumn(_lblStatus, 1);
            Content = grid;

            LoadBoxes();
            _cmbCB.SelectionChanged += async (s, e) => await ShowBalanceAsync();
        }

        private static void Row(Grid g, int i, string label)
        {
            g.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold });
            Grid.SetRow(g.Children[g.Children.Count - 1], i); Grid.SetColumn(g.Children[g.Children.Count - 1], 0);
        }

        private void LoadBoxes()
        {
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                _cmbCB.Items.Clear();
                using var cmd = new NpgsqlCommand("SELECT id, name FROM cash_boxes WHERE COALESCE(is_active,TRUE)=TRUE ORDER BY name", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read()) _cmbCB.Items.Add(new ComboItem { Id = r.GetInt32(0), Name = r.GetString(1) });
                if (_cmbCB.Items.Count > 0) _cmbCB.SelectedIndex = 0;
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ: " + ex.Message; }
        }

        private async Task ShowBalanceAsync()
        {
            try
            {
                if (_cmbCB.SelectedItem is not ComboItem c) { _lblBal.Text = ""; return; }
                var bal = await FinancialService.GetCashBoxBalanceAsync(c.Id);
                _lblBal.Text = "الرصيد المتوقّع: " + bal.CurrentBalance.ToString("N2") + "  (أدخل الفعلي — الفرق يُسجَّل كتسوية)";
            }
            catch (Exception ex) { _lblBal.Text = "خطأ: " + ex.Message; }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_cmbCB.SelectedItem is not ComboItem c) { _lblStatus.Text = "اختر الخزينة."; return; }
            if (!decimal.TryParse(_txtActual.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var actual) || actual < 0) { _lblStatus.Text = "الرصيد الفعلي غير صحيح."; return; }
            try
            {
                bool ok = await FinancialService.CloseShiftAsync(c.Id, actual, _txtNotes.Text);
                if (!ok) { _lblStatus.Text = "تعذّر إغلاق الوردية."; return; }
                AuditService.Log("CLOSE_SHIFT", "CashBox", c.Id, "Shift closed, actual=" + actual);
                DialogResult = true;
                Close();
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ: " + ex.Message; }
        }

        public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();
    }
}
