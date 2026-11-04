using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Npgsql;
using Techtoon.UI.Services;

namespace Techtoon.UI.Views
{
    public class DmgItemOption { public int Id { get; set; } public string Name { get; set; } }
    public class DmgWarehouseOption { public int Id { get; set; } public string Name { get; set; } }

    public partial class DamagedItemEditWindow : Window
    {
        private int _id = 0;
        private string _status = "draft";
        public bool Saved { get; private set; }

        public ObservableCollection<DmgItemOption> Items { get; } = new ObservableCollection<DmgItemOption>();
        public ObservableCollection<DmgWarehouseOption> Warehouses { get; } = new ObservableCollection<DmgWarehouseOption>();
        public ObservableCollection<StockReason> Reasons { get; } = new ObservableCollection<StockReason>();

        public DamagedItemEditWindow()
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

            Items.Clear();
            using (var cmd = new NpgsqlCommand("SELECT id, name FROM items WHERE is_active = TRUE ORDER BY name", conn))
            using (var r = cmd.ExecuteReader())
                while (r.Read()) Items.Add(new DmgItemOption { Id = r.GetInt32(0), Name = r.GetString(1) });
            cmbItem.ItemsSource = Items;

            Warehouses.Clear();
            using (var cmd = new NpgsqlCommand("SELECT id, name FROM warehouses WHERE is_active = TRUE ORDER BY name", conn))
            using (var r = cmd.ExecuteReader())
                while (r.Read()) Warehouses.Add(new DmgWarehouseOption { Id = r.GetInt32(0), Name = r.GetString(1) });
            cmbWarehouse.ItemsSource = Warehouses;
            if (Warehouses.Count > 0) cmbWarehouse.SelectedIndex = 0;

            Reasons.Clear();
            foreach (var r in StockReasonsService.GetReasons("damaged")) Reasons.Add(r);
            cmbReason.ItemsSource = Reasons;
            if (Reasons.Count > 0) cmbReason.SelectedIndex = 0;
        }

        public void LoadForEdit(int id)
        {
            _id = id;
            var d = DamagedItemsService.GetById(id);
            if (d == null) return;
            lblTitle.Text = "تعديل صنف تالف #" + id;
            _status = d.Status;
            cmbItem.SelectedValue = d.ItemId;
            cmbItem.IsEnabled = false;
            cmbWarehouse.SelectedValue = d.WarehouseId;
            cmbWarehouse.IsEnabled = false;
            cmbReason.SelectedValue = d.DamageReasonId ?? 0;
            txtQty.Text = d.Quantity.ToString("0.00");
            txtUnitCost.Text = d.UnitCost.ToString("N2");
            txtTotalValue.Text = d.TotalValue.ToString("N2");
            txtNotes.Text = d.Notes;
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            btnSave.Visibility = _status == "draft" ? Visibility.Visible : Visibility.Collapsed;
            btnStartInspection.Visibility = _status == "draft" ? Visibility.Visible : Visibility.Collapsed;
            btnCompleteInspection.Visibility = _status == "under_inspection" ? Visibility.Visible : Visibility.Collapsed;
            pnlInspection.Visibility = _status == "under_inspection" ? Visibility.Visible : Visibility.Collapsed;

            cmbItem.IsEnabled = _status == "draft";
            cmbWarehouse.IsEnabled = _status == "draft";
            cmbReason.IsEnabled = _status == "draft";
            txtQty.IsReadOnly = _status != "draft";
            txtNotes.IsReadOnly = _status != "draft";
        }

        private void CmbItem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbItem.SelectedValue == null) return;
            int itemId = (int)cmbItem.SelectedValue;
            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT COALESCE(avg_cost_price, cost_price, 0) FROM items WHERE id=@item", conn);
            cmd.Parameters.AddWithValue("item", itemId);
            txtUnitCost.Text = (Convert.ToDecimal(cmd.ExecuteScalar() ?? 0)).ToString("N2");
            TxtQty_TextChanged(null, null);
        }

        private void TxtQty_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(txtQty.Text, out double qty) && decimal.TryParse(txtUnitCost.Text, out decimal cost))
                txtTotalValue.Text = ((decimal)qty * cost).ToString("N2");
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (cmbItem.SelectedValue == null) { MessageBox.Show("اختر الصنف"); return; }
            if (!double.TryParse(txtQty.Text, out double qty) || qty <= 0) { MessageBox.Show("كمية غير صحيحة"); return; }
            try
            {
                int itemId = (int)cmbItem.SelectedValue;
                int whId = cmbWarehouse.SelectedValue != null ? (int)cmbWarehouse.SelectedValue : 0;
                int? reasonId = cmbReason.SelectedValue as int?;
                if (_id == 0)
                {
                    _id = DamagedItemsService.Create(itemId, whId, qty, reasonId, txtNotes.Text, 1);
                    Saved = true;
                    _status = "draft";
                    LoadForEdit(_id);
                    MessageBox.Show("تم حفظ السجل");
                }
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void BtnStartInspection_Click(object sender, RoutedEventArgs e)
        {
            if (_id == 0) { BtnSave_Click(sender, e); if (_id == 0) return; }
            try
            {
                DamagedItemsService.StartInspection(_id, 1);
                _status = "under_inspection";
                UpdateButtons();
                MessageBox.Show("بدأ الفحص");
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void BtnCompleteInspection_Click(object sender, RoutedEventArgs e)
        {
            var result = (cmbInspectionResult.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            if (string.IsNullOrEmpty(result)) { MessageBox.Show("اختر نتيجة الفحص"); return; }
            try
            {
                DamagedItemsService.CompleteInspection(_id, result, txtInspectionNotes.Text, 1);
                _status = result == "back_to_stock" ? "returned" : (result == "writeoff" ? "writeoff_requested" : "inspected");
                UpdateButtons();
                MessageBox.Show("تم تسجيل نتيجة الفحص");
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