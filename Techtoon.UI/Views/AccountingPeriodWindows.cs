using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Npgsql;
using Techtoon.Core.Data;
using Techtoon.UI.Services;

namespace Techtoon.UI.Views
{
    /// <summary>
    /// A4: محرر الفترة المحاسبية (إضافة/تعديل).
    /// الحقول: period_name, start_date, end_date, is_fiscal_year, status.
    /// </summary>
    public class AccountingPeriodEditWindow : Window
    {
        private int _id = 0;
        private readonly int _currentUserId;

        private readonly TextBox _txtName = new() { Margin = new Thickness(4) };
        private readonly DatePicker _dpStart = new() { Margin = new Thickness(4) };
        private readonly DatePicker _dpEnd = new() { Margin = new Thickness(4) };
        private readonly CheckBox _chkFiscal = new() { Margin = new Thickness(4), Content = "سنة مالية", VerticalAlignment = VerticalAlignment.Center };
        private readonly ComboBox _cmbStatus = new() { Margin = new Thickness(4), MinWidth = 180 };
        private readonly TextBlock _lblStatus = new() { Foreground = System.Windows.Media.Brushes.Red, TextWrapping = TextWrapping.Wrap };

        public static string GetCS() => SecureConnectionManager.GetConnectionString();

        public AccountingPeriodEditWindow(int id = 0)
        {
            _id = id;
            _currentUserId = AuthService.CurrentUser?.Id ?? 0;

            Title = id > 0 ? "تعديل الفترة المحاسبية" : "إضافة فترة محاسبية";
            Width = 500; Height = 430;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FlowDirection = FlowDirection.RightToLeft;
            FontFamily = new System.Windows.Media.FontFamily("Tahoma");
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 247, 250));

            var grid = new Grid { Margin = new Thickness(12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int i = 0; i < 8; i++) grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            Row(grid, 0, "اسم الفترة:");
            grid.Children.Add(_txtName); Grid.SetRow(_txtName, 0); Grid.SetColumn(_txtName, 1);

            Row(grid, 1, "تاريخ البداية:");
            grid.Children.Add(_dpStart); Grid.SetRow(_dpStart, 1); Grid.SetColumn(_dpStart, 1);

            Row(grid, 2, "تاريخ النهاية:");
            grid.Children.Add(_dpEnd); Grid.SetRow(_dpEnd, 2); Grid.SetColumn(_dpEnd, 1);

            Row(grid, 3, "سنة مالية:");
            grid.Children.Add(_chkFiscal); Grid.SetRow(_chkFiscal, 3); Grid.SetColumn(_chkFiscal, 1);

            Row(grid, 4, "الحالة:");
            grid.Children.Add(_cmbStatus); Grid.SetRow(_cmbStatus, 4); Grid.SetColumn(_cmbStatus, 1);

            var info = new TextBlock
            {
                Text = "نهاية الفترة يجب أن تكون بعد بدايتها. الفترة المغلقة تمنع حفظ أي مستند بتاريخ داخلها.",
                Foreground = System.Windows.Media.Brushes.DarkSlateGray,
                Margin = new Thickness(4),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(info, 5); grid.Children.Add(info); Grid.SetColumn(info, 1);

            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
            var save = new Button { Content = "حفظ", Padding = new Thickness(20, 6, 20, 6), Margin = new Thickness(4) };
            var cancel = new Button { Content = "إلغاء", Padding = new Thickness(20, 6, 20, 6), Margin = new Thickness(4) };
            save.Click += Save_Click; cancel.Click += (s, e) => { DialogResult = false; Close(); };
            btns.Children.Add(save); btns.Children.Add(cancel);
            Grid.SetRow(btns, 6); grid.Children.Add(btns); Grid.SetColumn(btns, 1);

            Grid.SetRow(_lblStatus, 7); grid.Children.Add(_lblStatus); Grid.SetColumn(_lblStatus, 1);

            Content = grid;

            _cmbStatus.Items.Clear();
            _cmbStatus.Items.Add(new ComboItem { Id = 0, Name = "مفتوحة" });
            _cmbStatus.Items.Add(new ComboItem { Id = 1, Name = "مغلقة" });
            _cmbStatus.SelectedIndex = 0;

            // القيم الافتراضية: الشهر الميلادي الحالي كاملًا (بداية الشهر أولًا ونهاية الشهر آخرًا)
            var now = DateTime.Today;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            _dpStart.SelectedDate = monthStart;
            _dpEnd.SelectedDate = monthEnd;

            if (_id > 0) LoadPeriod();
        }

        private static void Row(Grid g, int i, string label)
        {
            var tb = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold };
            Grid.SetRow(tb, i); Grid.SetColumn(tb, 0);
            g.Children.Add(tb);
        }

