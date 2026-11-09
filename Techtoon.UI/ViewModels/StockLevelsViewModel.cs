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
        public decimal TotalQuantity { get; set; }
        public decimal Reserved { get; set; }
        public decimal Damaged { get; set; }
        public decimal UnderInspection { get; set; }
        public decimal Available { get { return TotalQuantity - Reserved - Damaged - UnderInspection; } }
        public decimal UnitCost { get; set; }
        public decimal StockValue { get { return TotalQuantity * UnitCost; } }
        public decimal MinStock { get; set; }
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
        public ICommand NextPageCommand { get; set; }
        public ICommand PrevPageCommand { get; set; }

        // P0-4 Batch 10: pagination contract مع API
        private int _page = 1;
        public int Page { get => _page; set { if (SetProperty(ref _page, System.Math.Max(1, value))) { OnPropertyChanged(nameof(TotalPages)); OnPropertyChanged(nameof(PageInfo)); } } }
        private int _pageSize = 50;
        public int PageSize { get => _pageSize; set { if (SetProperty(ref _pageSize, System.Math.Max(1, value))) { OnPropertyChanged(nameof(TotalPages)); OnPropertyChanged(nameof(PageInfo)); } } }
        private int _totalCount;
        public int TotalCount { get => _totalCount; set { if (SetProperty(ref _totalCount, value)) { OnPropertyChanged(nameof(TotalPages)); OnPropertyChanged(nameof(PageInfo)); } } }
        public int TotalPages => PageSize > 0 ? (int)System.Math.Ceiling(TotalCount / (double)PageSize) : 0;
        public string PageInfo => TotalPages > 0 ? $"صفحة {Page} من {TotalPages} ({TotalCount} صنف)" : $"صفحة {Page} (0 صنف)";

        public StockLevelsViewModel()
        {
            Levels = new ObservableCollection<StockLevelRow>();
            RefreshCommand = new UiRelayCommand(_ => _ = LoadAsync());
            SearchCommand = new UiRelayCommand(_ => { Page = 1; _ = LoadAsync(); });
            NextPageCommand = new UiRelayCommand(_ => { if (TotalPages == 0 || Page < TotalPages) { Page++; _ = LoadAsync(); } });
            PrevPageCommand = new UiRelayCommand(_ => { if (Page > 1) { Page--; _ = LoadAsync(); } });
            _ = LoadAsync();
        }

        public async System.Threading.Tasks.Task LoadAsync()
        {
            Levels.Clear();
            try
            {
                using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
                await conn.OpenAsync();
                string fromWhere = @"FROM items i CROSS JOIN warehouses w LEFT JOIN stock_levels sl ON sl.item_id=i.id AND sl.warehouse_id=w.id WHERE i.is_active = TRUE AND i.is_kit IS NOT TRUE AND w.is_active = TRUE";
                if (!string.IsNullOrWhiteSpace(SearchText))
                    fromWhere += " AND (i.name ILIKE @q OR i.barcode ILIKE @q)";
                using (var cc = new NpgsqlCommand("SELECT COUNT(*) " + fromWhere.Replace("FROM items", "FROM items"), conn))
                {
                    if (!string.IsNullOrWhiteSpace(SearchText)) cc.Parameters.AddWithValue("q", "%" + SearchText + "%");
                    TotalCount = Convert.ToInt32(await cc.ExecuteScalarAsync());
                }
                if (TotalPages > 0 && Page > TotalPages) Page = TotalPages;
                var sql = @"SELECT i.id, i.name, i.barcode, w.name, 
                            COALESCE(i.opening_quantity,0) + COALESCE((SELECT SUM(CASE WHEN type IN ('in','opening','transfer_in') THEN ABS(quantity) WHEN type IN ('out','transfer_out') THEN -ABS(quantity) WHEN type='adjustment' THEN quantity ELSE 0 END) FROM inventory_movements WHERE item_id=i.id),0),
                            COALESCE(sl.reserved_quantity,0), COALESCE(sl.damaged_quantity,0), COALESCE(sl.under_inspection_quantity,0),
                            COALESCE(i.avg_cost_price, i.cost_price, 0), COALESCE(i.min_stock,0) " + fromWhere + " ORDER BY i.name LIMIT @limit OFFSET @offset";

                using var cmd = new NpgsqlCommand(sql, conn);
                if (!string.IsNullOrWhiteSpace(SearchText))
                    cmd.Parameters.AddWithValue("q", "%" + SearchText + "%");
                cmd.Parameters.AddWithValue("limit", PageSize);
                cmd.Parameters.AddWithValue("offset", (Page - 1) * PageSize);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    Levels.Add(new StockLevelRow
                    {
                        ItemId = r.GetInt32(0),
                        ItemName = r.GetString(1),
                        Barcode = r.IsDBNull(2) ? "" : r.GetString(2),
                        Warehouse = r.GetString(3),
                        TotalQuantity = Convert.ToDecimal(r.GetValue(4)),
                        Reserved = Convert.ToDecimal(r.GetValue(5)),
                        Damaged = Convert.ToDecimal(r.GetValue(6)),
                        UnderInspection = Convert.ToDecimal(r.GetValue(7)),
                        UnitCost = Convert.ToDecimal(r.GetValue(8)),
                        MinStock = Convert.ToDecimal(r.GetValue(9))
                    });
                }
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }
    }
}