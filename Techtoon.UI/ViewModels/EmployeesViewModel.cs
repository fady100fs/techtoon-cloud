using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Npgsql;

namespace Techtoon.UI.ViewModels
{
    public class EmployeeRow : ViewModelBase
    {
        public int Id { get; set; }
        public string EmployeeCode { get; set; }
        public string FullName { get; set; }
        public string JobTitle { get; set; }
        public string Department { get; set; }
        public string Phone { get; set; }
        public decimal BasicSalary { get; set; }
        public string StatusDisplay { get; set; }
    }

    public class EmployeesViewModel : ViewModelBase
    {
        public ObservableCollection<EmployeeRow> Employees { get; } = new();
        private EmployeeRow _selectedItem;
        public EmployeeRow SelectedItem { get => _selectedItem; set => SetProperty(ref _selectedItem, value); }
        private string _searchText = "";
        public string SearchText { get => _searchText; set { SetProperty(ref _searchText, value); LoadEmployees(); } }
        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public ICommand RefreshCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public EmployeesViewModel()
        {
            RefreshCommand = new RelayCommand(LoadEmployees);
            AddCommand = new RelayCommand(() => OpenEditWindow(0));
            EditCommand = new RelayCommand(() => OpenEditWindow(SelectedItem?.Id ?? 0), () => SelectedItem != null);
            DeleteCommand = new RelayCommand(DeleteEmployee, () => SelectedItem != null);
            LoadEmployees();
        }

        public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();

        public void LoadEmployees()
        {
            Employees.Clear();
            try {
                using var conn = new NpgsqlConnection(GetCS()); conn.Open();
                string sql = @"SELECT id, COALESCE(employee_code,''), full_name, COALESCE(job_title,''), COALESCE(department,''), COALESCE(phone,''), COALESCE(basic_salary,0), COALESCE(status::text,'active') FROM employees WHERE 1=1";
                if (!string.IsNullOrWhiteSpace(SearchText)) sql += " AND (full_name ILIKE @q OR employee_code ILIKE @q)";
                sql += " ORDER BY full_name LIMIT 500";
                using var cmd = new NpgsqlCommand(sql, conn);
                if (!string.IsNullOrWhiteSpace(SearchText)) cmd.Parameters.AddWithValue("q", "%" + SearchText + "%");
                using var r = cmd.ExecuteReader();
                while (r.Read()) Employees.Add(new EmployeeRow {
                    Id = r.GetInt32(0), EmployeeCode = r.IsDBNull(1) ? "" : r.GetString(1),
                    FullName = r.IsDBNull(2) ? "" : r.GetString(2), JobTitle = r.IsDBNull(3) ? "" : r.GetString(3),
                    Department = r.IsDBNull(4) ? "" : r.GetString(4), Phone = r.IsDBNull(5) ? "" : r.GetString(5),
                    BasicSalary = r.IsDBNull(6) ? 0 : (decimal)r.GetDouble(6),
                    StatusDisplay = ParseStatus(r.IsDBNull(7) ? "active" : r.GetString(7))
                });
                StatusMessage = "Loaded " + Employees.Count;
            } catch (Exception ex) { StatusMessage = "Err: " + ex.Message; }
        }

        private static string ParseStatus(string val) => (val == "active" || val == "1") ? "Active" : "Inactive";

        private void OpenEditWindow(int id) { try { new Views.EmployeesEditWindow(id).ShowDialog(); LoadEmployees(); } catch { } }

        private void DeleteEmployee()
        {
            if (SelectedItem == null) return;
            int id = SelectedItem.Id;
            try {
                using var conn = new NpgsqlConnection(GetCS()); conn.Open();
                var items = new System.Collections.Generic.List<Views.DependencyItem>();
                
                // فحص المرتبات
                using (var cmdCheck = new NpgsqlCommand("SELECT id FROM salary_records WHERE employee_id = @id LIMIT 10", conn)) {
                    cmdCheck.Parameters.AddWithValue("id", id);
                    using (var rCheck = cmdCheck.ExecuteReader())
                        while (rCheck.Read()) items.Add(new Views.DependencyItem { Type = "Salary", Display = $"Salary ID: {rCheck.GetInt32(0)}", TargetWindow = "SalariesEditWindow", TargetId = rCheck.GetInt32(0) });
                }
                // فحص السلف
                using (var cmdCheck = new NpgsqlCommand("SELECT id FROM loans WHERE borrower_name IN (SELECT full_name FROM employees WHERE id = @id) LIMIT 10", conn)) {
                    cmdCheck.Parameters.AddWithValue("id", id);
                    using (var rCheck = cmdCheck.ExecuteReader())
                        while (rCheck.Read()) items.Add(new Views.DependencyItem { Type = "Loan", Display = $"Loan ID: {rCheck.GetInt32(0)}", TargetWindow = "LoansEditWindow", TargetId = rCheck.GetInt32(0) });
                }
                
                if (items.Count > 0) {
                    var win = new Views.DependenciesWindow(items);
                    if (win.ShowDialog() == true && win.SelectedItem != null) OpenDependencyWindow?.Invoke(win.SelectedItem.TargetWindow, win.SelectedItem.TargetId);
                    return;
                }
                
                if (System.Windows.MessageBox.Show("Confirm Delete?", "Confirm", System.Windows.MessageBoxButton.YesNo) != System.Windows.MessageBoxResult.Yes) return;
                using var cmd = new NpgsqlCommand("DELETE FROM employees WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("id", id); cmd.ExecuteNonQuery(); LoadEmployees();
            } catch (Exception ex) { StatusMessage = "Err: " + ex.Message; }
        }
    }
}