        private void LoadPeriod()
        {
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var cmd = new NpgsqlCommand(
                    "SELECT period_name, start_date, end_date, COALESCE(is_fiscal_year, FALSE), status FROM accounting_periods WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("id", _id);
                using var r = cmd.ExecuteReader();
                if (r.Read())
                {
                    _txtName.Text = r.IsDBNull(0) ? "" : r.GetString(0);
                    _dpStart.SelectedDate = DbValue.AsDateTime(r.GetValue(1)).Date;
                    _dpEnd.SelectedDate = DbValue.AsDateTime(r.GetValue(2)).Date;
                    _chkFiscal.IsChecked = r.IsDBNull(3) ? false : r.GetBoolean(3);
                    string st = r.IsDBNull(4) ? "" : r.GetString(4);
                    _cmbStatus.SelectedIndex = AccountingPeriodStatus.IsClosed(st) ? 1 : 0;
                }
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ: " + ex.Message; }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string name = _txtName.Text.Trim();
            if (name.Length == 0) { _lblStatus.Text = "اسم الفترة مطلوب."; return; }
            if (_dpStart.SelectedDate == null || _dpEnd.SelectedDate == null) { _lblStatus.Text = "تاريخ البداية والنهاية مطلوبان."; return; }

            DateTime start = DbValue.AsDateTime(_dpStart.SelectedDate).Date;
            DateTime end = DbValue.AsDateTime(_dpEnd.SelectedDate).Date;
            if (end <= start) { _lblStatus.Text = "تاريخ النهاية يجب أن يكون بعد تاريخ البداية."; return; }

            bool closed = _cmbStatus.SelectedIndex == 1;

            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var tx = conn.BeginTransaction();

                if (_id > 0)
                {
                    // تحديث الحالة: الإغلاق الفعلي يتم عبر نافذة الإغلاق (closing_date/notes/closed_by)
                    string existingStatus;
                    using (var chk = new NpgsqlCommand("SELECT status FROM accounting_periods WHERE id=@id FOR UPDATE", conn, tx))
                    {
                        chk.Parameters.AddWithValue("id", _id);
                        existingStatus = chk.ExecuteScalar()?.ToString() ?? "";
                    }
                    bool wasClosed = AccountingPeriodStatus.IsClosed(existingStatus);

                    using var cmd = new NpgsqlCommand(
                        @"UPDATE accounting_periods
                          SET period_name=@name, start_date=@s, end_date=@e, is_fiscal_year=@fy, status=@st
                          WHERE id=@id", conn, tx);
                    cmd.Parameters.AddWithValue("name", name);
                    cmd.Parameters.AddWithValue("s", start);
                    cmd.Parameters.AddWithValue("e", end);
                    cmd.Parameters.AddWithValue("fy", _chkFiscal.IsChecked == true);
                    cmd.Parameters.AddWithValue("st", closed ? AccountingPeriodStatus.ClosedValue : AccountingPeriodStatus.OpenValue);
                    cmd.Parameters.AddWithValue("id", _id);
                    cmd.ExecuteNonQuery();

                    // إن كانت مغلقة الآن ولم تكن مغلقة سابقًا ⇒ ختم الإغلاق
                    if (closed && !wasClosed)
                    {
                        using var cl = new NpgsqlCommand(
                            "UPDATE accounting_periods SET closing_date=NOW(), closed_by=@cb WHERE id=@id", conn, tx);
                        cl.Parameters.AddWithValue("cb", _currentUserId > 0 ? (object)_currentUserId : DBNull.Value);
                        cl.Parameters.AddWithValue("id", _id);
                        cl.ExecuteNonQuery();
                    }
                }
                else
                {
                    using var cmd = new NpgsqlCommand(
                        @"INSERT INTO accounting_periods
                          (period_name, start_date, end_date, is_fiscal_year, status, closing_date, closing_notes, created_by, closed_by, created_at)
                          VALUES (@name, @s, @e, @fy, @st, NULL, NULL, @cb, NULL, NOW())", conn, tx);
                    cmd.Parameters.AddWithValue("name", name);
                    cmd.Parameters.AddWithValue("s", start);
                    cmd.Parameters.AddWithValue("e", end);
                    cmd.Parameters.AddWithValue("fy", _chkFiscal.IsChecked == true);
                    cmd.Parameters.AddWithValue("st", closed ? AccountingPeriodStatus.ClosedValue : AccountingPeriodStatus.OpenValue);
                    cmd.Parameters.AddWithValue("cb", _currentUserId > 0 ? (object)_currentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
                AuditService.Log(_id > 0 ? "UPDATE" : "CREATE", "AccountingPeriod", _id,
                    "فترة محاسبية: " + name + " من " + start.ToString("yyyy-MM-dd") + " إلى " + end.ToString("yyyy-MM-dd") + " — " + (closed ? "مغلقة" : "مفتوحة"));
                DialogResult = true;
                Close();
            }
            catch (Npgsql.PostgresException px)
            {
                _lblStatus.Text = "خطأ في قاعدة البيانات: " + px.Message;
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ: " + ex.Message; }
        }
    }

    /// <summary>
    /// A4: إغلاق الفترة المحاسبية — يضبط status='closed' + closing_date + closing_notes + closed_by.
    /// </summary>
    public class AccountingPeriodCloseWindow : Window
    {
        private readonly int _id;
        private readonly int _currentUserId;
        private readonly TextBox _txtNotes = new() { Margin = new Thickness(4), Height = 70, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };
        private readonly TextBlock _lblStatus = new() { Foreground = System.Windows.Media.Brushes.Red, TextWrapping = TextWrapping.Wrap };

        public static string GetCS() => SecureConnectionManager.GetConnectionString();

        public AccountingPeriodCloseWindow(int id, string periodName)
        {
            _id = id;
            _currentUserId = AuthService.CurrentUser?.Id ?? 0;

            Title = "إغلاق الفترة المحاسبية - " + periodName;
            Width = 460; Height = 340;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FlowDirection = FlowDirection.RightToLeft;
            FontFamily = new System.Windows.Media.FontFamily("Tahoma");
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 247, 250));

            var grid = new Grid { Margin = new Thickness(12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int i = 0; i < 5; i++) grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            var lbl = new TextBlock { Text = "ملاحظات الإغلاق:", VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold };
            Grid.SetRow(lbl, 0); Grid.SetColumn(lbl, 0); grid.Children.Add(lbl);
            Grid.SetRow(_txtNotes, 0); Grid.SetColumn(_txtNotes, 1); grid.Children.Add(_txtNotes);

            var info = new TextBlock
            {
                Text = "بعد الإغلاق لن يمكن حفظ أي فاتورة أو مستند بتاريخ يقع داخل هذه الفترة.",
                Foreground = System.Windows.Media.Brushes.DarkSlateGray,
                Margin = new Thickness(4),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(info, 1); Grid.SetColumn(info, 1); grid.Children.Add(info);

            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
            var save = new Button { Content = "تأكيد الإغلاق", Padding = new Thickness(20, 6, 20, 6), Margin = new Thickness(4) };
            var cancel = new Button { Content = "إلغاء", Padding = new Thickness(20, 6, 20, 6), Margin = new Thickness(4) };
            save.Click += Save_Click; cancel.Click += (s, e) => { DialogResult = false; Close(); };
            btns.Children.Add(save); btns.Children.Add(cancel);
            Grid.SetRow(btns, 2); Grid.SetColumn(btns, 1); grid.Children.Add(btns);

            Grid.SetRow(_lblStatus, 3); Grid.SetColumn(_lblStatus, 1); grid.Children.Add(_lblStatus);

            Content = grid;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string notes = _txtNotes.Text.Trim();
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var cmd = new NpgsqlCommand(
                    @"UPDATE accounting_periods
                      SET status=@st, closing_date=NOW(), closing_notes=@notes, closed_by=@cb
                      WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("st", AccountingPeriodStatus.ClosedValue);
                cmd.Parameters.AddWithValue("notes", notes.Length > 0 ? (object)notes : DBNull.Value);
                cmd.Parameters.AddWithValue("cb", _currentUserId > 0 ? (object)_currentUserId : DBNull.Value);
                cmd.Parameters.AddWithValue("id", _id);
                cmd.ExecuteNonQuery();

                AuditService.Log("UPDATE", "AccountingPeriod", _id, "إغلاق الفترة المحاسبية: " + notes);
                DialogResult = true;
                Close();
            }
            catch (Npgsql.PostgresException px)
            {
                _lblStatus.Text = "خطأ في قاعدة البيانات: " + px.Message;
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ: " + ex.Message; }
        }
    }
}
