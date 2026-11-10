using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Npgsql;
using Techtoon.Core.Data;
using Techtoon.UI.Services;

namespace Techtoon.UI.Views
{
    // ============================================================
    // نافذة السحب/السداد على خط الائتمان (overdraft)
    // ============================================================
    public class OverdraftWindow : Window
    {
        private readonly int _loanId;
        private readonly decimal _creditLimit;
        private readonly ComboBox _cmbAction = new() { Margin = new Thickness(4), Width = 160 };
        private readonly ComboBox _cmbCB = new() { DisplayMemberPath = "Name", Margin = new Thickness(4) };
        private readonly TextBox _txtAmount = new() { Margin = new Thickness(4) };
        private readonly DatePicker _dpDate = new() { Margin = new Thickness(4), SelectedDate = DateTime.Today };
        private readonly TextBox _txtNotes = new() { Margin = new Thickness(4), Height = 40, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };
        private readonly TextBlock _lblStatus = new() { Foreground = System.Windows.Media.Brushes.Red, TextWrapping = TextWrapping.Wrap };
        private readonly TextBlock _lblInfo = new() { Foreground = System.Windows.Media.Brushes.DarkSlateGray, Margin = new Thickness(4), TextWrapping = TextWrapping.Wrap };
        private readonly DataGrid _grid = new()
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            CanUserAddRows = false,
            HeadersVisibility = DataGridHeadersVisibility.Column
        };
        private readonly ObservableCollection<OverdraftService.OverdraftTransaction> _tx = new();

        public OverdraftWindow(int loanId, string loanNumber, decimal creditLimit)
        {
            _loanId = loanId; _creditLimit = creditLimit;
            Title = "السحب على البنك (خط ائتمان) - " + loanNumber;
            Width = 560; Height = 560; WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FlowDirection = FlowDirection.RightToLeft;

            _cmbAction.Items.Add("سحب (نقد يدخل الخزينة)");
            _cmbAction.Items.Add("سداد (نقد يخرج من الخزينة)");
            _cmbAction.SelectedIndex = 0;

            _grid.Columns.Add(new DataGridTextColumn { Header = "التاريخ", Binding = new System.Windows.Data.Binding("DateDisplay"), Width = 100 });
            _grid.Columns.Add(new DataGridTextColumn { Header = "النوع", Binding = new System.Windows.Data.Binding("TypeDisplay"), Width = 80 });
            _grid.Columns.Add(new DataGridTextColumn { Header = "المبلغ", Binding = new System.Windows.Data.Binding("AmountDisplay"), Width = 100 });
            _grid.Columns.Add(new DataGridTextColumn { Header = "الرصيد بعد", Binding = new System.Windows.Data.Binding("BalanceDisplay"), Width = 110 });
            _grid.Columns.Add(new DataGridTextColumn { Header = "ملاحظات", Binding = new System.Windows.Data.Binding("Notes"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            _grid.ItemsSource = _tx;

            var grid = new Grid { Margin = new Thickness(12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int i = 0; i < 9; i++) grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            Row(grid, 0, "العملية:"); grid.Children.Add(_cmbAction); Grid.SetRow(_cmbAction, 0); Grid.SetColumn(_cmbAction, 1);
            Row(grid, 1, "المبلغ:"); grid.Children.Add(_txtAmount); Grid.SetRow(_txtAmount, 1); Grid.SetColumn(_txtAmount, 1);
            Row(grid, 2, "التاريخ:"); grid.Children.Add(_dpDate); Grid.SetRow(_dpDate, 2); Grid.SetColumn(_dpDate, 2);
            Row(grid, 3, "الخزينة:"); grid.Children.Add(_cmbCB); Grid.SetRow(_cmbCB, 3); Grid.SetColumn(_cmbCB, 1);
            Row(grid, 4, "ملاحظات:"); grid.Children.Add(_txtNotes); Grid.SetRow(_txtNotes, 4); Grid.SetColumn(_txtNotes, 1);
            Grid.SetRow(_lblInfo, 5); grid.Children.Add(_lblInfo); Grid.SetColumn(_lblInfo, 1);
            Grid.SetRow(_grid, 6); grid.Children.Add(_grid); Grid.SetColumn(_grid, 1);
            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
            var save = new Button { Content = "تنفيذ", Padding = new Thickness(20, 6, 20, 6), Margin = new Thickness(4) };
            var cancel = new Button { Content = "إلغاء", Padding = new Thickness(20, 6, 20, 6), Margin = new Thickness(4) };
            save.Click += Save_Click; cancel.Click += (s, e) => Close();
            btns.Children.Add(save); btns.Children.Add(cancel);
            Grid.SetRow(btns, 7); grid.Children.Add(btns); Grid.SetColumn(btns, 1);
            Grid.SetRow(_lblStatus, 8); grid.Children.Add(_lblStatus); Grid.SetColumn(_lblStatus, 1);
            Content = grid;

            LoadCashBoxes();
            Refresh();
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

        private void Refresh()
        {
            try
            {
                _tx.Clear();
                foreach (var t in OverdraftService.GetTransactions(_loanId)) _tx.Add(t);
                var used = OverdraftService.GetUsedBalance(_loanId);
                _lblInfo.Text = "حد الائتمان: " + _creditLimit.ToString("N2") + " — المستخدم: " + used.ToString("N2") +
                               " — المتاح: " + Math.Max(0, _creditLimit - used).ToString("N2");
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ: " + ex.Message; }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(_txtAmount.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var amount) || amount <= 0) { _lblStatus.Text = "المبلغ غير صحيح."; return; }
            if (_cmbCB.SelectedItem is not ComboItem cb) { _lblStatus.Text = "اختر الخزينة (إلزامي)."; return; }
            var date = _dpDate.SelectedDate ?? DateTime.Today;
            try
            {
                if (_cmbAction.SelectedIndex == 0)
                    OverdraftService.Withdraw(_loanId, amount, cb.Id, date, _txtNotes.Text, 1);
                else
                    OverdraftService.Repay(_loanId, amount, cb.Id, date, _txtNotes.Text, 1);
                _txtAmount.Clear();
                Refresh();
                _lblStatus.Text = "تم التنفيذ بنجاح";
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ: " + ex.Message; }
        }

        public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();
    }
}
