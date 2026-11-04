using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Npgsql;
using Techtoon.UI.Services;

namespace Techtoon.UI.ViewModels
{
    public class StockLevelRow
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public string Barcode { get; set; }
        public string Warehouse { get; set; }
        public double TotalQuantity { get; set; }
        public double Reserved { get; set; }
        public double Damaged { get; set; }
        public double UnderInspection { get; set; }
        public double Available { get { return TotalQuantity - Reserved - Damaged - UnderInspection; } }
        public decimal UnitCost { get; set; }
        public decimal StockValue { get { return (decimal)TotalQuantity * UnitCost; } }
        public double MinStock { get; set; }
        public bool IsLowStock { get { return TotalQuantity <= MinStock; } }

        public string TotalDisplay { get { return TotalQuantity.ToString("0.00"); } }
        public string AvailableDisplay { get { return Available.ToString("0.00"); } }
        public string ReservedDisplay { get { return Reserved.ToString("0.00"); } }
        public string DamagedDisplay { get { return Damaged.ToString("0.00"); } }
        public string UnderInspectionDisplay { get { return UnderInspection.ToString("0.00"); } }
        public string StockValueDisplay { get { return StockValue.ToString("N2"); } }
    }

    public class StockLevelsViewModel : ViewModelBase
    {
        public ObservableCollection<StockLevelRow> Levels { get; set; }
        private string _search = "";
        public string SearchText { get => _search; set { _search = value; OnPropertyChanged("SearchText"); } }

        public ICommand RefreshCommand { get; set; }
        public ICommand SearchCommand { get; set; }

        public StockLevelsViewModel()
        {
            Levels = new ObservableCollection<StockLevelRow>();
            RefreshCommand = new UiRelayCommand(_ => Load());
            SearchCommand = new UiRelayCommand(_ => Load());
            Load();
        }

        public void Load()
        {
            Levels.Clear();
            try
            {
                using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
                conn.Open();
                var sql = @"SELECT i.id, i.name, i.barcode, w.name, 
                            COALESCE(i.opening_quantity,0) + COALESCE((SELECT SUM(CASE WHEN type IN ('in','opening') THEN quantity WHEN type='out' THEN -quantity WHEN type='adjustment' THEN quantity ELSE 0 END) FROM inventory_movements WHERE item_id=i.id),0),
                            COALESCE(sl.reserved_quantity,0), COALESCE(sl.damaged_quantity,0), COALESCE(sl.under_inspection_quantity,0),
                            COALESCE(i.avg_cost_price, i.cost_price, 0), COALESCE(i.min_stock,0)
                            FROM items i 
                            CROSS JOIN warehouses w
                            LEFT JOIN stock_levels sl ON sl.item_id=i.id AND sl.warehouse_id=w.id
                            WHERE i.is_active = TRUE AND i.is_kit IS NOT TRUE AND w.is_active = TRUE";
                if (!string.IsNullOrWhiteSpace(SearchText))
                    sql += " AND (i.name ILIKE @q OR i.barcode ILIKE @q)";
                sql += " ORDER BY i.name";

                using var cmd = new NpgsqlCommand(sql, conn);
                if (!string.IsNullOrWhiteSpace(SearchText))
                    cmd.Parameters.AddWithValue("q", "%" + SearchText + "%");
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    Levels.Add(new StockLevelRow
                    {
                        ItemId = r.GetInt32(0),
                        ItemName = r.GetString(1),
                        Barcode = r.IsDBNull(2) ? "" : r.GetString(2),
                        Warehouse = r.GetString(3),
                        TotalQuantity = r.GetDouble(4),
                        Reserved = r.GetDouble(5),
                        Damaged = r.GetDouble(6),
                        UnderInspection = r.GetDouble(7),
                        UnitCost = (decimal)r.GetDouble(8),
                        MinStock = r.GetDouble(9)
                    });
                }
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }
    }
}