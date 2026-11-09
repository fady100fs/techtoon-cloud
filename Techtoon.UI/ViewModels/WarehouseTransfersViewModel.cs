using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Npgsql;

namespace Techtoon.UI.ViewModels
{
    public class WarehouseTransferRow : ViewModelBase
    {
        public int Id { get; set; }
        public string TransferNumber { get; set; }
        public DateTime Date { get; set; }
        public string FromWarehouse { get; set; }
        public string ToWarehouse { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
        public string StatusDisplay => Status switch
        {
            "draft" => "مسودة",
            "pending_approval" => "بانتظار الاعتماد",
            "approved" => "معتمد",
            "shipped" => "تم الشحن",
            "partially_received" => "استلام جزئي",
            "fully_received" => "استلام كامل",
            "cancelled" => "ملغي",
            _ => Status
        };
    }

    public class WarehouseTransfersViewModel : ViewModelBase
    {
        public ObservableCollection<WarehouseTransferRow> Transfers { get; } = new();

        private WarehouseTransferRow _selectedItem;
        public WarehouseTransferRow SelectedItem { get => _selectedItem; set => SetProperty(ref _selectedItem, value); }

        private string _searchText = "";
        public string SearchText { get => _searchText; set { SetProperty(ref _searchText, value); LoadTransfers(); } }
        
        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public ICommand RefreshCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public WarehouseTransfersViewModel()
        {
            RefreshCommand = new RelayCommand(LoadTransfers);
            AddCommand = new RelayCommand(() => OpenEditWindow(0));
            EditCommand = new RelayCommand(() => OpenEditWindow(SelectedItem?.Id ?? 0), () => SelectedItem != null);
            DeleteCommand = new RelayCommand(DeleteTransfer, () => SelectedItem != null);
            LoadTransfers();
        }

        public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();

        public void LoadTransfers()
        {
            Transfers.Clear();
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                string sql = @"SELECT wt.id, COALESCE(wt.transfer_number,''), wt.date, 
                    wf.name, wt_to.name, wt.status, COALESCE(wt.notes,'')
                    FROM warehouse_transfers wt
                    LEFT JOIN warehouses wf ON wf.id = wt.from_warehouse_id
                    LEFT JOIN warehouses wt_to ON wt_to.id = wt.to_warehouse_id
                    WHERE 1=1";
                if (!string.IsNullOrWhiteSpace(SearchText))
                    sql += " AND (wt.transfer_number ILIKE @q OR wt.notes ILIKE @q)";
                sql += " ORDER BY wt.date DESC LIMIT 500";
                using var cmd = new NpgsqlCommand(sql, conn);
                if (!string.IsNullOrWhiteSpace(SearchText))
                    cmd.Parameters.AddWithValue("q", "%" + SearchText + "%");
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    Transfers.Add(new WarehouseTransferRow
                    {
                        Id = r.GetInt32(0),
                        TransferNumber = r.IsDBNull(1) ? "" : r.GetString(1),
                        Date = r.IsDBNull(2) ? DateTime.MinValue : r.GetDateTime(2),
                        FromWarehouse = r.IsDBNull(3) ? "" : r.GetString(3),
                        ToWarehouse = r.IsDBNull(4) ? "" : r.GetString(4),
                        Status = r.IsDBNull(5) ? "" : r.GetString(5),
                        Notes = r.IsDBNull(6) ? "" : r.GetString(6)
                    });
                StatusMessage = "تم تحميل " + Transfers.Count + " تحويل";
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        private void OpenEditWindow(int id)
        {
            try
            {
                var win = new Views.WarehouseTransfersEditWindow(id);
                win.ShowDialog();
                LoadTransfers();
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        private void DeleteTransfer()
        {
            if (SelectedItem == null) return;
            if (System.Windows.MessageBox.Show("تأكيد الحذف؟", "تأكيد", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning) != System.Windows.MessageBoxResult.Yes) return;
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var cmd = new NpgsqlCommand("DELETE FROM warehouse_transfers WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("id", SelectedItem.Id);
                cmd.ExecuteNonQuery();
                StatusMessage = "تم الحذف";
                LoadTransfers();
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }
    }
}