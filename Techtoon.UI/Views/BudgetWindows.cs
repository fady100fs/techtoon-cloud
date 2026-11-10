using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Npgsql;
using Techtoon.UI.Services;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    // ============================================================
    // B1 — نافذة إضافة/تعديل ميزانية (budgets)
    // تحقّق من التكرار (حساب + شهر + سنة) داخل معاملة واحدة + سجل تدقيق
    // ============================================================
    public class BudgetEditWindow : Window
    {
        private readonly int _budgetId;

        private readonly ComboBox _cmbAccount = new() { DisplayMemberPath = "Name", Margin = new Thickness(4), MinWidth = 320 };
        private readonly ComboBox _cmbMonth = new() { DisplayMemberPath = "Name", Margin = new Thickness(4), MinWidth = 140 };
        private readonly TextBox _txtYear = new() { Margin = new Thickness(4) };
        private readonly TextBox _txtAmount = new() { Margin = new Thickness(4) };
        private readonly TextBox _txtNotes = new() { Margin = new Thickness(4), Height = 60, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };
        private readonly CheckBox _chkExpenseOnly = new() { Content = "حسابات المصروفات فقط", IsChecked = true, Margin = new Thickness(4) };
        private readonly TextBlock _lblStatus = new() { Foreground = Brushes.Red, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4) };
        private readonly TextBlock _lblAccount = new() { Foreground = Brushes.DarkSlateGray, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4) };

        private int _loadedAccountId;
        private int _loadedMonth = DateTime.Now.Month;
        private int _loadedYear = DateTime.Now.Year;
        private decimal _loadedAmount;
        private string _loadedNotes = "";
        private bool _suppressReload;

        public BudgetEditWindow(int budgetId)
        {
            _budgetId = budgetId;
            Title = budgetId > 0 ? "تعديل ميزانية" : "إضافة ميزانية";
            Width = 520; Height = 520;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FlowDirection = FlowDirection.RightToLeft;
            FontFamily = new FontFamily("Tahoma");
            Background = new SolidColorBrush(Color.FromRgb(245, 247, 250));

            var grid = new Grid { Margin = new Thickness(12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int i = 0; i < 9; i++) grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            AddRow(grid, 0, "الحساب:", _cmbAccount);
            AddRow(grid, 1, "نوع الحساب:", _chkExpenseOnly);
            AddRow(grid, 2, "الشهر:", _cmbMonth);
            AddRow(grid, 3, "السنة:", _txtYear);
            AddRow(grid, 4, "المبلغ الميزاني:", _txtAmount);
            AddRow(grid, 5, "ملاحظات:", _txtNotes);
            Grid.SetRow(_lblAccount, 6); Grid.SetColumn(_lblAccount, 1); grid.Children.Add(_lblAccount);
            Grid.SetRow(_lblStatus, 7); Grid.SetColumn(_lblStatus, 1); grid.Children.Add(_lblStatus);

            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
            var save = new Button { Content = "حفظ", Padding = new Thickness(24, 6, 24, 6), Margin = new Thickness(4), Background = new SolidColorBrush(Color.FromRgb(0, 137, 123)), Foreground = Brushes.White, FontWeight = FontWeights.Bold };
            var cancel = new Button { Content = "إلغاء", Padding = new Thickness(24, 6, 24, 6), Margin = new Thickness(4) };
            save.Click += Save_Click;
            cancel.Click += (s, e) => Close();
            btns.Children.Add(save); btns.Children.Add(cancel);
            Grid.SetRow(btns, 8); Grid.SetColumn(btns, 1); grid.Children.Add(btns);

            Content = grid;

            _chkExpenseOnly.Checked += (s, e) => LoadAccounts();
            _chkExpenseOnly.Unchecked += (s, e) => LoadAccounts();

            LoadMonths();
            LoadAccounts();
            LoadExisting();
        }

        private static void AddRow(Grid g, int row, string label, UIElement control)
        {
            var lbl = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 6, 0) };
            Grid.SetRow(lbl, row); Grid.SetColumn(lbl, 0); g.Children.Add(lbl);
            Grid.SetRow(control, row); Grid.SetColumn(control, 1); g.Children.Add(control);
        }

        public static string GetCS() => SecureConnectionManager.GetConnectionString();

        private void LoadMonths()
        {
            _cmbMonth.Items.Clear();
            for (int m = 1; m <= 12; m++) _cmbMonth.Items.Add(new ComboItem { Id = m, Name = BudgetMonths.Name(m) });
            SelectById(_cmbMonth, _loadedMonth);
            if (_cmbMonth.SelectedIndex < 0 && _cmbMonth.Items.Count > 0) _cmbMonth.SelectedIndex = 0;
        }

        private static bool SelectById(ComboBox box, int id)
        {
            foreach (var item in box.Items)
            {
                if (item is ComboItem ci && ci.Id == id) { box.SelectedItem = ci; return true; }
            }
            return false;
        }

        private void LoadAccounts()
        {
            if (_suppressReload) return;
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                string sql = @"SELECT id, COALESCE(code,''), COALESCE(name,'')
                               FROM accounts
                               WHERE UPPER(COALESCE(type::text,'')) IN ('EXPENSE','EXPENSES','مصروفات')";
                if (_chkExpenseOnly.IsChecked != true) sql = "SELECT id, COALESCE(code,''), COALESCE(name,'') FROM accounts";
                sql += " ORDER BY code";

                int keep = (_cmbAccount.SelectedItem as ComboItem)?.Id ?? _loadedAccountId;
                _cmbAccount.Items.Clear();
                using var cmd = new NpgsqlCommand(sql, conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    string code = BudgetAssetsDb.ToText(r.GetValue(1));
                    string name = BudgetAssetsDb.ToText(r.GetValue(2));
                    _cmbAccount.Items.Add(new ComboItem
                    {
                        Id = BudgetAssetsDb.ToInt(r.GetValue(0)),
                        Name = string.IsNullOrWhiteSpace(code) ? name : code + " - " + name
                    });
                }
                if (keep > 0 && !SelectById(_cmbAccount, keep) && _chkExpenseOnly.IsChecked == true)
                {
                    // الحساب المحفوظ ليس من حسابات المصروفات — اعرض كل الحسابات حتى لا يفقد الاختيار
                    _suppressReload = true;
                    _chkExpenseOnly.IsChecked = false;
                    _suppressReload = false;
                    LoadAccounts();
                    return;
                }
                if (_cmbAccount.SelectedIndex < 0 && _cmbAccount.Items.Count > 0) _cmbAccount.SelectedIndex = 0;
                UpdateAccountInfo();
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ في تحميل الحسابات: " + ex.Message; }
        }

        private void UpdateAccountInfo()
        {
            var acc = _cmbAccount.SelectedItem as ComboItem;
            _lblAccount.Text = acc == null ? "لا توجد حسابات — تحقّق من وجود حسابات في دليل الحسابات."
                                          : "الحساب المختار: " + acc.Name;
        }

        private void LoadExisting()
        {
            if (_budgetId <= 0)
            {
                _txtYear.Text = DateTime.Now.Year.ToString();
                _txtAmount.Text = "0";
                UpdateAccountInfo();
                return;
            }
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var cmd = new NpgsqlCommand(
                    "SELECT account_id, month, year, budgeted_amount, COALESCE(notes,'') FROM budgets WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("id", _budgetId);
                using var r = cmd.ExecuteReader();
                if (!r.Read())
                {
                    _lblStatus.Text = "الميزانية غير موجودة.";
                    return;
                }
                _loadedAccountId = BudgetAssetsDb.ToInt(r.GetValue(0));
                _loadedMonth = BudgetAssetsDb.ToInt(r.GetValue(1));
                _loadedYear = BudgetAssetsDb.ToInt(r.GetValue(2));
                _loadedAmount = BudgetAssetsDb.ToDecimal(r.GetValue(3));
                _loadedNotes = BudgetAssetsDb.ToText(r.GetValue(4));

                if (!SelectById(_cmbAccount, _loadedAccountId))
                {
                    // الحساب ليس من حسابات المصروفات — اعرض كل الحسابات
                    _suppressReload = true;
                    _chkExpenseOnly.IsChecked = false;
                    _suppressReload = false;
                    LoadAccounts();
                    SelectById(_cmbAccount, _loadedAccountId);
                }
                SelectById(_cmbMonth, _loadedMonth);
                _txtYear.Text = _loadedYear.ToString();
                _txtAmount.Text = _loadedAmount.ToString("0.##");
                _txtNotes.Text = _loadedNotes;
                UpdateAccountInfo();
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ: " + ex.Message; }
        }

        private static bool TryParseAmount(string text, out decimal amount)
        {
            amount = 0m;
            string t = (text ?? "").Trim();
            if (t.Length == 0) return false;
            if (decimal.TryParse(t, NumberStyles.Any, CultureInfo.CurrentCulture, out amount)) return true;
            return decimal.TryParse(t, NumberStyles.Any, CultureInfo.InvariantCulture, out amount);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!(_cmbAccount.SelectedItem is ComboItem acc)) { _lblStatus.Text = "اختر الحساب (إلزامي)."; return; }
            if (!(_cmbMonth.SelectedItem is ComboItem mon)) { _lblStatus.Text = "اختر الشهر (إلزامي)."; return; }
            if (!int.TryParse((_txtYear.Text ?? "").Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture, out int year) || year < 1900 || year > 2999)
            { _lblStatus.Text = "السنة غير صحيحة — أدخل سنة بين 1900 و 2999."; return; }
            if (!TryParseAmount(_txtAmount.Text, out decimal amount) || amount < 0m)
            { _lblStatus.Text = "مبلغ الميزانية غير صحيح — أدخل رقمًا موجبًا."; return; }

            NpgsqlConnection? conn = null;
            NpgsqlTransaction? tx = null;
            try
            {
                conn = new NpgsqlConnection(GetCS());
                conn.Open();
                tx = conn.BeginTransaction();

                // منع التكرار (حساب + شهر + سنة)
                string dupSql = "SELECT id FROM budgets WHERE account_id=@aid AND month=@m AND year=@y";
                if (_budgetId > 0) dupSql += " AND id<>@id";
                dupSql += " LIMIT 1";
                using (var dup = new NpgsqlCommand(dupSql, conn, tx))
                {
                    dup.Parameters.AddWithValue("aid", acc.Id);
                    dup.Parameters.AddWithValue("m", mon.Id);
                    dup.Parameters.AddWithValue("y", year);
                    if (_budgetId > 0) dup.Parameters.AddWithValue("id", _budgetId);
                    var exists = dup.ExecuteScalar();
                    if (exists != null && exists != DBNull.Value)
                    {
                        _lblStatus.Text = "توجد بالفعل ميزانية لهذا الحساب في " + BudgetMonths.Name(mon.Id) + " " + year +
                                          " — عدّل الميزانية الموجودة بدلًا من إضافة ميزانية مكررة.";
                        return;
                    }
                }

                int budgetId = _budgetId;
                if (_budgetId > 0)
                {
                    using (var upd = new NpgsqlCommand(@"UPDATE budgets
                        SET account_id=@aid, month=@m, year=@y, budgeted_amount=@amt, notes=@notes
                        WHERE id=@id", conn, tx))
                    {
                        upd.Parameters.AddWithValue("aid", acc.Id);
                        upd.Parameters.AddWithValue("m", mon.Id);
                        upd.Parameters.AddWithValue("y", year);
                        upd.Parameters.AddWithValue("amt", amount);
                        upd.Parameters.AddWithValue("notes", string.IsNullOrWhiteSpace(_txtNotes.Text) ? (object)DBNull.Value : _txtNotes.Text.Trim());
                        upd.Parameters.AddWithValue("id", _budgetId);
                        upd.ExecuteNonQuery();
                    }
                }
                else
                {
                    using (var ins = new NpgsqlCommand(@"INSERT INTO budgets
                        (account_id, month, year, budgeted_amount, notes, created_by, created_at)
                        VALUES (@aid, @m, @y, @amt, @notes, @uid, NOW())
                        RETURNING id", conn, tx))
                    {
                        ins.Parameters.AddWithValue("aid", acc.Id);
                        ins.Parameters.AddWithValue("m", mon.Id);
                        ins.Parameters.AddWithValue("y", year);
                        ins.Parameters.AddWithValue("amt", amount);
                        ins.Parameters.AddWithValue("notes", string.IsNullOrWhiteSpace(_txtNotes.Text) ? (object)DBNull.Value : _txtNotes.Text.Trim());
                        int? uid = BudgetAssetsDb.CurrentUserId();
                        ins.Parameters.AddWithValue("uid", uid.HasValue ? (object)uid.Value : DBNull.Value);
                        budgetId = Convert.ToInt32(ins.ExecuteScalar(), CultureInfo.InvariantCulture);
                    }
                }

                tx.Commit();
                tx = null;

                string details = "Account=" + acc.Name + ", period=" + BudgetMonths.Name(mon.Id) + " " + year +
                                 ", amount=" + amount.ToString("N2");
                AuditService.Log(_budgetId > 0 ? "UPDATE" : "CREATE", "Budget", budgetId, details);

                DialogResult = true;
                Close();
            }
            catch (Npgsql.PostgresException px) when (px.SqlState == "23505")
            {
                _lblStatus.Text = "توجد بالفعل ميزانية لهذا الحساب في نفس الشهر والسنة — لا يمكن الحفظ مرتين.";
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "خطأ: " + ex.Message;
            }
            finally
            {
                try { if (tx != null) { tx.Rollback(); tx.Dispose(); } } catch { }
                try { if (conn != null) conn.Dispose(); } catch { }
            }
        }
    }
}
