using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Npgsql;
using Techtoon.Core.Models;

namespace Techtoon.UI.Views
{
    public class ComponentItemOption
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double CostPrice { get; set; }
        public string DisplayText => Name + " (" + CostPrice.ToString("N2") + ")";
    }

    public class KitComponentRow
    {
        public int Id { get; set; }
        public int ComponentItemId { get; set; }
        public string ComponentName { get; set; }
        public double Quantity { get; set; }
        public double UnitCost { get; set; }
        public double TotalCost => Quantity * UnitCost;
    }

    public partial class ItemEditWindow : Window
    {
        private int _id = 0;
        private ObservableCollection<KitComponentRow> _kitComponents = new();

        public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();

        public ItemEditWindow(int id) : this(LoadItemById(id)) { _id = id; }

        private static Item? LoadItemById(int id)
        {
            try {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var cmd = new NpgsqlCommand("SELECT id, name, barcode, sell_price, cost_price, min_stock, opening_quantity FROM items WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("id", id);
                using var r = cmd.ExecuteReader();
                if (r.Read())
                {
                    return new Item
                    {
                        Id = r.GetInt32(0),
                        Name = r.IsDBNull(1) ? "" : r.GetString(1),
                        Barcode = r.IsDBNull(2) ? "" : r.GetString(2),
                        SellPrice = r.IsDBNull(3) ? 0 : r.GetDouble(3),
                        CostPrice = r.IsDBNull(4) ? 0 : r.GetDouble(4),
                        MinStock = r.IsDBNull(5) ? 0 : r.GetDouble(5),
                        OpeningQuantity = r.IsDBNull(6) ? 0 : r.GetDouble(6)
                    };
                }
            } catch { }
            return null;
        }

        public ItemEditWindow(Item? item = null)
        {
            InitializeComponent();
            DgKitComponents.ItemsSource = _kitComponents;
            if (item != null) _id = item.Id;
            try { LblTitle.Text = _id > 0 ? "\u062A\u0639\u062F\u064A\u0644 \u0635\u0646\u0641" : "\u0625\u0636\u0627\u0641\u0629 \u0635\u0646\u0641 \u062C\u062F\u064A\u062F"; } catch { }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCategories();
            LoadComponentItems();
            if (_id > 0) LoadItemData(_id);
        }

        private void LoadCategories()
        {
            try {
                CmbCategory.Items.Clear();
                using var conn = new NpgsqlConnection(GetCS()); conn.Open();
                using var cmd = new NpgsqlCommand("SELECT id, name FROM categories ORDER BY name", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    CmbCategory.Items.Add(new { Id = r.GetInt32(0), Name = r.GetString(1) });
            } catch { }
        }

        private void LoadComponentItems()
        {
            try {
                CmbComponent.Items.Clear();
                using var conn = new NpgsqlConnection(GetCS()); conn.Open();
                string sql = "SELECT id, name, COALESCE(cost_price,0) FROM items WHERE COALESCE(is_kit, FALSE) = FALSE AND COALESCE(is_active, TRUE) = TRUE ORDER BY name";
                if (_id > 0) sql += " AND id <> " + _id;
                using var cmd = new NpgsqlCommand(sql, conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    CmbComponent.Items.Add(new ComponentItemOption { Id = r.GetInt32(0), Name = r.GetString(1), CostPrice = r.GetDouble(2) });
            } catch { }
        }

        private void LoadItemData(int itemId)
        {
            try {
                using var conn = new NpgsqlConnection(GetCS()); conn.Open();
                using var cmd = new NpgsqlCommand("SELECT name, barcode, sell_price, cost_price, min_stock, opening_quantity, category_id, is_active, is_kit FROM items WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("id", itemId);
                using var r = cmd.ExecuteReader();
                if (r.Read())
                {
                    TxtName.Text = r.IsDBNull(0) ? "" : r.GetString(0);
                    TxtBarcode.Text = r.IsDBNull(1) ? "" : r.GetString(1);
                    TxtSellPrice.Text = (r.IsDBNull(2) ? 0 : r.GetDouble(2)).ToString("N2");
                    TxtCostPrice.Text = (r.IsDBNull(3) ? 0 : r.GetDouble(3)).ToString("N2");
                    TxtMinStock.Text = (r.IsDBNull(4) ? 0 : r.GetDouble(4)).ToString("N2");
                    TxtOpeningQty.Text = (r.IsDBNull(5) ? 0 : r.GetDouble(5)).ToString("N2");
                    ChkActive.IsChecked = r.IsDBNull(7) ? true : r.GetBoolean(7);
                    ChkIsKit.IsChecked = r.IsDBNull(8) ? false : r.GetBoolean(8);
                }
                if (ChkIsKit.IsChecked == true) LoadKitComponents(conn, itemId);
            } catch { }
            UpdateKitComponentsVisibility();
            UpdateTotalKitCost();
        }

        private void LoadKitComponents(NpgsqlConnection conn, int kitItemId)
        {
            try {
                _kitComponents.Clear();
                using var cmd = new NpgsqlCommand(@"SELECT kc.id, kc.component_item_id, i.name, kc.quantity, COALESCE(i.cost_price,0) FROM kit_components kc JOIN items i ON i.id = kc.component_item_id WHERE kc.kit_item_id = @kitId", conn);
                cmd.Parameters.AddWithValue("kitId", kitItemId);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    _kitComponents.Add(new KitComponentRow { Id = r.GetInt32(0), ComponentItemId = r.GetInt32(1), ComponentName = r.GetString(2), Quantity = r.GetDouble(3), UnitCost = r.GetDouble(4) });
            } catch { }
        }

        private void ChkIsKit_Changed(object sender, RoutedEventArgs e)
        {
            UpdateKitComponentsVisibility();
            if (ChkIsKit.IsChecked == true) { TxtOpeningQty.Text = "0"; TxtOpeningQty.IsEnabled = false; TxtCostPrice.IsReadOnly = true; }
            else { TxtOpeningQty.IsEnabled = true; TxtCostPrice.IsReadOnly = false; }
        }

        private void UpdateKitComponentsVisibility()
        {
            KitComponentsSection.Visibility = ChkIsKit.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnAddComponent_Click(object sender, RoutedEventArgs e)
        {
            try {
                if (CmbComponent.SelectedItem is not ComponentItemOption selected) return;
                double qty = 1;
                if (!double.TryParse(TxtComponentQty.Text, out qty) || qty <= 0) return;
                var existing = _kitComponents.FirstOrDefault(c => c.ComponentItemId == selected.Id);
                if (existing != null) { existing.Quantity += qty; DgKitComponents.Items.Refresh(); }
                else { _kitComponents.Add(new KitComponentRow { Id = 0, ComponentItemId = selected.Id, ComponentName = selected.Name, Quantity = qty, UnitCost = selected.CostPrice }); }
                TxtComponentQty.Text = "1"; CmbComponent.SelectedIndex = -1; UpdateTotalKitCost();
            } catch { }
        }

        private void BtnRemoveComponent_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                int rowId = (int)btn.Tag;
                var row = _kitComponents.FirstOrDefault(c => c.Id == rowId);
                if (row != null) { _kitComponents.Remove(row); UpdateTotalKitCost(); }
            }
        }

        private void UpdateTotalKitCost()
        {
            double total = _kitComponents.Sum(c => c.TotalCost);
            LblTotalKitCost.Text = total.ToString("N2");
            TxtCostPrice.Text = total.ToString("N2");
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try {
                if (string.IsNullOrWhiteSpace(TxtName.Text)) return;
                bool isKit = ChkIsKit.IsChecked == true;
                if (isKit && _kitComponents.Count == 0) return;
                double sellPrice, costPrice, minStock, openingQty;
                double.TryParse(TxtSellPrice.Text, out sellPrice);
                double.TryParse(TxtCostPrice.Text, out costPrice);
                double.TryParse(TxtMinStock.Text, out minStock);
                double.TryParse(TxtOpeningQty.Text, out openingQty);
                if (isKit) { costPrice = _kitComponents.Sum(c => c.TotalCost); openingQty = 0; }

                using var conn = new NpgsqlConnection(GetCS()); conn.Open();
                if (_id > 0)
                {
                    using var cmd = new NpgsqlCommand(@"UPDATE items SET name=@name, barcode=@bc, sell_price=@sp, cost_price=@cp, min_stock=@ms, opening_quantity=@oq, is_active=@ia, is_kit=@ik, avg_cost_price=@cp, last_cost_recalculation=NOW() WHERE id=@id", conn);
                    cmd.Parameters.AddWithValue("id", _id); cmd.Parameters.AddWithValue("name", TxtName.Text.Trim());
                    cmd.Parameters.AddWithValue("bc", (object)TxtBarcode.Text ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("sp", sellPrice); cmd.Parameters.AddWithValue("cp", costPrice);
                    cmd.Parameters.AddWithValue("ms", minStock); cmd.Parameters.AddWithValue("oq", openingQty);
                    cmd.Parameters.AddWithValue("ia", ChkActive.IsChecked ?? true); cmd.Parameters.AddWithValue("ik", isKit);
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    using var cmd = new NpgsqlCommand(@"INSERT INTO items (name, barcode, sell_price, cost_price, avg_cost_price, min_stock, opening_quantity, is_active, is_kit, last_cost_recalculation) VALUES (@name, @bc, @sp, @cp, @cp, @ms, @oq, @ia, @ik, NOW()) RETURNING id", conn);
                    cmd.Parameters.AddWithValue("name", TxtName.Text.Trim());
                    cmd.Parameters.AddWithValue("bc", (object)TxtBarcode.Text ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("sp", sellPrice); cmd.Parameters.AddWithValue("cp", costPrice);
                    cmd.Parameters.AddWithValue("ms", minStock); cmd.Parameters.AddWithValue("oq", openingQty);
                    cmd.Parameters.AddWithValue("ia", ChkActive.IsChecked ?? true); cmd.Parameters.AddWithValue("ik", isKit);
                    var o = cmd.ExecuteScalar();
                    if (o != null && o != DBNull.Value) _id = Convert.ToInt32(o);
                }
                if (isKit)
                {
                    using var delCmd = new NpgsqlCommand("DELETE FROM kit_components WHERE kit_item_id=@kitId", conn);
                    delCmd.Parameters.AddWithValue("kitId", _id); delCmd.ExecuteNonQuery();
                    foreach (var comp in _kitComponents)
                    {
                        using var insCmd = new NpgsqlCommand("INSERT INTO kit_components (kit_item_id, component_item_id, quantity) VALUES (@kitId, @compId, @qty)", conn);
                        insCmd.Parameters.AddWithValue("kitId", _id); insCmd.Parameters.AddWithValue("compId", comp.ComponentItemId);
                        insCmd.Parameters.AddWithValue("qty", comp.Quantity); insCmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    using var delCmd = new NpgsqlCommand("DELETE FROM kit_components WHERE kit_item_id=@kitId", conn);
                    delCmd.Parameters.AddWithValue("kitId", _id); delCmd.ExecuteNonQuery();
                }
                DialogResult = true; Close();
            } catch (Exception ex) { LblStatus.Text = "\u062E;&#x0637;&#x0623;: " + ex.Message; }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_id == 0) return;
            try {
                using var conn = new NpgsqlConnection(GetCS()); conn.Open();
                var items = new System.Collections.Generic.List<DependencyItem>();
                using (var cmdCheck = new NpgsqlCommand("SELECT id FROM inventory_movements WHERE item_id = @id LIMIT 10", conn))
                {
                    cmdCheck.Parameters.AddWithValue("id", _id);
                    using var rCheck = cmdCheck.ExecuteReader();
                    while (rCheck.Read())
                        items.Add(new DependencyItem { Type = "Movement", Display = "ID: " + rCheck.GetInt32(0), TargetWindow = "", TargetId = rCheck.GetInt32(0) });
                }
                if (items.Count > 0) { new DependenciesWindow(items).ShowDialog(); return; }
                if (MessageBox.Show("\u062A\u0623\u0643\u064A\u062F \u0627;&#x0644;&#x062D;&#x0630;&#x0641;\u061F", "\u062A\u0623\u0643\u064A\u062F", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
                using var cmd = new NpgsqlCommand("DELETE FROM items WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("id", _id); cmd.ExecuteNonQuery();
                DialogResult = true; Close();
            } catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false; Close();
        }
    }
}