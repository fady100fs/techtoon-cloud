using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Npgsql;
using Techtoon.UI.Services;
using Techtoon.UI.Views;

namespace Techtoon.UI.ViewModels
{
    public class CashBoxRow : ViewModelBase
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Type { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal CurrentBalance { get; set; }
        public int AccountId { get; set; }
        public string OpeningDisplay { get { return OpeningBalance.ToString("N2"); } }
        public string CurrentDisplay { get { return CurrentBalance.ToString("N2"); } }
        public string TypeDisplay => Type ?? string.Empty;
        public string StatusDisplay => "نشط";
    }

    public class CashBoxesViewModel : ViewModelBase
    {
        public ObservableCollection<CashBoxRow> CashBoxes { get; set; }
        private CashBoxRow _selectedRow;
        public CashBoxRow SelectedRow { get => _selectedRow; set => SetProperty(ref _selectedRow, value); }
        public ICommand RefreshCommand { get; set; }
        public ICommand AddCommand { get; set; }
        public ICommand EditCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        public ICommand TransferCommand { get; set; }
        public ICommand CloseShiftCommand { get; set; }
        public ObservableCollection<string> OperationsLog { get; } = new();

        public CashBoxesViewModel()
        {
            CashBoxes = new ObservableCollection<CashBoxRow>();
            RefreshCommand = new UiRelayCommand(_ => _ = LoadAsync());
            AddCommand = new UiRelayCommand(_ => AddBox());
            EditCommand = new UiRelayCommand(_ => EditBox(), _ => SelectedRow != null);
            DeleteCommand = new UiRelayCommand(_ => DeleteBox(), _ => SelectedRow != null);
            TransferCommand = new UiRelayCommand(_ => TransferBetweenBoxes());
            CloseShiftCommand = new UiRelayCommand(_ => CloseShift());
            _ = LoadAsync();
        }

        public async System.Threading.Tasks.Task LoadAsync()
        {
            CashBoxes.Clear();
            try
            {
                using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
                await conn.OpenAsync();
                // حساب الرصيد ديناميكيًا: افتتاحي + قبض - صرف - مصروفات + قروض + تحويلات واردة - تحويلات صادرة
                string sql = @"
                    SELECT cb.id, cb.name, cb.code, cb.type, COALESCE(cb.opening_balance, 0),
                    COALESCE(cb.opening_balance, 0) 
                    + COALESCE((SELECT SUM(amount) FROM payments WHERE cash_box_id = cb.id AND payment_type = 'receipt'), 0)
                    - COALESCE((SELECT SUM(amount) FROM payments WHERE cash_box_id = cb.id AND payment_type = 'payment'), 0)
                    - COALESCE((SELECT SUM(amount) FROM expenses WHERE cash_box_id = cb.id), 0)
                    + COALESCE((SELECT SUM(principal_amount) FROM loans WHERE bank_account_id = cb.id), 0)
                    + COALESCE((SELECT SUM(amount) FROM cash_transfers WHERE to_cash_box_id = cb.id), 0)
                    - COALESCE((SELECT SUM(amount) FROM cash_transfers WHERE from_cash_box_id = cb.id), 0)
                    AS current_balance
                    FROM cash_boxes cb WHERE cb.is_active = TRUE ORDER BY cb.name";

                using var cmd = new NpgsqlCommand(sql, conn);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    CashBoxes.Add(new CashBoxRow
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Code = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        Type = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        OpeningBalance = Convert.ToDecimal(reader.GetValue(4)),
                        CurrentBalance = Convert.ToDecimal(reader.GetValue(5))
                    });
                }
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }
                // CRUD — نافذة تحرير برمجية (بدون XAML) لدعم الأزرار الميتة سابقًا
        private void AddBox()
        {
            var dlg = new CashBoxEditor(0);
            if (dlg.ShowDialog() == true) _ = LoadAsync();
        }

        private void EditBox()
        {
            if (SelectedRow == null) return;
            var dlg = new CashBoxEditor(SelectedRow.Id);
            if (dlg.ShowDialog() == true) _ = LoadAsync();
        }

