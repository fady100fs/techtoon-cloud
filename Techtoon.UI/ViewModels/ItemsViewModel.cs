using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Npgsql;
using Techtoon.Core.Models;

namespace Techtoon.UI.ViewModels
{
    public class ItemRow : ViewModelBase
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Barcode { get; set; }
        public decimal SellPrice { get; set; }
        public decimal CostPrice { get; set; }
        public decimal Stock { get; set; }
        public decimal MinStock { get; set; }
        public bool IsActive { get; set; }
        public bool IsKit { get; set; }
        public int KitComponentsCount { get; set; }
        private bool _isSelected;
        public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
        public string StockDisplay => Stock.ToString("N2");
        public string SellPriceDisplay => SellPrice.ToString("N2");
        public string CostPriceDisplay => CostPrice.ToString("N2");
        public string StockValueDisplay => (Stock * CostPrice).ToString("N2");
        public string StatusDisplay => IsActive ? "نشط" : "غير نشط";
        public string KitBadge => IsKit ? "📦 كيت" : "";
        public string WarehouseName { get; set; }
        public string KitComponentsDisplay => IsKit ? KitComponentsCount.ToString() : "-";
    }

    public class WarehouseOption { public int Id { get; set; } public string Name { get; set; } public override string ToString() => Name ?? ""; }

    public class ItemsViewModel : ViewModelBase
    {
        public ObservableCollection<ItemRow> Items { get; } = new();
        public ObservableCollection<WarehouseOption> Warehouses { get; } = new();

        private ItemRow _selectedRow;
        public ItemRow SelectedRow { get => _selectedRow; set { SetProperty(ref _selectedRow, value); if (value != null) EditItem(value); } }

        private string _searchText = "";
        public string SearchText { get => _searchText; set { SetProperty(ref _searchText, value); _ = LoadItemsAsync(); } }
        
        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
        
        private bool _selectAll;
        public bool SelectAll
        {
            get => _selectAll;
            set { SetProperty(ref _selectAll, value); foreach (var i in Items) i.IsSelected = value; }
        }

        public ICommand RefreshCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand BulkMoveWarehouseCommand { get; }
        public ICommand BulkToggleActiveCommand { get; }
        public ICommand ExportExcelCommand { get; }
        public ICommand ImportExcelCommand { get; }
        public ICommand RecalculateAllKitCostsCommand { get; }

        public ItemsViewModel()
        {
            RefreshCommand = new RelayCommand(() => _ = LoadItemsAsync());
            EditCommand = new RelayCommand(EditSelected, () => Items.Any(i => i.IsSelected));
            AddCommand = new RelayCommand(AddNewItem);
            DeleteCommand = new RelayCommand(DeleteSelected, () => Items.Any(i => i.IsSelected));
            BulkMoveWarehouseCommand = new RelayCommand(BulkMoveWarehouse, () => Items.Any(i => i.IsSelected));
            BulkToggleActiveCommand = new RelayCommand(BulkToggleActive, () => Items.Any(i => i.IsSelected));
            ExportExcelCommand = new RelayCommand(ExportToExcel);
            ImportExcelCommand = new RelayCommand(ImportFromExcel);
            RecalculateAllKitCostsCommand = new RelayCommand(RecalculateAllKitCosts);
            _ = LoadItemsAsync();
            _ = LoadWarehouses();
        }

        public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();

        public async Task LoadItemsAsync()
        {
            Items.Clear();
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                await conn.OpenAsync();
                string sql = @"SELECT i.id, i.name, COALESCE(i.barcode,''), COALESCE(i.sell_price,0), COALESCE(i.cost_price,0),
                    i.opening_quantity + COALESCE((SELECT SUM(CASE WHEN im.type IN ('in','opening','transfer_in') THEN ABS(im.quantity) WHEN im.type IN ('out','transfer_out') THEN -ABS(im.quantity) WHEN im.type='adjustment' THEN im.quantity ELSE 0 END) FROM inventory_movements im WHERE im.item_id=i.id), 0),
                    COALESCE(i.min_stock,0), COALESCE(i.is_active, TRUE), COALESCE(i.is_kit, FALSE),
                    (SELECT COUNT(*) FROM kit_components kc WHERE kc.kit_item_id = i.id),
                    (SELECT w.name FROM stock_levels sl JOIN warehouses w ON w.id = sl.warehouse_id WHERE sl.item_id = i.id LIMIT 1)
                    FROM items i WHERE 1=1";
                if (!string.IsNullOrWhiteSpace(SearchText)) sql += " AND (i.name ILIKE @q OR i.barcode ILIKE @q)";
                sql += " ORDER BY i.name LIMIT 500";
                using var cmd = new NpgsqlCommand(sql, conn);
                if (!string.IsNullOrWhiteSpace(SearchText)) cmd.Parameters.AddWithValue("q", "%" + SearchText + "%");
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                    Items.Add(new ItemRow { 
                        Id = r.GetInt32(0), 
                        Name = r.IsDBNull(1) ? "" : r.GetString(1), 
                        Barcode = r.IsDBNull(2) ? "" : r.GetString(2), 
                        SellPrice = r.IsDBNull(3) ? 0 : Convert.ToDecimal(r.GetValue(3)), 
                        CostPrice = r.IsDBNull(4) ? 0 : Convert.ToDecimal(r.GetValue(4)), 
                        Stock = r.IsDBNull(5) ? 0 : Convert.ToDecimal(r.GetValue(5)), 
                        MinStock = r.IsDBNull(6) ? 0 : Convert.ToDecimal(r.GetValue(6)), 
                        IsActive = r.IsDBNull(7) ? true : r.GetBoolean(7),
                        IsKit = r.IsDBNull(8) ? false : r.GetBoolean(8),
                        KitComponentsCount = r.IsDBNull(9) ? 0 : r.GetInt32(9),
                        WarehouseName = r.IsDBNull(10) ? "-" : r.GetString(10)
                    });
                StatusMessage = "تم تحميل " + Items.Count + " صنف";
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        private async Task LoadWarehouses()
        {
            Warehouses.Clear();
            try { using var conn = new NpgsqlConnection(GetCS()); await conn.OpenAsync(); using var cmd = new NpgsqlCommand("SELECT id, name FROM warehouses WHERE COALESCE(is_active, TRUE) = TRUE ORDER BY name", conn); using var r = await cmd.ExecuteReaderAsync(); while (await r.ReadAsync()) Warehouses.Add(new WarehouseOption { Id = r.GetInt32(0), Name = r.GetString(1) }); }
            catch { }
        }

        private void EditItem(ItemRow row)
        {
            try
            {
                var item = new Item();
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using var cmd = new NpgsqlCommand("SELECT id, name, barcode, sell_price, cost_price, min_stock, opening_quantity, is_active FROM items WHERE id=@id", conn);
                    cmd.Parameters.AddWithValue("id", row.Id);
                    using var r = cmd.ExecuteReader();
                    if (r.Read())
                    {
                        item.Id = r.GetInt32(0);
                        item.Name = r.IsDBNull(1) ? "" : r.GetString(1);
                        item.Barcode = r.IsDBNull(2) ? "" : r.GetString(2);
                        item.SellPrice = r.IsDBNull(3) ? 0m : r.GetDecimal(3);
                        item.CostPrice = r.IsDBNull(4) ? 0m : r.GetDecimal(4);
                        item.MinStock = r.IsDBNull(5) ? 0m : r.GetDecimal(5);
                        item.OpeningQuantity = r.IsDBNull(6) ? 0m : r.GetDecimal(6);
                    }
                }
                var win = new Views.ItemEditWindow(item);
                win.ShowDialog();
                _ = LoadItemsAsync();
            }
            catch { }
        }

        private void EditSelected()
        {
            var sel = Items.FirstOrDefault(i => i.IsSelected);
            if (sel != null) EditItem(sel);
        }

        private void AddNewItem()
        {
            try
            {
                var win = new Views.ItemEditWindow(null);
                win.ShowDialog();
                _ = LoadItemsAsync();
            }
            catch { }
        }

        private void DeleteSelected()
        {
            var sel = Items.Where(i => i.IsSelected).ToList();
            if (sel.Count == 0) return;
            if (System.Windows.MessageBox.Show("حذف " + sel.Count + " صنف؟", "تأكيد", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning) != System.Windows.MessageBoxResult.Yes) return;
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var tran = conn.BeginTransaction();
                try
                {
                    var ids = sel.Select(i => i.Id).ToArray();
                    // P0-4 Batch 5: batch delete with ANY (was N+1 per-row DELETEs)
                    using (var mCmd = new NpgsqlCommand("DELETE FROM inventory_movements WHERE item_id = ANY(@ids)", conn, tran))
                    { mCmd.Parameters.AddWithValue("ids", ids); mCmd.ExecuteNonQuery(); }
                    using (var dCmd = new NpgsqlCommand("DELETE FROM items WHERE id = ANY(@ids)", conn, tran))
                    { dCmd.Parameters.AddWithValue("ids", ids); dCmd.ExecuteNonQuery(); }
                    tran.Commit();
                }
                catch { try { tran.Rollback(); } catch { } throw; }
                StatusMessage = "حذف " + sel.Count + " صنف";
                _ = LoadItemsAsync();
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        private void BulkMoveWarehouse()
        {
            var sel = Items.Where(i => i.IsSelected).ToList();
            if (sel.Count == 0) return;
            var win = new System.Windows.Window { Title = "اختر المخزن", Width = 350, Height = 200, WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen, FlowDirection = System.Windows.FlowDirection.RightToLeft };
            var panel = new System.Windows.Controls.StackPanel { Margin = new System.Windows.Thickness(15) };
            panel.Children.Add(new System.Windows.Controls.TextBlock { Text = "المخزن:", Margin = new System.Windows.Thickness(0, 0, 0, 10) });
            var combo = new System.Windows.Controls.ComboBox { ItemsSource = Warehouses, DisplayMemberPath = "Name", SelectedValuePath = "Id", Padding = new System.Windows.Thickness(6, 4, 6, 4), Margin = new System.Windows.Thickness(0, 0, 0, 15) };
            if (Warehouses.Count > 0) combo.SelectedIndex = 0;
            panel.Children.Add(combo);
            var btnOk = new System.Windows.Controls.Button { Content = "تأكيد", Width = 100, HorizontalAlignment = System.Windows.HorizontalAlignment.Center, Padding = new System.Windows.Thickness(10, 5, 10, 5) };
            panel.Children.Add(btnOk);
            win.Content = panel;
            int? whId = null;
            btnOk.Click += (s, e) => { whId = combo.SelectedValue as int?; win.Close(); };
            win.ShowDialog();
            if (!whId.HasValue) return;
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var tran = conn.BeginTransaction();
                try
                {
                    var ids = sel.Select(i => i.Id).ToArray();
                    // P0-4 Batch 5: batch update with ANY (was N+1 per-row UPDATE)
                    using var cmd = new NpgsqlCommand("UPDATE items SET category_id=@whId WHERE id = ANY(@ids)", conn, tran);
                    cmd.Parameters.AddWithValue("whId", whId.Value);
                    cmd.Parameters.AddWithValue("ids", ids);
                    cmd.ExecuteNonQuery();
                    tran.Commit();
                }
                catch { try { tran.Rollback(); } catch { } throw; }
                StatusMessage = "نقل " + sel.Count + " صنف";
                _ = LoadItemsAsync();
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        private void BulkToggleActive()
        {
            var sel = Items.Where(i => i.IsSelected).ToList();
            if (sel.Count == 0) return;
            bool newStatus = !sel.All(i => i.IsActive);
            if (System.Windows.MessageBox.Show(newStatus ? "تفعيل " + sel.Count + " صنف؟" : "تعطيل " + sel.Count + " صنف؟", "تأكيد", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes) return;
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var tran = conn.BeginTransaction();
                try
                {
                    var ids = sel.Select(i => i.Id).ToArray();
                    // P0-4 Batch 5: batch update with ANY (was N+1 per-row UPDATE)
                    using var cmd = new NpgsqlCommand("UPDATE items SET is_active=@a WHERE id = ANY(@ids)", conn, tran);
                    cmd.Parameters.AddWithValue("a", newStatus);
                    cmd.Parameters.AddWithValue("ids", ids);
                    cmd.ExecuteNonQuery();
                    tran.Commit();
                }
                catch { try { tran.Rollback(); } catch { } throw; }
                StatusMessage = newStatus ? "تفعيل " + sel.Count : "تعطيل " + sel.Count;
                _ = LoadItemsAsync();
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        private void ExportToExcel()
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "CSV files (*.csv)|*.csv|Excel files (*.xlsx)|*.xlsx", DefaultExt = ".csv" };
                if (dialog.ShowDialog() != true) return;
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Name,Barcode,SellPrice,CostPrice,Stock,MinStock,IsActive");
                foreach (var item in Items)
                {
                    sb.AppendLine($"{item.Name},{item.Barcode},{item.SellPrice},{item.CostPrice},{item.Stock},{item.MinStock},{item.IsActive}");
                }
                System.IO.File.WriteAllText(dialog.FileName, sb.ToString(), System.Text.Encoding.UTF8);
                StatusMessage = "تم تصدير " + Items.Count + " صنف";
                System.Windows.MessageBox.Show("تم التصدير بنجاح", "تصدير", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        private void ImportFromExcel()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "CSV files (*.csv)|*.csv|Excel files (*.xlsx)|*.xlsx" };
                if (dialog.ShowDialog() != true) return;
                var lines = System.IO.File.ReadAllLines(dialog.FileName, System.Text.Encoding.UTF8);
                int imported = 0;
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    for (int i = 1; i < lines.Length; i++)
                    {
                        var parts = lines[i].Split(',');
                        if (parts.Length < 4) continue;
                        string name = parts[0].Trim();
                        string barcode = parts.Length > 1 ? parts[1].Trim() : "";
                        decimal sellPrice = 0, costPrice = 0;
                        decimal.TryParse(parts.Length > 2 ? parts[2].Trim() : "0", out sellPrice);
                        decimal.TryParse(parts.Length > 3 ? parts[3].Trim() : "0", out costPrice);
                        using var cmd = new NpgsqlCommand(@"INSERT INTO items (name, barcode, sell_price, cost_price, is_active, opening_quantity)
                            VALUES (@name, @bc, @sp, @cp, TRUE, 0) ON CONFLICT DO NOTHING", conn);
                        cmd.Parameters.AddWithValue("name", name);
                        cmd.Parameters.AddWithValue("bc", barcode);
                        cmd.Parameters.AddWithValue("sp", sellPrice);
                        cmd.Parameters.AddWithValue("cp", costPrice);
                        cmd.ExecuteNonQuery();
                        imported++;
                    }
                }
                StatusMessage = "تم استيراد " + imported + " صنف";
                System.Windows.MessageBox.Show("تم استيراد " + imported + " صنف", "استيراد", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                _ = LoadItemsAsync();
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        // ============================================================
        //  إعادة احتساب تكاليف جميع الكيتات
        // ============================================================
        private void RecalculateAllKitCosts()
        {
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                string sql = @"UPDATE items SET 
                                cost_price = subq.total_cost, 
                                avg_cost_price = subq.total_cost, 
                                last_cost_recalculation = NOW()
                               FROM (
                                   SELECT kc.kit_item_id, SUM(kc.quantity * i2.cost_price) as total_cost
                                   FROM kit_components kc
                                   JOIN items i2 ON i2.id = kc.component_item_id
                                   GROUP BY kc.kit_item_id
                               ) subq
                               WHERE items.id = subq.kit_item_id AND items.is_kit = TRUE;";
                using var cmd = new NpgsqlCommand(sql, conn);
                int affected = cmd.ExecuteNonQuery();
                StatusMessage = "تم تحديث تكلفة " + affected + " كيت";
                System.Windows.MessageBox.Show("تم تحديث تكلفة " + affected + " كيت بنجاح", "إعادة احتساب", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                _ = LoadItemsAsync();
            }
            catch (Exception ex) 
            { 
                StatusMessage = "خطأ: " + ex.Message;
                System.Windows.MessageBox.Show("خطأ: " + ex.Message, "خطأ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
