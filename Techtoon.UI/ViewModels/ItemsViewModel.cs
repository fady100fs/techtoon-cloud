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
        private bool _isSelected;
        public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
        public string StockDisplay => Stock.ToString("N2");
        public string SellPriceDisplay => SellPrice.ToString("N2");
        public string CostPriceDisplay => CostPrice.ToString("N2");
        public string StockValueDisplay => (Stock * CostPrice).ToString("N2");
        public string StatusDisplay => IsActive ? "\u0646\u0634\u0637" : "\u063A\u064A\u0631 \u0646\u0634\u0637";
    }

    public class WarehouseOption { public int Id { get; set; } public string Name { get; set; } public override string ToString() => Name ?? ""; }

    public class ItemsViewModel : ViewModelBase
    {
        public ObservableCollection<ItemRow> Items { get; } = new();
        public ObservableCollection<WarehouseOption> Warehouses { get; } = new();

        private ItemRow _selectedRow;
        public ItemRow SelectedRow { get => _selectedRow; set { SetProperty(ref _selectedRow, value); if (value != null) EditItem(value); } }

        private string _searchText = "";
        public string SearchText { get => _searchText; set { SetProperty(ref _searchText, value); LoadItems(); } }
        
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

        public ItemsViewModel()
        {
            RefreshCommand = new RelayCommand(LoadItems);
            EditCommand = new RelayCommand(EditSelected, () => Items.Any(i => i.IsSelected));
            AddCommand = new RelayCommand(AddNewItem);
            DeleteCommand = new RelayCommand(DeleteSelected, () => Items.Any(i => i.IsSelected));
            BulkMoveWarehouseCommand = new RelayCommand(BulkMoveWarehouse, () => Items.Any(i => i.IsSelected));
            BulkToggleActiveCommand = new RelayCommand(BulkToggleActive, () => Items.Any(i => i.IsSelected));
            ExportExcelCommand = new RelayCommand(ExportToExcel);
            ImportExcelCommand = new RelayCommand(ImportFromExcel);
            LoadItems();
            LoadWarehouses();
        }

        public static string GetCS() => "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=npg_K9nHVkY5OjRC;Database=neondb;SSL Mode=Require;Trust Server Certificate=true";

        public void LoadItems()
        {
            Items.Clear();
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                string sql = @"SELECT i.id, i.name, COALESCE(i.barcode,''), COALESCE(i.sell_price,0), COALESCE(i.cost_price,0),
                    i.opening_quantity + COALESCE((SELECT SUM(CASE WHEN im.type IN ('in','opening') THEN im.quantity WHEN im.type='out' THEN -im.quantity WHEN im.type='adjustment' THEN im.quantity ELSE 0 END) FROM inventory_movements im WHERE im.item_id=i.id), 0),
                    COALESCE(i.min_stock,0), COALESCE(i.is_active, TRUE) FROM items i WHERE i.is_kit IS NOT TRUE";
                if (!string.IsNullOrWhiteSpace(SearchText)) sql += " AND (i.name ILIKE @q OR i.barcode ILIKE @q)";
                sql += " ORDER BY i.name LIMIT 500";
                using var cmd = new NpgsqlCommand(sql, conn);
                if (!string.IsNullOrWhiteSpace(SearchText)) cmd.Parameters.AddWithValue("q", "%" + SearchText + "%");
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    Items.Add(new ItemRow { Id = r.GetInt32(0), Name = r.IsDBNull(1) ? "" : r.GetString(1), Barcode = r.IsDBNull(2) ? "" : r.GetString(2), SellPrice = r.IsDBNull(3) ? 0 : (decimal)r.GetDouble(3), CostPrice = r.IsDBNull(4) ? 0 : (decimal)r.GetDouble(4), Stock = r.IsDBNull(5) ? 0 : (decimal)r.GetDouble(5), MinStock = r.IsDBNull(6) ? 0 : (decimal)r.GetDouble(6), IsActive = r.IsDBNull(7) ? true : r.GetBoolean(7) });
                StatusMessage = "\u062A\u0645 \u062A\u062D\u0645\u064A\u0644 " + Items.Count + " \u0635\u0646\u0641";
            }
            catch (Exception ex) { StatusMessage = "\u062E\u0637\u0623: " + ex.Message; }
        }

        private void LoadWarehouses()
        {
            Warehouses.Clear();
            try { using var conn = new NpgsqlConnection(GetCS()); conn.Open(); using var cmd = new NpgsqlCommand("SELECT id, name FROM warehouses WHERE COALESCE(is_active, TRUE) = TRUE ORDER BY name", conn); using var r = cmd.ExecuteReader(); while (r.Read()) Warehouses.Add(new WarehouseOption { Id = r.GetInt32(0), Name = r.GetString(1) }); }
            catch { }
        }

        // فتح نافذة تعديل عند النقر على صف
        private void EditItem(ItemRow row)
        {
            try
            {
                // اقرأ بيانات الصنف من DB وأنشئ كائن Item
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
                        item.SellPrice = r.IsDBNull(3) ? 0 : r.GetDouble(3);
                        item.CostPrice = r.IsDBNull(4) ? 0 : r.GetDouble(4);
                        item.MinStock = r.IsDBNull(5) ? 0 : r.GetDouble(5);
                        item.OpeningQuantity = r.IsDBNull(6) ? 0 : r.GetDouble(6);
                    }
                }
                var win = new Views.ItemEditWindow(item);
                win.ShowDialog();
                LoadItems();
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
                LoadItems();
            }
            catch { }
        }

        private void DeleteSelected()
        {
            var sel = Items.Where(i => i.IsSelected).ToList();
            if (sel.Count == 0) return;
            if (System.Windows.MessageBox.Show("\u062D\u0630\u0641 " + sel.Count + " \u0635\u0646\u0641\u061F", "\u062A\u0623\u0643\u064A\u062F", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning) != System.Windows.MessageBoxResult.Yes) return;
            try { using var conn = new NpgsqlConnection(GetCS()); conn.Open(); foreach (var item in sel) { using var c1 = new NpgsqlCommand("DELETE FROM inventory_movements WHERE item_id=@id", conn); c1.Parameters.AddWithValue("id", item.Id); c1.ExecuteNonQuery(); using var c2 = new NpgsqlCommand("DELETE FROM items WHERE id=@id", conn); c2.Parameters.AddWithValue("id", item.Id); c2.ExecuteNonQuery(); } StatusMessage = "\u062D\u0630\u0641 " + sel.Count + " \u0635\u0646\u0641"; LoadItems(); }
            catch (Exception ex) { StatusMessage = "\u062E\u0637\u0623: " + ex.Message; }
        }

        private void BulkMoveWarehouse()
        {
            var sel = Items.Where(i => i.IsSelected).ToList();
            if (sel.Count == 0) return;
            var win = new System.Windows.Window { Title = "\u0627\u062E\u062A\u0631 \u0627\u0644\u0645\u062E\u0632\u0646", Width = 350, Height = 200, WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen, FlowDirection = System.Windows.FlowDirection.RightToLeft };
            var panel = new System.Windows.Controls.StackPanel { Margin = new System.Windows.Thickness(15) };
            panel.Children.Add(new System.Windows.Controls.TextBlock { Text = "\u0627\u0644\u0645\u062E\u0632\u0646:", Margin = new System.Windows.Thickness(0, 0, 0, 10) });
            var combo = new System.Windows.Controls.ComboBox { ItemsSource = Warehouses, DisplayMemberPath = "Name", SelectedValuePath = "Id", Padding = new System.Windows.Thickness(6, 4, 6, 4), Margin = new System.Windows.Thickness(0, 0, 0, 15) };
            if (Warehouses.Count > 0) combo.SelectedIndex = 0;
            panel.Children.Add(combo);
            var btnOk = new System.Windows.Controls.Button { Content = "\u062A\u0623\u0643\u064A\u062F", Width = 100, HorizontalAlignment = System.Windows.HorizontalAlignment.Center, Padding = new System.Windows.Thickness(10, 5, 10, 5) };
            panel.Children.Add(btnOk);
            win.Content = panel;
            int? whId = null;
            btnOk.Click += (s, e) => { whId = combo.SelectedValue as int?; win.Close(); };
            win.ShowDialog();
            if (!whId.HasValue) return;
            try { using var conn = new NpgsqlConnection(GetCS()); conn.Open(); foreach (var item in sel) { using var cmd = new NpgsqlCommand("UPDATE items SET category_id=@whId WHERE id=@id", conn); cmd.Parameters.AddWithValue("whId", whId.Value); cmd.Parameters.AddWithValue("id", item.Id); cmd.ExecuteNonQuery(); } StatusMessage = "\u0646\u0642\u0644 " + sel.Count + " \u0635\u0646\u0641"; LoadItems(); }
            catch (Exception ex) { StatusMessage = "\u062E\u0637\u0623: " + ex.Message; }
        }

        private void BulkToggleActive()
        {
            var sel = Items.Where(i => i.IsSelected).ToList();
            if (sel.Count == 0) return;
            bool newStatus = !sel.All(i => i.IsActive);
            if (System.Windows.MessageBox.Show(newStatus ? "\u062A\u0641\u0639\u064A\u0644 " + sel.Count + " \u0635\u0646\u0641\u061F" : "\u062A\u0639\u0637\u064A\u0644 " + sel.Count + " \u0635\u0646\u0641\u061F", "\u062A\u0623\u0643\u064A\u062F", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes) return;
            try { using var conn = new NpgsqlConnection(GetCS()); conn.Open(); foreach (var item in sel) { using var cmd = new NpgsqlCommand("UPDATE items SET is_active=@a WHERE id=@id", conn); cmd.Parameters.AddWithValue("a", newStatus); cmd.Parameters.AddWithValue("id", item.Id); cmd.ExecuteNonQuery(); } StatusMessage = newStatus ? "\u062A\u0641\u0639\u064A\u0644 " + sel.Count : "\u062A\u0639\u0637\u064A\u0644 " + sel.Count; LoadItems(); }
            catch (Exception ex) { StatusMessage = "\u062E\u0637\u0623: " + ex.Message; }
        }

        // ============================================================
        //  تصدير Excel (CSV)
        // ============================================================
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
                StatusMessage = "\u062A\u0645 \u062A\u0635\u062F\u064A\u0631 " + Items.Count + " \u0635\u0646\u0641";
                System.Windows.MessageBox.Show("\u062A\u0645 \u0627\u0644\u062A\u0635\u062F\u064A\u0631 \u0628\u0646\u062C\u0627\u062D", "\u062A\u0635\u062F\u064A\u0631", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex) { StatusMessage = "\u062E\u0637\u0623: " + ex.Message; }
        }

        // ============================================================
        //  استيراد Excel (CSV)
        // ============================================================
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
                    // تخطي السطر الأول (العناوين)
                    for (int i = 1; i < lines.Length; i++)
                    {
                        var parts = lines[i].Split(',');
                        if (parts.Length < 4) continue;
                        string name = parts[0].Trim();
                        string barcode = parts.Length > 1 ? parts[1].Trim() : "";
                        double sellPrice = 0, costPrice = 0;
                        double.TryParse(parts.Length > 2 ? parts[2].Trim() : "0", out sellPrice);
                        double.TryParse(parts.Length > 3 ? parts[3].Trim() : "0", out costPrice);

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
                StatusMessage = "\u062A\u0645 \u0627\u0633\u062A\u064A\u0631\u0627\u062F " + imported + " \u0635\u0646\u0641";
                System.Windows.MessageBox.Show("\u062A\u0645 \u0627\u0633\u062A\u064A\u0631\u0627\u062F " + imported + " \u0635\u0646\u0641", "\u0627\u0633\u062A\u064A\u0631\u0627\u062F", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                LoadItems();
            }
            catch (Exception ex) { StatusMessage = "\u062E\u0637\u0623: " + ex.Message; }
        }
    }
}
