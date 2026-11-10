using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Npgsql;
using Techtoon.Core.Data;
using Techtoon.UI.Services;

namespace Techtoon.UI.Views
{
    /// <summary>
    /// B5: محرر توزيع التكلفة (حساب + نسبة + شهر/سنة).
    /// يتحقق من أن النسبة بين 0 و 100 ومن عدم تكرار (مركز التكلفة، الحساب، الشهر، السنة).
    /// </summary>
    public class CostAllocationEditWindow : Window
    {
        private readonly int _costCenterId;
        private int _allocationId;
        private readonly int _currentUserId;

        private readonly ComboBox _cmbAccount = new() { DisplayMemberPath = "Name", Margin = new Thickness(4), MinWidth = 260 };
        private readonly ComboBox _cmbMonth = new() { Margin = new Thickness(4), MinWidth = 110 };
        private readonly TextBox _txtYear = new() { Margin = new Thickness(4), MinWidth = 90 };
        private readonly TextBox _txtPercentage = new() { Margin = new Thickness(4), MinWidth = 110 };
        private readonly TextBox _txtNotes = new() { Margin = new Thickness(4), Height = 50, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };
        private readonly TextBlock _lblStatus = new() { Foreground = System.Windows.Media.Brushes.Red, TextWrapping = TextWrapping.Wrap };

        public static string GetCS() => SecureConnectionManager.GetConnectionString();

        public CostAllocationEditWindow(int costCenterId, int allocationId = 0)
        {
            _costCenterId = costCenterId;
            _allocationId = allocationId;
            _currentUserId = AuthService.CurrentUser?.Id ?? 0;

            Title = allocationId > 0 ? "تعديل توزيع التكلفة" : "إضافة توزيع تكلفة";
            Width = 520; Height = 460;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FlowDirection = FlowDirection.RightToLeft;
            FontFamily = new System.Windows.Media.FontFamily("Tahoma");
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 247, 250));

            var grid = new Grid { Margin = new Thickness(12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int i = 0; i < 8; i++) grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            Row(grid, 0, "الحساب:");
            grid.Children.Add(_cmbAccount); Grid.SetRow(_cmbAccount, 0); Grid.SetColumn(_cmbAccount, 1);

            Row(grid, 1, "الشهر:");
            grid.Children.Add(_cmbMonth); Grid.SetRow(_cmbMonth, 1); Grid.SetColumn(_cmbMonth, 1);

            Row(grid, 2, "السنة:");
            grid.Children.Add(_txtYear); Grid.SetRow(_txtYear, 2); Grid.SetColumn(_txtYear, 1);

            Row(grid, 3, "نسبة التوزيع (%):");
            grid.Children.Add(_txtPercentage); Grid.SetRow(_txtPercentage, 3); Grid.SetColumn(_txtPercentage, 1);

            Row(grid, 4, "ملاحظات:");
            grid.Children.Add(_txtNotes); Grid.SetRow(_txtNotes, 4); Grid.SetColumn(_txtNotes, 1);

            var info = new TextBlock
            {
                Text = "النسبة يجب أن تكون بين 0 و 100، ولا يمكن تكرار نفس الحساب لنفس الشهر والسنة.",
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

            LoadMonths();
            LoadAccounts();
            _txtYear.Text = DateTime.Now.Year.ToString();
            _txtPercentage.Text = "0";
            if (_allocationId > 0) LoadAllocation();
        }

        private static void Row(Grid g, int i, string label)
        {
            var tb = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold };
            Grid.SetRow(tb, i); Grid.SetColumn(tb, 0);
            g.Children.Add(tb);
        }

        /// <summary>ملاحظات فارغة → NULL (وإلا نص).</summary>
        private object NotesParam()
        {
            string s = _txtNotes.Text.Trim();
            return s.Length > 0 ? (object)s : DBNull.Value;
        }

        private void LoadMonths()
        {
            string[] names = { "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو",
                               "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر" };
            _cmbMonth.Items.Clear();
            for (int i = 1; i <= 12; i++) _cmbMonth.Items.Add(new ComboItem { Id = i, Name = i + " - " + names[i - 1] });
            _cmbMonth.SelectedIndex = DateTime.Now.Month - 1;
        }

