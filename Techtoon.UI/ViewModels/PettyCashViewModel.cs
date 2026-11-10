using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Npgsql;
using Techtoon.UI.Services;

namespace Techtoon.UI.ViewModels
{
    public class CustodyRow : ViewModelBase
    {
        public int Id { get; set; }
        public string CustodyNumber { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public decimal Amount { get; set; }
        public decimal SpentAmount { get; set; }
        public DateTime IssuedDate { get; set; }
        public string Status { get; set; }
        public int CashBoxId { get; set; }
        public decimal Remaining => Amount - SpentAmount;
        public string StatusDisplay => Status switch
        {
            "open" => "مفتوحة",
            "partially_settled" => "مسددة جزئيًا",
            "settled" => "مسددة",
            "overdue" => "متأخرة",
            "cancelled" => "ملغاة",
            _ => Status ?? ""
        };
        public string AmountDisplay => Amount.ToString("N2");
        public string SpentDisplay => SpentAmount.ToString("N2");
        public string RemainingDisplay => Remaining.ToString("N2");
        public string DateDisplay => IssuedDate.ToString("yyyy-MM-dd");
    }

    /// <summary>P5: إدارة العهد النثرية — issuance + expenses + settlement + increase requests.</summary>
    public class PettyCashViewModel : ViewModelBase
    {
        public ObservableCollection<CustodyRow> Custodies { get; } = new();

        private CustodyRow _selectedItem;
        public CustodyRow SelectedItem { get => _selectedItem; set => SetProperty(ref _selectedItem, value); }
        private string _searchText = "";
        public string SearchText { get => _searchText; set { SetProperty(ref _searchText, value); LoadCustodies(); } }
        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public ICommand RefreshCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ExpenseCommand { get; }
        public ICommand SettleCommand { get; }
        public ICommand IncreaseCommand { get; }

        public PettyCashViewModel()
        {
            RefreshCommand = new RelayCommand(LoadCustodies);
            AddCommand = new RelayCommand(() => OpenEditWindow(0));
            EditCommand = new RelayCommand(() => OpenEditWindow(SelectedItem?.Id ?? 0), () => SelectedItem != null);
            DeleteCommand = new RelayCommand(DeleteCustody, () => SelectedItem != null);
            ExpenseCommand = new RelayCommand(AddExpense, () => SelectedItem != null);
            SettleCommand = new RelayCommand(SettleCustody, () => SelectedItem != null);
            IncreaseCommand = new RelayCommand(RequestIncrease, () => SelectedItem != null);
            LoadCustodies();
        }

        public static string GetCS() => SecureConnectionManager.GetConnectionString();

        public void LoadCustodies()
        {
            // P5: الحفاظ على التحديد بعد إعادة التحميل (وإلا تعطّلت الأزرار المعتمدة على الصف المحدد)
            var keepId = SelectedItem?.Id ?? 0;
            Custodies.Clear();
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                string sql = @"SELECT pc.id, COALESCE(pc.custody_number,'عهدة #' || pc.id), pc.employee_id,
                    COALESCE(e.full_name,''), COALESCE(pc.amount,0),
                    COALESCE((SELECT SUM(pe.amount) FROM petty_cash_expenses pe WHERE pe.custody_id = pc.id AND pe.status <> 'rejected'),0),
                    pc.issued_date, COALESCE(pc.status,'open'), COALESCE(pc.cash_box_id,0)
                    FROM petty_cash_custodies pc
                    LEFT JOIN employees e ON e.id = pc.employee_id
                    WHERE 1=1";
                if (!string.IsNullOrWhiteSpace(SearchText))
                    sql += " AND (pc.custody_number ILIKE @q OR e.full_name ILIKE @q)";
                sql += " ORDER BY pc.issued_date DESC LIMIT 500";
                using var cmd = new NpgsqlCommand(sql, conn);
                if (!string.IsNullOrWhiteSpace(SearchText)) cmd.Parameters.AddWithValue("q", "%" + SearchText + "%");
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    Custodies.Add(new CustodyRow
                    {
                        Id = r.GetInt32(0),
                        CustodyNumber = r.GetString(1),
                        EmployeeId = r.IsDBNull(2) ? 0 : r.GetInt32(2),
                        EmployeeName = r.GetString(3),
                        Amount = Convert.ToDecimal(r.GetValue(4)),
                        SpentAmount = Convert.ToDecimal(r.GetValue(5)),
                        IssuedDate = Techtoon.Core.Data.DbValue.AsDateTime(r.GetValue(6)),
                        Status = r.GetString(7),
                        CashBoxId = r.GetInt32(8)
                    });
                StatusMessage = "تم التحميل: " + Custodies.Count + " عهدة";
                if (keepId > 0)
                {
                    var keep = Custodies.FirstOrDefault(c => c.Id == keepId);
                    if (keep != null) SelectedItem = keep;
                }
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        private void OpenEditWindow(int id)
        {
            try
            {
                var win = new Views.PettyCashEditWindow(id);
                win.ShowDialog();
                LoadCustodies();
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        private void DeleteCustody()
        {
            if (SelectedItem == null) return;
            if (SelectedItem.SpentAmount > 0)
            {
                StatusMessage = "لا يمكن الحذف: توجد مصروفات على العهدة — استخدم التسوية بدلًا من الحذف.";
                return;
            }
            if (System.Windows.MessageBox.Show("تأكيد الحذف؟", "تأكيد", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning) != System.Windows.MessageBoxResult.Yes) return;
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var cmd = new NpgsqlCommand("DELETE FROM petty_cash_custodies WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("id", SelectedItem.Id);
                cmd.ExecuteNonQuery();
                StatusMessage = "تم الحذف";
                LoadCustodies();
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        /// <summary>P5: تسجيل مصروف على العهدة.</summary>
        private void AddExpense()
        {
            if (SelectedItem == null) return;
            try
            {
                var win = new Views.CustodyExpenseWindow(SelectedItem.Id, SelectedItem.CustodyNumber, SelectedItem.Amount, SelectedItem.SpentAmount);
                if (win.ShowDialog() == true) { StatusMessage = "تم تسجيل المصروف"; LoadCustodies(); }
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        /// <summary>P5: تسوية العهدة (إرجاع النقد للخزينة + إثبات المصروفات).</summary>
        private void SettleCustody()
        {
            if (SelectedItem == null) return;
            if (SelectedItem.Status == "settled") { StatusMessage = "العهدة مسددة بالفعل."; return; }
            try
            {
                var win = new Views.CustodySettlementWindow(SelectedItem.Id, SelectedItem.CustodyNumber, SelectedItem.Amount, SelectedItem.SpentAmount, SelectedItem.CashBoxId);
                if (win.ShowDialog() == true) { StatusMessage = "تم اعتماد التسوية"; LoadCustodies(); }
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        /// <summary>P5: طلب زيادة عهدة.</summary>
        private void RequestIncrease()
        {
            if (SelectedItem == null) return;
            try
            {
                var win = new Views.CustodyIncreaseRequestWindow(SelectedItem.Id, SelectedItem.CustodyNumber, SelectedItem.Amount);
                if (win.ShowDialog() == true) { StatusMessage = "تم إرسال طلب الزيادة"; LoadCustodies(); }
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }
    }
}
