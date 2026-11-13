using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Npgsql;

namespace Techtoon.UI.ViewModels
{
    public class SalaryRow : ViewModelBase
    {
        public int Id { get; set; }
        public string EmployeeName { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal NetSalary { get; set; }
        public DateTime PaymentDate { get; set; }
        public string StatusDisplay { get; set; }
    }

    public class SalariesViewModel : ViewModelBase
    {
        public ObservableCollection<SalaryRow> Salaries { get; } = new();
        private SalaryRow _selectedItem;
        public SalaryRow SelectedItem { get => _selectedItem; set => SetProperty(ref _selectedItem, value); }
        private string _searchText = "";
        public string SearchText { get => _searchText; set { SetProperty(ref _searchText, value); LoadSalaries(); } }
        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public ICommand RefreshCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public SalariesViewModel()
        {
            RefreshCommand = new RelayCommand(LoadSalaries);
            AddCommand = new RelayCommand(() => OpenEditWindow(0));
            EditCommand = new RelayCommand(() => OpenEditWindow(SelectedItem?.Id ?? 0), () => SelectedItem != null);
            DeleteCommand = new RelayCommand(DeleteSalary, () => SelectedItem != null);
            LoadSalaries();
        }

        public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();

        public void LoadSalaries()
        {
            Salaries.Clear();
            try {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                string sql = @"SELECT sr.id, COALESCE(e.full_name,''), sr.month, sr.year, sr.net_salary, sr.payment_date, COALESCE(sr.status,'paid')
                    FROM salary_records sr LEFT JOIN employees e ON e.id = sr.employee_id WHERE 1=1";
                // البحث يُضاف مباشرةً — كان الاستبدال على "ORDER BY" لا يعمل
                // (السلسلة الأساسية لا تحتوي ORDER BY) فكان البحث يُتجاهل بصمت.
                if (!string.IsNullOrWhiteSpace(SearchText))
                    sql += " AND e.full_name ILIKE @q";
                sql += " ORDER BY sr.payment_date DESC LIMIT 500";
                using var cmd = new NpgsqlCommand(sql, conn);
                if (!string.IsNullOrWhiteSpace(SearchText)) cmd.Parameters.AddWithValue("q", "%" + SearchText + "%");
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    Salaries.Add(new SalaryRow {
                        Id = r.GetInt32(0), EmployeeName = r.IsDBNull(1) ? "" : r.GetString(1),
                        Month = r.GetInt32(2), Year = r.GetInt32(3),
                        NetSalary = r.IsDBNull(4) ? 0 : (decimal)r.GetDouble(4),
                        PaymentDate = r.IsDBNull(5) ? DateTime.MinValue : r.GetDateTime(5),
                        StatusDisplay = r.IsDBNull(6) ? "" : r.GetString(6)
                    });
                StatusMessage = "Loaded " + Salaries.Count;
            } catch (Exception ex) { StatusMessage = "Err: " + ex.Message; }
        }

        private void OpenEditWindow(int id) { try { new Views.SalariesEditWindow(id).ShowDialog(); LoadSalaries(); } catch { } }
        private void DeleteSalary() {
            if (SelectedItem == null) return;
            if (System.Windows.MessageBox.Show("Confirm Delete?", "Confirm", System.Windows.MessageBoxButton.YesNo) != System.Windows.MessageBoxResult.Yes) return;
            try { using var conn = new NpgsqlConnection(GetCS()); conn.Open();
                using var cmd = new NpgsqlCommand("DELETE FROM salary_records WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("id", SelectedItem.Id); cmd.ExecuteNonQuery(); LoadSalaries();
            } catch (Exception ex) { StatusMessage = "Err: " + ex.Message; }
        }
    }
}