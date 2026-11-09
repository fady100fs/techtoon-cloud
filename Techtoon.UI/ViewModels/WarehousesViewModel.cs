using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Npgsql;

namespace Techtoon.UI.ViewModels
{
    public class WarehouseRow : ViewModelBase
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public string ResponsiblePerson { get; set; }
        public bool IsActive { get; set; }
        private bool _isSelected;
        public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
    }

    public class WarehousesViewModel : ViewModelBase
    {
        public ObservableCollection<WarehouseRow> Warehouses { get; } = new();

        private WarehouseRow _selectedItem;
        public WarehouseRow SelectedItem { get => _selectedItem; set => SetProperty(ref _selectedItem, value); }

        private string _searchText = "";
        public string SearchText { get => _searchText; set { SetProperty(ref _searchText, value); LoadWarehouses(); } }
        
        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public ICommand RefreshCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public WarehousesViewModel()
        {
            RefreshCommand = new RelayCommand(LoadWarehouses);
            AddCommand = new RelayCommand(() => OpenEditWindow(0));
            EditCommand = new RelayCommand(() => OpenEditWindow(SelectedItem?.Id ?? 0), () => SelectedItem != null);
            DeleteCommand = new RelayCommand(DeleteWarehouse, () => SelectedItem != null);
            LoadWarehouses();
        }

        public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();

        public void LoadWarehouses()
        {
            Warehouses.Clear();
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                string sql = @"SELECT id, code, name, COALESCE(location,''), COALESCE(responsible_person,''), COALESCE(is_active, TRUE) FROM warehouses ORDER BY name LIMIT 500";
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    sql = sql.Replace("ORDER BY", " AND (name ILIKE @q OR code ILIKE @q) ORDER BY");
                }
                using var cmd = new NpgsqlCommand(sql, conn);
                if (!string.IsNullOrWhiteSpace(SearchText))
                    cmd.Parameters.AddWithValue("q", "%" + SearchText + "%");
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    Warehouses.Add(new WarehouseRow
                    {
                        Id = r.GetInt32(0),
                        Code = r.IsDBNull(1) ? "" : r.GetString(1),
                        Name = r.IsDBNull(2) ? "" : r.GetString(2),
                        Location = r.IsDBNull(3) ? "" : r.GetString(3),
                        ResponsiblePerson = r.IsDBNull(4) ? "" : r.GetString(4),
                        IsActive = r.IsDBNull(5) ? true : r.GetBoolean(5)
                    });
                StatusMessage = "تم تحميل " + Warehouses.Count + " سجل";
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        private void OpenEditWindow(int id)
        {
            try
            {
                var win = new Views.WarehousesEditWindow(id);
                win.ShowDialog();
                LoadWarehouses();
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        private void DeleteWarehouse()
        {
            if (SelectedItem == null) return;
            int id = SelectedItem.Id;
            
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                
                var items = new System.Collections.Generic.List<Views.DependencyItem>();
                
                using (var cmdCheck = new NpgsqlCommand("SELECT item_id FROM stock_levels WHERE warehouse_id = @id LIMIT 10", conn))
                {
                    cmdCheck.Parameters.AddWithValue("id", id);
                    using (var rCheck = cmdCheck.ExecuteReader())
                    {
                        while (rCheck.Read())
                        {
                            items.Add(new Views.DependencyItem { Type = "رصيد مخزون", Display = $"صنف مرتبط بالمخزون - ID: {rCheck.GetInt32(0)}", TargetWindow = "ItemEditWindow", TargetId = rCheck.GetInt32(0) });
                        }
                    }
                }
                
                using (var cmdCheck = new NpgsqlCommand("SELECT transfer_number, id FROM warehouse_transfers WHERE from_warehouse_id = @id OR to_warehouse_id = @id LIMIT 10", conn))
                {
                    cmdCheck.Parameters.AddWithValue("id", id);
                    using (var rCheck = cmdCheck.ExecuteReader())
                    {
                        while (rCheck.Read())
                        {
                            string val = rCheck.IsDBNull(0) ? "" : rCheck.GetValue(0).ToString();
                            items.Add(new Views.DependencyItem { Type = "تحويل مخزون", Display = $"تحويل مخزون: {val}", TargetWindow = "WarehouseTransfersEditWindow", TargetId = rCheck.GetInt32(1) });
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
                
                using var cmd = new NpgsqlCommand("DELETE FROM warehouses WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("id", id);
                cmd.ExecuteNonQuery();
                
                StatusMessage = "تم الحذف بنجاح";
                LoadWarehouses();
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

