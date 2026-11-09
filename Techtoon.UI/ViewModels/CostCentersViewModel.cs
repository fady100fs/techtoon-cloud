using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Npgsql;

namespace Techtoon.UI.ViewModels
{
    public class CostCenterRow : ViewModelBase
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string ManagerName { get; set; }
        public decimal BudgetLimit { get; set; }
        public bool IsActive { get; set; }
        private bool _isSelected;
        public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
    }

    public class CostCentersViewModel : ViewModelBase
    {
        public ObservableCollection<CostCenterRow> CostCenters { get; } = new();

        private CostCenterRow _selectedItem;
        public CostCenterRow SelectedItem { get => _selectedItem; set => SetProperty(ref _selectedItem, value); }

        private string _searchText = "";
        public string SearchText { get => _searchText; set { SetProperty(ref _searchText, value); LoadCostCenters(); } }
        
        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public ICommand RefreshCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public CostCentersViewModel()
        {
            RefreshCommand = new RelayCommand(LoadCostCenters);
            AddCommand = new RelayCommand(() => OpenEditWindow(0));
            EditCommand = new RelayCommand(() => OpenEditWindow(SelectedItem?.Id ?? 0), () => SelectedItem != null);
            DeleteCommand = new RelayCommand(DeleteCostCenter, () => SelectedItem != null);
            LoadCostCenters();
        }

        public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();

        public void LoadCostCenters()
        {
            CostCenters.Clear();
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                string sql = @"SELECT id, code, name, COALESCE(manager_name,''), COALESCE(budget_limit,0), COALESCE(is_active, TRUE) FROM cost_centers ORDER BY name LIMIT 500";
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    sql = sql.Replace("ORDER BY", " AND (name ILIKE @q OR code ILIKE @q) ORDER BY");
                }
                using var cmd = new NpgsqlCommand(sql, conn);
                if (!string.IsNullOrWhiteSpace(SearchText))
                    cmd.Parameters.AddWithValue("q", "%" + SearchText + "%");
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    CostCenters.Add(new CostCenterRow
                    {
                        Id = r.GetInt32(0),
                        Code = r.IsDBNull(1) ? "" : r.GetString(1),
                        Name = r.IsDBNull(2) ? "" : r.GetString(2),
                        ManagerName = r.IsDBNull(3) ? "" : r.GetString(3),
                        BudgetLimit = r.IsDBNull(4) ? 0 : (decimal)r.GetDouble(4),
                        IsActive = r.IsDBNull(5) ? true : r.GetBoolean(5)
                    });
                StatusMessage = "تم تحميل " + CostCenters.Count + " سجل";
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        private void OpenEditWindow(int id)
        {
            try
            {
                var win = new Views.CostCentersEditWindow(id);
                win.ShowDialog();
                LoadCostCenters();
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        private void DeleteCostCenter()
        {
            if (SelectedItem == null) return;
            int id = SelectedItem.Id;
            
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                
                var items = new System.Collections.Generic.List<Views.DependencyItem>();
                
                using (var cmdCheck = new NpgsqlCommand("SELECT id FROM journal_lines WHERE cost_center_id = @id LIMIT 10", conn))
                {
                    cmdCheck.Parameters.AddWithValue("id", id);
                    using (var rCheck = cmdCheck.ExecuteReader())
                    {
                        while (rCheck.Read())
                        {
                            items.Add(new Views.DependencyItem { Type = "قيد يومية", Display = $"بند قيد يومية: {rCheck.GetInt32(1)}", TargetWindow = "Accounts", TargetId = rCheck.GetInt32(0) });
                        }
                    }
                }
                
                if (items.Count > 0)
                {
                    var win = new Views.DependenciesWindow(items);
                    if (win.ShowDialog() == true && win.SelectedItem != null)
                    {
                        OpenDependencyWindow?.Invoke(win.SelectedItem.TargetWindow, win.SelectedItem.TargetId);
                    }
                    return;
                }
                
                if (System.Windows.MessageBox.Show("تأكيد الحذف؟", "تأكيد", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning) != System.Windows.MessageBoxResult.Yes) return;
                
                using var cmd = new NpgsqlCommand("DELETE FROM cost_centers WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("id", id);
                cmd.ExecuteNonQuery();
                
                StatusMessage = "تم الحذف بنجاح";
                LoadCostCenters();
            }
            catch (Npgsql.PostgresException px)
            {
                System.Windows.MessageBox.Show("خطأ في قاعدة البيانات: " + px.Message, "خطأ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            catch (Exception ex) 
            { 
                System.Windows.MessageBox.Show("خطأ: " + ex.Message, "خطأ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error); 
            }
        }
    }
}