        private void LoadAccounts()
        {
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                _cmbAccount.Items.Clear();
                using var cmd = new NpgsqlCommand("SELECT id, code, name FROM accounts ORDER BY code LIMIT 1000", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    _cmbAccount.Items.Add(new ComboItem { Id = r.GetInt32(0), Name = r.GetString(1) + " - " + r.GetString(2) });
                if (_cmbAccount.Items.Count > 0) _cmbAccount.SelectedIndex = 0;
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ: " + ex.Message; }
        }

        private void LoadAllocation()
        {
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var cmd = new NpgsqlCommand(
                    "SELECT account_id, allocation_percentage, month, year, COALESCE(notes,'') FROM cost_allocations WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("id", _allocationId);
                using var r = cmd.ExecuteReader();
                if (r.Read())
                {
                    int accId = r.GetInt32(0);
                    decimal pct = Convert.ToDecimal(r.GetValue(1));
                    int month = r.GetInt32(2);
                    int year = r.GetInt32(3);
                    _txtNotes.Text = r.IsDBNull(4) ? "" : r.GetString(4);
                    _txtPercentage.Text = pct.ToString("0.####", CultureInfo.InvariantCulture);
                    _txtYear.Text = year.ToString();
                    _cmbMonth.SelectedIndex = Math.Clamp(month - 1, 0, 11);
                    for (int i = 0; i < _cmbAccount.Items.Count; i++)
                        if (((ComboItem)_cmbAccount.Items[i]).Id == accId) { _cmbAccount.SelectedIndex = i; break; }
                }
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ: " + ex.Message; }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!(_cmbAccount.SelectedItem is ComboItem acc) || acc.Id <= 0) { _lblStatus.Text = "اختر الحساب (إلزامي)."; return; }
            if (!(_cmbMonth.SelectedItem is ComboItem mon) || mon.Id < 1 || mon.Id > 12) { _lblStatus.Text = "اختر الشهر (إلزامي)."; return; }
            if (!int.TryParse(_txtYear.Text.Trim(), out int year) || year < 1900 || year > 2999) { _lblStatus.Text = "السنة غير صحيحة."; return; }
            if (!decimal.TryParse(_txtPercentage.Text.Trim(), NumberStyles.Any, CultureInfo.CurrentCulture, out decimal pct))
            {
                if (!decimal.TryParse(_txtPercentage.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out pct))
                { _lblStatus.Text = "نسبة التوزيع غير صحيحة."; return; }
            }
            if (pct < 0m || pct > 100m) { _lblStatus.Text = "نسبة التوزيع يجب أن تكون بين 0 و 100."; return; }

            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var tx = conn.BeginTransaction();

                // منع التكرار: (مركز التكلفة، الحساب، الشهر، السنة)
                using (var dup = new NpgsqlCommand(
                    "SELECT COUNT(*) FROM cost_allocations WHERE cost_center_id=@cc AND account_id=@acc AND month=@m AND year=@y AND id <> @id", conn, tx))
                {
                    dup.Parameters.AddWithValue("cc", _costCenterId);
                    dup.Parameters.AddWithValue("acc", acc.Id);
                    dup.Parameters.AddWithValue("m", mon.Id);
                    dup.Parameters.AddWithValue("y", year);
                    dup.Parameters.AddWithValue("id", _allocationId);
                    if (Convert.ToInt64(dup.ExecuteScalar()) > 0)
                    {
                        tx.Rollback();
                        _lblStatus.Text = "يوجد توزيع مسجّل لهذا الحساب في نفس الشهر والسنة.";
                        return;
                    }
                }

                if (_allocationId > 0)
                {
                    using var cmd = new NpgsqlCommand(
                        @"UPDATE cost_allocations
                          SET account_id=@acc, allocation_percentage=@pct, month=@m, year=@y, notes=@notes
                          WHERE id=@id", conn, tx);
                    cmd.Parameters.AddWithValue("acc", acc.Id);
                    cmd.Parameters.AddWithValue("pct", pct);
                    cmd.Parameters.AddWithValue("m", mon.Id);
                    cmd.Parameters.AddWithValue("y", year);
                    cmd.Parameters.AddWithValue("notes", NotesParam());
                    cmd.Parameters.AddWithValue("id", _allocationId);
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    using var cmd = new NpgsqlCommand(
                        @"INSERT INTO cost_allocations
                          (cost_center_id, account_id, allocation_percentage, month, year, notes, created_by, created_at)
                          VALUES (@cc, @acc, @pct, @m, @y, @notes, @cb, NOW())", conn, tx);
                    cmd.Parameters.AddWithValue("cc", _costCenterId);
                    cmd.Parameters.AddWithValue("acc", acc.Id);
                    cmd.Parameters.AddWithValue("pct", pct);
                    cmd.Parameters.AddWithValue("m", mon.Id);
                    cmd.Parameters.AddWithValue("y", year);
                    cmd.Parameters.AddWithValue("notes", NotesParam());
                    cmd.Parameters.AddWithValue("cb", _currentUserId > 0 ? (object)_currentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
                AuditService.Log(_allocationId > 0 ? "UPDATE" : "CREATE", "CostAllocation", _allocationId,
                    "توزيع تكلفة: مركز " + _costCenterId + " / حساب " + acc.Name + " / " + mon.Id + "/" + year + " / " + pct + "%");
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
    /// B5: محرر حركة مركز التكلفة (تاريخ، وصف، نوع، مبلغ، مرجع اختياري).
    /// 'debit' يزيد تكلفة المركز و 'credit' ينقصها.
    /// </summary>
    public class CostCenterTransactionWindow : Window
    {
        private readonly int _costCenterId;
        private int _transactionId;
        private readonly int _currentUserId;

        private readonly DatePicker _dpDate = new() { Margin = new Thickness(4), SelectedDate = DateTime.Today };
        private readonly TextBox _txtDesc = new() { Margin = new Thickness(4), Height = 45, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };
        private readonly ComboBox _cmbType = new() { Margin = new Thickness(4), MinWidth = 200 };
        private readonly TextBox _txtAmount = new() { Margin = new Thickness(4), MinWidth = 140 };
        private readonly TextBox _txtRefType = new() { Margin = new Thickness(4), MinWidth = 160 };
        private readonly TextBox _txtRefId = new() { Margin = new Thickness(4), MinWidth = 90 };
        private readonly TextBlock _lblStatus = new() { Foreground = System.Windows.Media.Brushes.Red, TextWrapping = TextWrapping.Wrap };

        public static string GetCS() => SecureConnectionManager.GetConnectionString();

        public CostCenterTransactionWindow(int costCenterId, int transactionId = 0)
        {
            _costCenterId = costCenterId;
            _transactionId = transactionId;
            _currentUserId = AuthService.CurrentUser?.Id ?? 0;

            Title = transactionId > 0 ? "تعديل حركة مركز التكلفة" : "إضافة حركة مركز تكلفة";
            Width = 540; Height = 480;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FlowDirection = FlowDirection.RightToLeft;
            FontFamily = new System.Windows.Media.FontFamily("Tahoma");
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 247, 250));

            var grid = new Grid { Margin = new Thickness(12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int i = 0; i < 9; i++) grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            Row(grid, 0, "التاريخ:");
            grid.Children.Add(_dpDate); Grid.SetRow(_dpDate, 0); Grid.SetColumn(_dpDate, 1);

            Row(grid, 1, "الوصف:");
            grid.Children.Add(_txtDesc); Grid.SetRow(_txtDesc, 1); Grid.SetColumn(_txtDesc, 1);

            Row(grid, 2, "نوع الحركة:");
            grid.Children.Add(_cmbType); Grid.SetRow(_cmbType, 2); Grid.SetColumn(_cmbType, 1);

            Row(grid, 3, "المبلغ:");
            grid.Children.Add(_txtAmount); Grid.SetRow(_txtAmount, 3); Grid.SetColumn(_txtAmount, 1);

            Row(grid, 4, "نوع المرجع (اختياري):");
            grid.Children.Add(_txtRefType); Grid.SetRow(_txtRefType, 4); Grid.SetColumn(_txtRefType, 1);

            Row(grid, 5, "رقم المرجع (اختياري):");
            grid.Children.Add(_txtRefId); Grid.SetRow(_txtRefId, 5); Grid.SetColumn(_txtRefId, 1);

            var info = new TextBlock
            {
                Text = "'مدين' يزيد تكلفة مركز التكلفة، و 'دائن' ينقصها. الصافي = مجموع المدين − مجموع الدائن.",
                Foreground = System.Windows.Media.Brushes.DarkSlateGray,
                Margin = new Thickness(4),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(info, 6); grid.Children.Add(info); Grid.SetColumn(info, 1);

            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
            var save = new Button { Content = "حفظ", Padding = new Thickness(20, 6, 20, 6), Margin = new Thickness(4) };
            var cancel = new Button { Content = "إلغاء", Padding = new Thickness(20, 6, 20, 6), Margin = new Thickness(4) };
            save.Click += Save_Click; cancel.Click += (s, e) => { DialogResult = false; Close(); };
            btns.Children.Add(save); btns.Children.Add(cancel);
            Grid.SetRow(btns, 7); grid.Children.Add(btns); Grid.SetColumn(btns, 1);

            Grid.SetRow(_lblStatus, 8); grid.Children.Add(_lblStatus); Grid.SetColumn(_lblStatus, 1);

            Content = grid;

            _cmbType.Items.Clear();
            _cmbType.Items.Add(new ComboItem { Id = 0, Name = "مدين (يزيد التكلفة)" });
            _cmbType.Items.Add(new ComboItem { Id = 1, Name = "دائن (ينقص التكلفة)" });
            _cmbType.SelectedIndex = 0;

            if (_transactionId > 0) LoadTransaction();
        }

        private static void Row(Grid g, int i, string label)
        {
            var tb = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold };
            Grid.SetRow(tb, i); Grid.SetColumn(tb, 0);
            g.Children.Add(tb);
        }

        private void LoadTransaction()
        {
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var cmd = new NpgsqlCommand(
                    "SELECT date, description, amount, transaction_type, COALESCE(reference_type,''), reference_id FROM cost_center_transactions WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("id", _transactionId);
                using var r = cmd.ExecuteReader();
                if (r.Read())
                {
                    _dpDate.SelectedDate = DbValue.AsDateTime(r.GetValue(0)).Date;
                    _txtDesc.Text = r.IsDBNull(1) ? "" : r.GetString(1);
                    _txtAmount.Text = Convert.ToDecimal(r.GetValue(2)).ToString("0.##", CultureInfo.InvariantCulture);
                    string type = r.IsDBNull(3) ? "debit" : r.GetString(3);
                    bool isCredit = type.Equals("credit", StringComparison.OrdinalIgnoreCase);
                    _cmbType.SelectedIndex = isCredit ? 1 : 0;
                    _txtRefType.Text = r.IsDBNull(4) ? "" : r.GetString(4);
                    _txtRefId.Text = r.IsDBNull(5) ? "" : r.GetInt32(5).ToString();
                }
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ: " + ex.Message; }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_dpDate.SelectedDate == null) { _lblStatus.Text = "التاريخ مطلوب."; return; }
            string desc = _txtDesc.Text.Trim();
            if (desc.Length == 0) { _lblStatus.Text = "وصف الحركة مطلوب."; return; }
            if (!decimal.TryParse(_txtAmount.Text.Trim(), NumberStyles.Any, CultureInfo.CurrentCulture, out decimal amount))
            {
                if (!decimal.TryParse(_txtAmount.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out amount))
                { _lblStatus.Text = "المبلغ غير صحيح."; return; }
            }
            if (amount <= 0m) { _lblStatus.Text = "المبلغ يجب أن يكون أكبر من صفر."; return; }

            bool isCredit = _cmbType.SelectedIndex == 1;
            string type = isCredit ? "credit" : "debit";
            string refType = _txtRefType.Text.Trim();
            int? refId = null;
            if (!string.IsNullOrWhiteSpace(_txtRefId.Text.Trim()))
            {
                if (!int.TryParse(_txtRefId.Text.Trim(), out int rid) || rid <= 0) { _lblStatus.Text = "رقم المرجع غير صحيح."; return; }
                refId = rid;
            }
            if (refId.HasValue && refType.Length == 0) refType = "manual";

            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var tx = conn.BeginTransaction();

                if (_transactionId > 0)
                {
                    using var cmd = new NpgsqlCommand(
                        @"UPDATE cost_center_transactions
                          SET date=@d, description=@desc, amount=@amt, transaction_type=@type,
                              reference_type=@rt, reference_id=@rid
                          WHERE id=@id", conn, tx);
                    cmd.Parameters.AddWithValue("d", DbValue.AsDateTime(_dpDate.SelectedDate));
                    cmd.Parameters.AddWithValue("desc", desc);
                    cmd.Parameters.AddWithValue("amt", amount);
                    cmd.Parameters.AddWithValue("type", type);
                    cmd.Parameters.AddWithValue("rt", refType.Length > 0 ? (object)refType : DBNull.Value);
                    cmd.Parameters.AddWithValue("rid", refId.HasValue ? (object)refId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("id", _transactionId);
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    using var cmd = new NpgsqlCommand(
                        @"INSERT INTO cost_center_transactions
                          (cost_center_id, date, description, amount, transaction_type, reference_type, reference_id, created_by, created_at)
                          VALUES (@cc, @d, @desc, @amt, @type, @rt, @rid, @cb, NOW())", conn, tx);
                    cmd.Parameters.AddWithValue("cc", _costCenterId);
                    cmd.Parameters.AddWithValue("d", DbValue.AsDateTime(_dpDate.SelectedDate));
                    cmd.Parameters.AddWithValue("desc", desc);
                    cmd.Parameters.AddWithValue("amt", amount);
                    cmd.Parameters.AddWithValue("type", type);
                    cmd.Parameters.AddWithValue("rt", refType.Length > 0 ? (object)refType : DBNull.Value);
                    cmd.Parameters.AddWithValue("rid", refId.HasValue ? (object)refId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("cb", _currentUserId > 0 ? (object)_currentUserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
                AuditService.Log(_transactionId > 0 ? "UPDATE" : "CREATE", "CostCenterTransaction", _transactionId,
                    "حركة مركز تكلفة " + _costCenterId + ": " + type + " " + amount.ToString("N2") + " - " + desc);
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