        /// <summary>P5: تحويل بين الخزائن — يغلق حلقة cash_transfers (كان بلا واجهة).</summary>
        private void TransferBetweenBoxes()
        {
            try
            {
                var win = new CashTransferWindow();
                if (win.ShowDialog() == true)
                {
                    _ = LoadAsync();
                    _ = LoadOperationsAsync();
                }
            }
            catch (Exception ex) { MessageBox.Show("خطأ: " + ex.Message); }
        }

        /// <summary>P5: إغلاق الوردية — يغلق حلقة shift_closings (كان بلا واجهة).</summary>
        private void CloseShift()
        {
            try
            {
                var win = new CashCloseShiftWindow();
                if (win.ShowDialog() == true)
                {
                    _ = LoadAsync();
                    _ = LoadOperationsAsync();
                }
            }
            catch (Exception ex) { MessageBox.Show("خطأ: " + ex.Message); }
        }

        /// <summary>P5: سجل الورديات والتحويلات (يعرض آخر العمليات).</summary>
        public async Task LoadOperationsAsync()
        {
            try
            {
                OperationsLog.Clear();
                using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand(@"
                    SELECT 'إغلاق وردية — خزينة ' || COALESCE(cb.name, sc.cash_box_id::text) ||
                           ' | متوقّع ' || sc.expected_closing || ' | فعلي ' || sc.actual_closing ||
                           ' | فرق ' || sc.difference || ' | ' || to_char(sc.shift_date,'YYYY-MM-DD HH24:MI')
                    FROM shift_closings sc LEFT JOIN cash_boxes cb ON cb.id = sc.cash_box_id
                    ORDER BY sc.id DESC LIMIT 5", conn))
                using (var r = await cmd.ExecuteReaderAsync())
                    while (await r.ReadAsync())
                        OperationsLog.Add("• " + r.GetString(0));

                using (var cmd = new NpgsqlCommand(@"
                    SELECT 'تحويل ' || COALESCE(f.name,'?') || ' ← ' || COALESCE(t.name,'?') ||
                           ' | مبلغ ' || ct.amount || ' | ' || to_char(ct.date,'YYYY-MM-DD HH24:MI')
                    FROM cash_transfers ct
                    LEFT JOIN cash_boxes f ON f.id = ct.from_cash_box_id
                    LEFT JOIN cash_boxes t ON t.id = ct.to_cash_box_id
                    ORDER BY ct.id DESC LIMIT 5", conn))
                using (var r = await cmd.ExecuteReaderAsync())
                    while (await r.ReadAsync())
                        OperationsLog.Add("• " + r.GetString(0));
            }
            catch (Exception ex) { OperationsLog.Add("خطأ في تحميل السجل: " + ex.Message); }
        }

        private void DeleteBox()
        {
            if (SelectedRow == null) return;
            if (MessageBox.Show("حذف الخزينة '" + SelectedRow.Name + "'؟", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            try
            {
                using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
                conn.Open();
                // ضابط: يمنع الحذف عند وجود حركات
                using (var chk = new NpgsqlCommand("SELECT (SELECT COUNT(*) FROM payments WHERE cash_box_id=@id) + (SELECT COUNT(*) FROM expenses WHERE cash_box_id=@id) + (SELECT COUNT(*) FROM cash_transfers WHERE from_cash_box_id=@id OR to_cash_box_id=@id)", conn))
                { chk.Parameters.AddWithValue("id", SelectedRow.Id); if (Convert.ToInt64(chk.ExecuteScalar()) > 0) { MessageBox.Show("لا يمكن الحذف: الخزينة مرتبطة بحركات مالية."); return; } }
                using var cmd = new NpgsqlCommand("DELETE FROM cash_boxes WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("id", SelectedRow.Id);
                cmd.ExecuteNonQuery();
                AuditService.Log("DELETE", "CashBox", SelectedRow.Id, "Deleted cash box: " + SelectedRow.Name);
                _ = LoadAsync();
            }
            catch (Exception ex) { MessageBox.Show("خطأ: " + ex.Message); }
        }
    }

    /// <summary>نافذة تحرير خزينة (برمجية) — CRUD فعلي في cash_boxes.</summary>
    public class CashBoxEditor : Window
    {
        private readonly int _id;
        private readonly TextBox _txtName = new TextBox { Margin = new Thickness(5) };
        private readonly TextBox _txtCode = new TextBox { Margin = new Thickness(5) };
        private readonly TextBox _txtOpening = new TextBox { Margin = new Thickness(5), Text = "0" };
        private readonly ComboBox _cmbType = new ComboBox { Margin = new Thickness(5), Width = 120 };
        private readonly ComboBox _cmbAccount = new ComboBox { Margin = new Thickness(5), Width = 170, DisplayMemberPath = "Name" };

        public CashBoxEditor(int id)
        {
            _id = id;
            Title = id > 0 ? "تعديل خزينة" : "إضافة خزينة";
            Width = 430; Height = 400; WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FlowDirection = FlowDirection.RightToLeft;
            _cmbType.Items.Add("CASH");
            _cmbType.Items.Add("BANK");
            _cmbType.SelectedIndex = 0;
            LoadAccounts();

            var panel = new StackPanel { Margin = new Thickness(10) };
            panel.Children.Add(new TextBlock { Text = "الاسم:" }); panel.Children.Add(_txtName);
            panel.Children.Add(new TextBlock { Text = "الكود (يُولَّد تلقائيًا إن تُرك فارغًا):" }); panel.Children.Add(_txtCode);
            panel.Children.Add(new TextBlock { Text = "النوع:" }); panel.Children.Add(_cmbType);
            panel.Children.Add(new TextBlock { Text = "رصيد افتتاحي:" }); panel.Children.Add(_txtOpening);
            panel.Children.Add(new TextBlock { Text = "الحساب المرتبط (إلزامي):" }); panel.Children.Add(_cmbAccount);
            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
            var btnSave = new Button { Content = "حفظ", Padding = new Thickness(15, 5, 15, 5), Margin = new Thickness(0, 0, 5, 0) };
            var btnCancel = new Button { Content = "إلغاء", Padding = new Thickness(15, 5, 15, 5) };
            btnSave.Click += BtnSave_Click;
            btnCancel.Click += (s, e) => Close();
            btns.Children.Add(btnSave); btns.Children.Add(btnCancel);
            panel.Children.Add(btns);
            Content = panel;
            if (_id > 0) LoadData();
        }

        /// <summary>P4: cash_boxes.account_id إلزامي (NOT NULL) — نحمّل حسابات الأصول لاختياره.</summary>
        private void LoadAccounts()
        {
            try
            {
                using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
                conn.Open();
                _cmbAccount.Items.Clear();
                using var cmd = new NpgsqlCommand("SELECT id, name FROM accounts WHERE UPPER(COALESCE(type,''))='ASSET' ORDER BY name", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read()) _cmbAccount.Items.Add(new ComboItem { Id = r.GetInt32(0), Name = r.GetString(1) });
                if (_cmbAccount.Items.Count > 0) _cmbAccount.SelectedIndex = 0;
                else MessageBox.Show("لا توجد حسابات أصول. أضف حسابًا من صفحة دليل الحسابات أولًا.", "تنبيه");
            }
            catch (Exception ex) { MessageBox.Show("خطأ تحميل الحسابات: " + ex.Message); }
        }

        private void LoadData()
        {
            try
            {
                using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
                conn.Open();
                using var cmd = new NpgsqlCommand("SELECT name, COALESCE(code,''), COALESCE(type,'CASH'), COALESCE(opening_balance,0), COALESCE(account_id,0) FROM cash_boxes WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("id", _id);
                using var r = cmd.ExecuteReader();
                if (r.Read())
                {
                    _txtName.Text = r.IsDBNull(0) ? "" : r.GetString(0);
                    _txtCode.Text = r.IsDBNull(1) ? "" : r.GetString(1);
                    var t = r.IsDBNull(2) ? "CASH" : r.GetString(2);
                    _cmbType.SelectedIndex = _cmbType.Items.Contains(t) ? _cmbType.Items.IndexOf(t) : 0;
                    _txtOpening.Text = Convert.ToDecimal(r.GetValue(3)).ToString("0.00");
                    var accId = Convert.ToInt32(r.GetValue(4));
                    for (int i = 0; i < _cmbAccount.Items.Count; i++)
                        if (((ComboItem)_cmbAccount.Items[i]).Id == accId) { _cmbAccount.SelectedIndex = i; break; }
                }
            }
            catch (Exception ex) { MessageBox.Show("خطأ: " + ex.Message); }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtName.Text)) { MessageBox.Show("الاسم مطلوب"); return; }
            if (!decimal.TryParse(_txtOpening.Text, out var opening)) { MessageBox.Show("رصيد غير صحيح"); return; }
            if (_cmbAccount.SelectedItem is not ComboItem acc) { MessageBox.Show("اختر الحساب المرتبط بالخزينة (إلزامي)"); return; }
            var type = _cmbType.SelectedItem?.ToString() ?? "CASH";
            try
            {
                using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
                conn.Open();
                using var tx = conn.BeginTransaction();
                if (_id > 0)
                {
                    using var cmd = new NpgsqlCommand("UPDATE cash_boxes SET name=@n, code=@c, type=@t, opening_balance=@o, account_id=@acc WHERE id=@id", conn, tx);
                    cmd.Parameters.AddWithValue("id", _id); cmd.Parameters.AddWithValue("n", _txtName.Text.Trim());
                    cmd.Parameters.AddWithValue("c", ResolveCode(conn, tx));
                    cmd.Parameters.AddWithValue("t", type);
                    cmd.Parameters.AddWithValue("o", opening); cmd.Parameters.AddWithValue("acc", acc.Id); cmd.ExecuteNonQuery();
                }
                else
                {
                    using var cmd = new NpgsqlCommand("INSERT INTO cash_boxes (name, code, type, opening_balance, is_active, created_at, account_id) VALUES (@n,@c,@t,@o,TRUE,NOW(),@acc) RETURNING id", conn, tx);
                    cmd.Parameters.AddWithValue("n", _txtName.Text.Trim());
                    cmd.Parameters.AddWithValue("c", ResolveCode(conn, tx));
                    cmd.Parameters.AddWithValue("t", type);
                    cmd.Parameters.AddWithValue("o", opening); cmd.Parameters.AddWithValue("acc", acc.Id);
                    var newId = Convert.ToInt32(cmd.ExecuteScalar());
                    using var up = new NpgsqlCommand("UPDATE cash_boxes SET code=@c2 WHERE id=@id", conn, tx);
                    up.Parameters.AddWithValue("@c2", "CB" + newId.ToString("000"));
                    up.Parameters.AddWithValue("@id", newId);
                    up.ExecuteNonQuery();
                }
                tx.Commit();
                AuditService.Log(_id > 0 ? "UPDATE" : "CREATE", "CashBox", _id, (string.IsNullOrWhiteSpace(_txtName.Text) ? "" : _txtName.Text.Trim()));
                DialogResult = true; Close();
            }
            catch (Exception ex) { MessageBox.Show("خطأ: " + ex.Message); }
        }

        /// <summary>P4: cash_boxes.code إلزامي (NOT NULL) — نولّد كودًا فريدًا إذا تُرك فارغًا.</summary>
        private object ResolveCode(NpgsqlConnection conn, NpgsqlTransaction tx)
        {
            if (!string.IsNullOrWhiteSpace(_txtCode.Text)) return _txtCode.Text.Trim();
            using var probe = new NpgsqlCommand("SELECT COALESCE(MAX(id),0) FROM cash_boxes", conn, tx);
            var maxId = Convert.ToInt32(probe.ExecuteScalar());
            return "CB" + (maxId + 1).ToString("000");
        }
    }
}