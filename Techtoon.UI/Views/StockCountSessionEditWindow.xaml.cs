using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Npgsql;
using Techtoon.UI.Services;

namespace Techtoon.UI.Views
{
    public partial class StockCountSessionEditWindow : Window
    {
        private int _sessionId = 0;
        private string _status = "draft";
        public bool Saved { get; private set; }

        public ObservableCollection<CountWarehouseOption> Warehouses { get; } = new ObservableCollection<CountWarehouseOption>();
        public ObservableCollection<StockCountItem> Items { get; } = new ObservableCollection<StockCountItem>();

        public StockCountSessionEditWindow()
        {
            InitializeComponent();
            DataContext = this;
            LoadWarehouses();
            dgItems.ItemsSource = Items;
            UpdateButtons();
        }

        private void LoadWarehouses()
        {
            Warehouses.Clear();
            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT id, name FROM warehouses WHERE is_active = TRUE ORDER BY name", conn);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                Warehouses.Add(new CountWarehouseOption { Id = r.GetInt32(0), Name = r.GetString(1) });
            cmbWarehouse.ItemsSource = Warehouses;
            if (Warehouses.Count > 0) cmbWarehouse.SelectedIndex = 0;
        }

        public void LoadForEdit(int sessionId)
        {
            _sessionId = sessionId;
            lblTitle.Text = "تعديل جلسة جرد #" + sessionId;

            try
            {
                using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
                conn.Open();
                int whId; bool hideBook;
                using (var cmd = new NpgsqlCommand(
                    "SELECT warehouse_id, count_type, status, book_balance_locked, notes FROM stock_count_sessions WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("id", sessionId);
                    using var r = cmd.ExecuteReader();
                    if (r.Read())
                    {
                        whId = r.GetInt32(0);
                        cmbCountType.SelectedIndex = r.GetString(1) == "partial" ? 1 : (r.GetString(1) == "surprise" ? 2 : 0);
                        _status = r.GetString(2);
                        hideBook = r.GetBoolean(3);
                        txtNotes.Text = r.IsDBNull(4) ? "" : r.GetString(4);
                    }
                    else return;
                }

                cmbWarehouse.SelectedValue = whId;
                cmbWarehouse.IsEnabled = false;
                cmbCountType.IsEnabled = false;
                chkHideBook.IsChecked = hideBook;
                chkHideBook.IsEnabled = false;

                // Load items
                Items.Clear();
                var items = InventoryCountService.GetSessionItems(sessionId, hideBook);
                foreach (var it in items) Items.Add(it);
                colBook.Visibility = hideBook ? Visibility.Collapsed : Visibility.Visible;

                UpdateButtons();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void UpdateButtons()
        {
            btnStart.Visibility = _status == "draft" ? Visibility.Visible : Visibility.Collapsed;
            btnSubmit.Visibility = _status == "in_progress" ? Visibility.Visible : Visibility.Collapsed;
            btnApprove.Visibility = _status == "under_review" ? Visibility.Visible : Visibility.Collapsed;
            btnReject.Visibility = _status == "under_review" ? Visibility.Visible : Visibility.Collapsed;
            btnExecute.Visibility = _status == "approved" ? Visibility.Visible : Visibility.Collapsed;
            btnSaveCounts.Visibility = (_status == "in_progress" || _status == "draft") ? Visibility.Visible : Visibility.Collapsed;
            dgItems.IsReadOnly = (_status == "approved" || _status == "executed");
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (_sessionId == 0) { CreateSession(); if (_sessionId == 0) return; }
            try
            {
                InventoryCountService.StartSession(_sessionId, 1);
                _status = "in_progress";
                LoadForEdit(_sessionId);
                MessageBox.Show("بدأ الجرد. أدخل الكميات الفعلية.");
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void CreateSession()
        {
            if (cmbWarehouse.SelectedValue == null) { MessageBox.Show("اختر المخزن"); return; }
            int whId = (int)cmbWarehouse.SelectedValue;
            string ct = (cmbCountType.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "full";
            bool hideBook = chkHideBook.IsChecked ?? false;
            try
            {
                _sessionId = InventoryCountService.CreateSession(whId, ct, hideBook, txtNotes.Text, 1);
                Saved = true;
                LoadForEdit(_sessionId);
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                InventoryCountService.SubmitForReview(_sessionId);
                _status = "under_review";
                UpdateButtons();
                MessageBox.Show("تم تسليم الجلسة للمراجعة");
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void BtnApprove_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                InventoryCountService.ApproveSession(_sessionId, 1);
                _status = "approved";
                UpdateButtons();
                MessageBox.Show("تم اعتماد الجلسة. يمكنك الآن تنفيذ التسويات.");
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void BtnReject_Click(object sender, RoutedEventArgs e)
        {
            var reason = Microsoft.VisualBasic.Interaction.InputBox("سبب الرفض:", "رفض الجلسة", "", -1, -1);
            if (string.IsNullOrEmpty(reason)) return;
            try
            {
                InventoryCountService.RejectSession(_sessionId, reason);
                _status = "in_progress";
                UpdateButtons();
                MessageBox.Show("تم رفض الجلسة وإعادتها للعد");
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void BtnExecute_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("تنفيذ التسويات؟ سيتم تعديل أرصدة المخزون.", "تأكيد", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            try
            {
                InventoryCountService.ExecuteSession(_sessionId, 1);
                _status = "executed";
                UpdateButtons();
                MessageBox.Show("تم تنفيذ التسويات بنجاح");
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void BtnSaveCounts_Click(object sender, RoutedEventArgs e)
        {
            int saved = 0;
            foreach (var item in Items)
            {
                if (item.CountedQuantity.HasValue)
                {
                    try
                    {
                        InventoryCountService.UpdateCountedQuantity(item.Id, item.CountedQuantity.Value, 1);
                        saved++;
                    }
                    catch { }
                }
            }
            // Refresh
            if (_sessionId > 0) LoadForEdit(_sessionId);
            MessageBox.Show("تم حفظ " + saved + " صنف");
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            if (Saved) DialogResult = true;
            Close();
        }
    }

    public class CountWarehouseOption
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}