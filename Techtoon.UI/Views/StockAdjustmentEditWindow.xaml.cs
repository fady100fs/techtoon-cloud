using System;
using System.Collections.ObjectModel;
using System.Windows;
using Npgsql;
using Techtoon.UI.Services;

namespace Techtoon.UI.Views
{
    public class AdjItemOption
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class AdjWarehouseOption
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public partial class StockAdjustmentEditWindow : Window
    {
        private int _adjId = 0;
        private string _status = "draft";
        public bool Saved { get; private set; }

        public ObservableCollection<AdjItemOption> Items { get; } = new ObservableCollection<AdjItemOption>();
        public ObservableCollection<AdjWarehouseOption> Warehouses { get; } = new ObservableCollection<AdjWarehouseOption>();
        public ObservableCollection<StockReason> Reasons { get; } = new ObservableCollection<StockReason>();

        public StockAdjustmentEditWindow()
        {
            InitializeComponent();
            DataContext = this;
            LoadLookups();
            UpdateButtons();
        }

        private void LoadLookups()
        {
            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            conn.Open();

            // Items
            Items.Clear();
            using (var cmd = new NpgsqlCommand("SELECT id, name FROM items WHERE is_active = TRUE ORDER BY name", conn))
            using (var r = cmd.ExecuteReader())
                while (r.Read()) Items.Add(new AdjItemOption { Id = r.GetInt32(0), Name = r.GetString(1) });
            cmbItem.ItemsSource = Items;

            // Warehouses
            Warehouses.Clear();
            using (var cmd = new NpgsqlCommand("SELECT id, name FROM warehouses WHERE is_active = TRUE ORDER BY name", conn))
            using (var r = cmd.ExecuteReader())
                while (r.Read()) Warehouses.Add(new AdjWarehouseOption { Id = r.GetInt32(0), Name = r.GetString(1) });
            cmbWarehouse.ItemsSource = Warehouses;
            if (Warehouses.Count > 0) cmbWarehouse.SelectedIndex = 0;

            // Reasons
            Reasons.Clear();
            var reasons = StockReasonsService.GetReasons("adjustment");
            foreach (var r in reasons) Reasons.Add(r);
            cmbReason.ItemsSource = Reasons;
            if (Reasons.Count > 0) cmbReason.SelectedIndex = 0;
        }

        public void LoadForEdit(int adjId)
        {
            _adjId = adjId;
            var adj = InventoryAdjustmentService.GetById(adjId);
            if (adj == null) return;

            lblTitle.Text = "تعديل تسوية #" + adjId;
            _status = adj.Status;
            cmbItem.SelectedValue = adj.ItemId;
            cmbItem.IsEnabled = false;
            cmbWarehouse.SelectedValue = adj.WarehouseId ?? 0;
            cmbWarehouse.IsEnabled = false;
            cmbReason.SelectedValue = adj.Reason;
            txtCurrentQty.Text = adj.OldQuantity.ToString("0.00");
            txtNewQty.Text = adj.NewQuantity.ToString("0.00");
            txtNotes.Text = adj.Notes;
            lblUnitCost.Text = adj.UnitCost.ToString("N2");
            lblValueBefore.Text = adj.ValueBefore.ToString("N2");
            lblValueAfter.Text = adj.ValueAfter.ToString("N2");

            UpdateButtons();
        }

        private void UpdateButtons()
        {
            btnSave.Visibility = _status == "draft" ? Visibility.Visible : Visibility.Collapsed;
            btnSubmit.Visibility = _status == "draft" ? Visibility.Visible : Visibility.Collapsed;
            btnApprove.Visibility = _status == "pending" ? Visibility.Visible : Visibility.Collapsed;
            btnExecute.Visibility = _status == "approved" ? Visibility.Visible : Visibility.Collapsed;

            cmbItem.IsEnabled = _status == "draft";
            cmbWarehouse.IsEnabled = _status == "draft";
            cmbReason.IsEnabled = _status == "draft";
            txtNewQty.IsReadOnly = _status != "draft";
            txtNotes.IsReadOnly = _status != "draft";
        }

        private void CmbItem_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cmbItem.SelectedValue == null) return;
            int itemId = (int)cmbItem.SelectedValue;
            int whId = cmbWarehouse.SelectedValue != null ? (int)cmbWarehouse.SelectedValue : 0;

            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            conn.Open();
            using var cmd = new NpgsqlCommand(
                "SELECT COALESCE(i.opening_quantity,0) + COALESCE((SELECT SUM(CASE WHEN type IN ('in','opening') THEN quantity WHEN type='out' THEN -quantity WHEN type='adjustment' THEN quantity ELSE 0 END) FROM inventory_movements WHERE item_id=i.id AND (@wh=0 OR warehouse_id=@wh OR warehouse_id IS NULL)),0), " +
                "COALESCE(i.avg_cost_price, i.cost_price, 0) FROM items i WHERE i.id=@item", conn);
            cmd.Parameters.AddWithValue("wh", whId);
            cmd.Parameters.AddWithValue("item", itemId);
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                txtCurrentQty.Text = r.GetDouble(0).ToString("0.00");
                lblUnitCost.Text = ((decimal)r.GetDouble(1)).ToString("N2");
            }
        }

        private void TxtNewQty_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (double.TryParse(txtNewQty.Text, out double newQty) && double.TryParse(txtCurrentQty.Text, out double cur))
            {
                txtDiff.Text = (newQty - cur).ToString("0.00");
                decimal unitCost = decimal.Parse(lblUnitCost.Text);
                lblValueBefore.Text = ((decimal)cur * unitCost).ToString("N2");
                lblValueAfter.Text = ((decimal)newQty * unitCost).ToString("N2");
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (cmbItem.SelectedValue == null) { MessageBox.Show("اختر الصنف"); return; }
            if (cmbReason.SelectedValue == null) { MessageBox.Show("اختر السبب"); return; }
            if (!double.TryParse(txtNewQty.Text, out double newQty)) { MessageBox.Show("كمية غير صحيحة"); return; }

            try
            {
                int itemId = (int)cmbItem.SelectedValue;
                int whId = cmbWarehouse.SelectedValue != null ? (int)cmbWarehouse.SelectedValue : 0;
                string reason = cmbReason.SelectedValue.ToString();

                if (_adjId == 0)
                {
                    _adjId = InventoryAdjustmentService.CreateAdjustment(itemId, whId, newQty, reason, txtNotes.Text, 1);
                    Saved = true;
                    _status = "draft";
                    LoadForEdit(_adjId);
                    MessageBox.Show("تم حفظ التسوية كمسودة");
                }
                else
                {
                    MessageBox.Show("التسوية محفوظة بالفعل");
                }
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (_adjId == 0) { BtnSave_Click(sender, e); if (_adjId == 0) return; }
            try
            {
                InventoryAdjustmentService.SubmitForApproval(_adjId);
                _status = "pending";
                UpdateButtons();
                MessageBox.Show("تم تسليم التسوية للاعتماد");
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void BtnApprove_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                InventoryAdjustmentService.Approve(_adjId, 1);
                _status = "approved";
                UpdateButtons();
                MessageBox.Show("تم اعتماد التسوية");
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void BtnExecute_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("تنفيذ التسوية؟ سيتم تعديل رصيد المخزون.", "تأكيد", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            try
            {
                InventoryAdjustmentService.Execute(_adjId, 1);
                _status = "executed";
                UpdateButtons();
                MessageBox.Show("تم تنفيذ التسوية بنجاح");
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            if (Saved) DialogResult = true;
            Close();
        }
    }
}