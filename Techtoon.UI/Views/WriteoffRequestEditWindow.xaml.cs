using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Npgsql;
using Techtoon.UI.Services;

namespace Techtoon.UI.Views
{
    public class WoItemOption { public int Id { get; set; } public string Name { get; set; } }
    public class WoWarehouseOption { public int Id { get; set; } public string Name { get; set; } }

    public partial class WriteoffRequestEditWindow : Window
    {
        private int _id = 0;
        private string _status = "draft";
        public bool Saved { get; private set; }

        public ObservableCollection<WoItemOption> Items { get; } = new ObservableCollection<WoItemOption>();
        public ObservableCollection<WoWarehouseOption> Warehouses { get; } = new ObservableCollection<WoWarehouseOption>();
        public ObservableCollection<StockReason> Reasons { get; } = new ObservableCollection<StockReason>();

        public WriteoffRequestEditWindow()
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
                while (r.Read()) Items.Add(new WoItemOption { Id = r.GetInt32(0), Name = r.GetString(1) });
            cmbItem.ItemsSource = Items;

            Warehouses.Clear();
            using (var cmd = new NpgsqlCommand("SELECT id, name FROM warehouses WHERE is_active = TRUE ORDER BY name", conn))
            using (var r = cmd.ExecuteReader())
                while (r.Read()) Warehouses.Add(new WoWarehouseOption { Id = r.GetInt32(0), Name = r.GetString(1) });
            cmbWarehouse.ItemsSource = Warehouses;
            if (Warehouses.Count > 0) cmbWarehouse.SelectedIndex = 0;

            Reasons.Clear();
            foreach (var r in StockReasonsService.GetReasons("writeoff")) Reasons.Add(r);
            cmbReason.ItemsSource = Reasons;
            if (Reasons.Count > 0) cmbReason.SelectedIndex = 0;
        }

        public void LoadForEdit(int id)
        {
            _id = id;
            var w = WriteoffService.GetById(id);
            if (w == null) return;
            lblTitle.Text = "تعديل طلب شطب #" + id;
            _status = w.Status;
            cmbItem.SelectedValue = w.ItemId;
            cmbItem.IsEnabled = false;
            cmbWarehouse.SelectedValue = w.WarehouseId;
            cmbWarehouse.IsEnabled = false;
            cmbReason.SelectedValue = w.WriteoffReasonId ?? 0;
            txtQty.Text = w.Quantity.ToString("0.00");
            txtUnitCost.Text = w.UnitCost.ToString("N2");
            txtTotalValue.Text = w.TotalValue.ToString("N2");
            txtNotes.Text = w.Notes;
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            btnSave.Visibility = _status == "draft" ? Visibility.Visible : Visibility.Collapsed;
            btnSubmit.Visibility = _status == "draft" ? Visibility.Visible : Visibility.Collapsed;
            btnApprove.Visibility = _status == "pending_approval" ? Visibility.Visible : Visibility.Collapsed;
            btnExecute.Visibility = _status == "approved" ? Visibility.Visible : Visibility.Collapsed;

            cmbItem.IsEnabled = _status == "draft";
            cmbWarehouse.IsEnabled = _status == "draft";
            cmbReason.IsEnabled = _status == "draft";
            cmbAction.IsEnabled = _status == "draft";
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
                string action = (cmbAction.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "writeoff";
                if (_id == 0)
                {
                    _id = WriteoffService.Create(null, itemId, whId, qty, reasonId, action, txtNotes.Text, 1);
                    Saved = true;
                    _status = "draft";
                    LoadForEdit(_id);
                    MessageBox.Show("تم حفظ الطلب");
                }
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (_id == 0) { BtnSave_Click(sender, e); if (_id == 0) return; }
            try
            {
                WriteoffService.SubmitForApproval(_id);
                _status = "pending_approval";
                UpdateButtons();
                MessageBox.Show("تم تسليم الطلب للاعتماد");
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void BtnApprove_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                WriteoffService.Approve(_id, 1);
                _status = "approved";
                UpdateButtons();
                MessageBox.Show("تم اعتماد الطلب");
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void BtnExecute_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("تنفيذ الشطب؟ سيتم خصم الكمية من المخزون.", "تأكيد", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            try
            {
                WriteoffService.Execute(_id, 1);
                _status = "executed";
                UpdateButtons();
                MessageBox.Show("تم تنفيذ الشطب بنجاح");
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