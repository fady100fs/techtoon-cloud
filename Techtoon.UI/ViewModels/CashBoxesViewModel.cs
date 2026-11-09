using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Npgsql;
using Techtoon.UI.Services;

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

        public CashBoxesViewModel()
        {
            CashBoxes = new ObservableCollection<CashBoxRow>();
            RefreshCommand = new UiRelayCommand(_ => _ = LoadAsync());
            AddCommand = new UiRelayCommand(_ => AddBox());
            EditCommand = new UiRelayCommand(_ => EditBox(), _ => SelectedRow != null);
            DeleteCommand = new UiRelayCommand(_ => DeleteBox(), _ => SelectedRow != null);
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
        private readonly TextBox _txtType = new TextBox { Margin = new Thickness(5), Text = "CASH" };
        private readonly TextBox _txtStatus = new TextBox { Margin = new Thickness(5), Text = "" };

        public CashBoxEditor(int id)
        {
            _id = id;
            Title = id > 0 ? "تعديل خزينة" : "إضافة خزينة";
            Width = 380; Height = 320; WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FlowDirection = FlowDirection.RightToLeft;
            var panel = new StackPanel { Margin = new Thickness(10) };
            panel.Children.Add(new TextBlock { Text = "الاسم:" }); panel.Children.Add(_txtName);
            panel.Children.Add(new TextBlock { Text = "الكود:" }); panel.Children.Add(_txtCode);
            panel.Children.Add(new TextBlock { Text = "النوع (CASH/BANK):" }); panel.Children.Add(_txtType);
            panel.Children.Add(new TextBlock { Text = "رصيد افتتاحي:" }); panel.Children.Add(_txtOpening);
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

        private void LoadData()
        {
            try
            {
                using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
                conn.Open();
                using var cmd = new NpgsqlCommand("SELECT name, COALESCE(code,''), COALESCE(type,'CASH'), COALESCE(opening_balance,0) FROM cash_boxes WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("id", _id);
                using var r = cmd.ExecuteReader();
                if (r.Read()) { _txtName.Text = r.IsDBNull(0) ? "" : r.GetString(0); _txtCode.Text = r.IsDBNull(1) ? "" : r.GetString(1); _txtType.Text = r.IsDBNull(2) ? "" : r.GetString(2); _txtOpening.Text = Convert.ToDecimal(r.GetValue(3)).ToString("0.00"); }
            }
            catch (Exception ex) { MessageBox.Show("خطأ: " + ex.Message); }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtName.Text)) { MessageBox.Show("الاسم مطلوب"); return; }
            if (!decimal.TryParse(_txtOpening.Text, out var opening)) { MessageBox.Show("رصيد غير صحيح"); return; }
            try
            {
                using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
                conn.Open();
                using var tx = conn.BeginTransaction();
                if (_id > 0)
                {
                    using var cmd = new NpgsqlCommand("UPDATE cash_boxes SET name=@n, code=@c, type=@t, opening_balance=@o WHERE id=@id", conn, tx);
                    cmd.Parameters.AddWithValue("id", _id); cmd.Parameters.AddWithValue("n", _txtName.Text.Trim());
                    cmd.Parameters.AddWithValue("c", string.IsNullOrWhiteSpace(_txtCode.Text) ? (object)DBNull.Value : _txtCode.Text.Trim());
                    cmd.Parameters.AddWithValue("t", string.IsNullOrWhiteSpace(_txtType.Text) ? "CASH" : _txtType.Text.Trim().ToUpper());
                    cmd.Parameters.AddWithValue("o", opening); cmd.ExecuteNonQuery();
                }
                else
                {
                    using var cmd = new NpgsqlCommand("INSERT INTO cash_boxes (name, code, type, opening_balance, is_active, created_at) VALUES (@n,@c,@t,@o,TRUE,NOW()) RETURNING id", conn, tx);
                    cmd.Parameters.AddWithValue("n", _txtName.Text.Trim());
                    cmd.Parameters.AddWithValue("c", string.IsNullOrWhiteSpace(_txtCode.Text) ? (object)DBNull.Value : _txtCode.Text.Trim());
                    cmd.Parameters.AddWithValue("t", string.IsNullOrWhiteSpace(_txtType.Text) ? "CASH" : _txtType.Text.Trim().ToUpper());
                    cmd.Parameters.AddWithValue("o", opening); cmd.ExecuteScalar();
                }
                tx.Commit();
                DialogResult = true; Close();
            }
            catch (Exception ex) { MessageBox.Show("خطأ: " + ex.Message); }
        }
    }
}