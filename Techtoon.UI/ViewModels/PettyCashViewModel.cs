using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Npgsql;

namespace Techtoon.UI.ViewModels
{
    public class CustodyRow : ViewModelBase
    {
        public int Id { get; set; }
        public string CustodyNumber { get; set; }
        public string EmployeeName { get; set; }
        public decimal Amount { get; set; }
        public DateTime IssuedDate { get; set; }
        public string Status { get; set; }
        public string StatusDisplay => Status switch
        {
            "open" => "مفتوحة",
            "partially_settled" => "مسواة جزئياً",
            "settled" => "مسواة",
            "overdue" => "متأخرة",
            "cancelled" => "ملغاة",
            _ => Status
        };
    }

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

        public PettyCashViewModel()
        {
            RefreshCommand = new RelayCommand(LoadCustodies);
            AddCommand = new RelayCommand(() => OpenEditWindow(0));
            EditCommand = new RelayCommand(() => OpenEditWindow(SelectedItem?.Id ?? 0), () => SelectedItem != null);
            DeleteCommand = new RelayCommand(DeleteCustody, () => SelectedItem != null);
            LoadCustodies();
        }

        public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();

        public void LoadCustodies()
        {
            Custodies.Clear();
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                string sql = @"SELECT pc.id, COALESCE(pc.custody_number,''), e.full_name, pc.amount, pc.issued_date, pc.status
                    FROM petty_cash_custodies pc
                    LEFT JOIN employees e ON e.id = pc.employee_id
                    WHERE 1=1";
                if (!string.IsNullOrWhiteSpace(SearchText))
                    sql += " AND (pc.custody_number ILIKE @q OR e.full_name ILIKE @q)";
                sql += " ORDER BY pc.issued_date DESC LIMIT 500";
                using var cmd = new NpgsqlCommand(sql, conn);
                if (!string.IsNullOrWhiteSpace(SearchText))
                    cmd.Parameters.AddWithValue("q", "%" + SearchText + "%");
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    Custodies.Add(new CustodyRow
                    {
                        Id = r.GetInt32(0),
                        CustodyNumber = r.IsDBNull(1) ? "" : r.GetString(1),
                        EmployeeName = r.IsDBNull(2) ? "" : r.GetString(2),
                        Amount = r.IsDBNull(3) ? 0 : (decimal)r.GetDouble(3),
                        IssuedDate = r.IsDBNull(4) ? DateTime.MinValue : Techtoon.Core.Data.DbValue.AsDateTime(r.GetValue(4)),
                        Status = r.IsDBNull(5) ? "" : r.GetString(5)
                    });
                StatusMessage = "تم تحميل " + Custodies.Count + " عهدة";
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
    }
}