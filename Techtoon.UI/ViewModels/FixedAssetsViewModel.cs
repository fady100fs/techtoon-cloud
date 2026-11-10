using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Npgsql;
using Techtoon.Core.Data;
using Techtoon.UI.Services;

namespace Techtoon.UI.ViewModels
{
    /// <summary>سطر أصل ثابت (fixed_assets) مع القيم المحسوبة للإهلاك.</summary>
    public class FixedAssetRow : ViewModelBase
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Code { get; set; } = "";
        public string Category { get; set; } = "";
        public DateTime PurchaseDate { get; set; }
        public decimal PurchaseCost { get; set; }
        public decimal SalvageValue { get; set; }
        public int UsefulLifeYears { get; set; }
        public decimal AccumulatedDepreciation { get; set; }
        public decimal NetBookValue { get; set; }
        public string Location { get; set; } = "";
        public string ResponsiblePerson { get; set; } = "";
        public string Status { get; set; } = "active";
        public string Notes { get; set; } = "";

        /// <summary>الأساس القابل للإهلاك = التكلفة − القيمة المتبقية.</summary>
        public decimal DepreciableBase => Math.Max(0m, PurchaseCost - SalvageValue);

        /// <summary>الإهلاك الشهري (قسط طريقة القسط الثابت).</summary>
        public decimal MonthlyDepreciation => UsefulLifeYears > 0
            ? Math.Round(DepreciableBase / UsefulLifeYears / 12m, 2, MidpointRounding.AwayFromZero)
            : 0m;

        /// <summary>المتبقي القابل للإهلاك.</summary>
        public decimal RemainingDepreciable => Math.Max(0m, DepreciableBase - AccumulatedDepreciation);

        /// <summary>هل يمكن تنفيذ إهلاك جديد لهذا الأصل؟</summary>
        public bool CanDepreciate => Status != "fully_depreciated"
            && MonthlyDepreciation > 0m
            && RemainingDepreciable > 0.004m;

        public string PurchaseDateDisplay => PurchaseDate == default ? "" : PurchaseDate.ToString("yyyy-MM-dd");
        public string PurchaseCostDisplay => PurchaseCost.ToString("N2");
        public string SalvageDisplay => SalvageValue.ToString("N2");
        public string AccumulatedDisplay => AccumulatedDepreciation.ToString("N2");
        public string NetBookDisplay => NetBookValue.ToString("N2");
        public string MonthlyDisplay => MonthlyDepreciation.ToString("N2");
        public string StatusDisplay => Status switch
        {
            "active" => "نشط",
            "fully_depreciated" => "مُستهلك بالكامل",
            _ => Status ?? ""
        };
    }

    /// <summary>سجل إهلاك واحد (depreciation_records).</summary>
    public class DepreciationRecordRow : ViewModelBase
    {
        public int Id { get; set; }
        public int AssetId { get; set; }
        public DateTime Date { get; set; }
        public decimal DepreciationAmount { get; set; }
        public decimal AccumulatedDepreciation { get; set; }
        public decimal NetBookValue { get; set; }
        public string Notes { get; set; } = "";

        public string DateDisplay => Date == default ? "" : Date.ToString("yyyy-MM-dd");
        public string AmountDisplay => DepreciationAmount.ToString("N2");
        public string AccumulatedDisplay => AccumulatedDepreciation.ToString("N2");
        public string NetBookDisplay => NetBookValue.ToString("N2");
    }

    /// <summary>
    /// B3 — الأصول الثابتة والإهلاك: إدارة fixed_assets + تسجيل الإهلاك الشهري (القسط الثابت)
    /// في معاملة واحدة مع تحديث depreciation_records و fixed_assets.
    /// </summary>
    public class FixedAssetsViewModel : ViewModelBase
    {
        public ObservableCollection<FixedAssetRow> Assets { get; } = new();
        public ObservableCollection<DepreciationRecordRow> DepreciationHistory { get; } = new();

        private FixedAssetRow? _selectedItem;
        public FixedAssetRow? SelectedItem
        {
            get => _selectedItem;
            set { if (SetProperty(ref _selectedItem, value)) LoadDepreciationHistory(); }
        }

        private string _searchText = "";
        public string SearchText { get => _searchText; set { if (SetProperty(ref _searchText, value)) LoadAssets(); } }

        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        private string _historyTitle = "";
        public string HistoryTitle { get => _historyTitle; set => SetProperty(ref _historyTitle, value); }

        public ICommand RefreshCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand DepreciateCommand { get; }
        public ICommand HistoryCommand { get; }

        public FixedAssetsViewModel()
        {
            RefreshCommand = new UiRelayCommand(_ => { LoadAssets(); LoadDepreciationHistory(); });
            AddCommand = new UiRelayCommand(_ => OpenEditWindow(0));
            EditCommand = new UiRelayCommand(_ => OpenEditWindow(SelectedItem?.Id ?? 0), _ => SelectedItem != null);
            DeleteCommand = new UiRelayCommand(_ => DeleteAsset(), _ => SelectedItem != null);
            DepreciateCommand = new UiRelayCommand(
                p => Depreciate(p as FixedAssetRow),
                p => (p as FixedAssetRow)?.CanDepreciate == true);
            HistoryCommand = new UiRelayCommand(_ => LoadDepreciationHistory());

            LoadAssets();
        }

        public static string GetCS() => SecureConnectionManager.GetConnectionString();

        public void LoadAssets()
        {
            int keepId = SelectedItem?.Id ?? 0;
            Assets.Clear();
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                string sql = @"SELECT f.id, COALESCE(f.name,''), COALESCE(f.code,''), COALESCE(f.category,''),
                                      f.purchase_date, f.purchase_cost, COALESCE(f.salvage_value,0), f.useful_life_years,
                                      COALESCE(f.accumulated_depreciation,0), f.net_book_value,
                                      COALESCE(f.location,''), COALESCE(f.responsible_person,''),
                                      COALESCE(f.status,'active'), COALESCE(f.notes,'')
                               FROM fixed_assets f
                               WHERE 1=1";
                if (!string.IsNullOrWhiteSpace(SearchText))
                    sql += " AND (f.name ILIKE @q OR f.code ILIKE @q OR COALESCE(f.category,'') ILIKE @q OR COALESCE(f.responsible_person,'') ILIKE @q)";
                sql += " ORDER BY f.code LIMIT 1000";

                using var cmd = new NpgsqlCommand(sql, conn);
                if (!string.IsNullOrWhiteSpace(SearchText))
                    cmd.Parameters.AddWithValue("q", "%" + SearchText + "%");

                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    Assets.Add(new FixedAssetRow
                    {
                        Id = BudgetAssetsDb.ToInt(r.GetValue(0)),
                        Name = BudgetAssetsDb.ToText(r.GetValue(1)),
                        Code = BudgetAssetsDb.ToText(r.GetValue(2)),
                        Category = BudgetAssetsDb.ToText(r.GetValue(3)),
                        PurchaseDate = DbValue.AsDateTime(r.GetValue(4)),
                        PurchaseCost = BudgetAssetsDb.ToDecimal(r.GetValue(5)),
                        SalvageValue = BudgetAssetsDb.ToDecimal(r.GetValue(6)),
                        UsefulLifeYears = BudgetAssetsDb.ToInt(r.GetValue(7)),
                        AccumulatedDepreciation = BudgetAssetsDb.ToDecimal(r.GetValue(8)),
                        NetBookValue = BudgetAssetsDb.ToDecimal(r.GetValue(9)),
                        Location = BudgetAssetsDb.ToText(r.GetValue(10)),
                        ResponsiblePerson = BudgetAssetsDb.ToText(r.GetValue(11)),
                        Status = BudgetAssetsDb.ToText(r.GetValue(12)),
                        Notes = BudgetAssetsDb.ToText(r.GetValue(13))
                    });
                }
                StatusMessage = "تم تحميل " + Assets.Count + " أصل ثابت";
                if (keepId > 0)
                {
                    var keep = Assets.FirstOrDefault(a => a.Id == keepId);
                    if (keep != null) SelectedItem = keep;
                }
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        /// <summary>سجل الإهلاك للأصل المحدد.</summary>
        public void LoadDepreciationHistory()
        {
            DepreciationHistory.Clear();
            var asset = SelectedItem;
            if (asset == null)
            {
                HistoryTitle = "سجل الإهلاك — اختر أصلًا لعرض سجله";
                return;
            }
            HistoryTitle = "سجل الإهلاك للأصل: " + asset.Code + " - " + asset.Name;
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var cmd = new NpgsqlCommand(@"SELECT id, asset_id, date, depreciation_amount,
                                                      accumulated_depreciation, net_book_value, COALESCE(notes,'')
                                               FROM depreciation_records
                                               WHERE asset_id = @id
                                               ORDER BY date DESC, id DESC", conn);
                cmd.Parameters.AddWithValue("id", asset.Id);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    DepreciationHistory.Add(new DepreciationRecordRow
                    {
                        Id = BudgetAssetsDb.ToInt(r.GetValue(0)),
                        AssetId = BudgetAssetsDb.ToInt(r.GetValue(1)),
                        Date = DbValue.AsDateTime(r.GetValue(2)),
                        DepreciationAmount = BudgetAssetsDb.ToDecimal(r.GetValue(3)),
                        AccumulatedDepreciation = BudgetAssetsDb.ToDecimal(r.GetValue(4)),
                        NetBookValue = BudgetAssetsDb.ToDecimal(r.GetValue(5)),
                        Notes = BudgetAssetsDb.ToText(r.GetValue(6))
                    });
                }
            }
            catch (Exception ex) { StatusMessage = "خطأ في تحميل سجل الإهلاك: " + ex.Message; }
        }

        private void OpenEditWindow(int id)
        {
            try
            {
                var win = new Views.FixedAssetEditWindow(id);
                if (win.ShowDialog() == true)
                {
                    StatusMessage = id > 0 ? "تم تعديل الأصل الثابت" : "تمت إضافة الأصل الثابت";
                    LoadAssets();
                    LoadDepreciationHistory();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("خطأ: " + ex.Message, "خطأ",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void Depreciate(FixedAssetRow? row)
        {
            if (row == null) return;
            try
            {
                var win = new Views.DepreciationWindow(row.Id, row.Code, row.Name);
                if (win.ShowDialog() == true)
                {
                    StatusMessage = "تم تسجيل الإهلاك للأصل " + row.Code;
                    LoadAssets();
                    LoadDepreciationHistory();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("خطأ: " + ex.Message, "خطأ",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void DeleteAsset()
        {
            var row = SelectedItem;
            if (row == null) return;
            try
            {
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM depreciation_records WHERE asset_id=@id", conn);
                    cmd.Parameters.AddWithValue("id", row.Id);
                    var count = Convert.ToInt64(cmd.ExecuteScalar() ?? 0L);
                    if (count > 0)
                    {
                        System.Windows.MessageBox.Show(
                            "لا يمكن حذف الأصل (" + row.Code + ") لأنه مرتبط بـ " + count + " سجل إهلاك.",
                            "تعذّر الحذف", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                        return;
                    }
                }

                if (System.Windows.MessageBox.Show(
                        "تأكيد حذف الأصل الثابت (" + row.Code + " - " + row.Name + ")؟",
                        "تأكيد", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning)
                    != System.Windows.MessageBoxResult.Yes) return;

                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using var cmd = new NpgsqlCommand("DELETE FROM fixed_assets WHERE id=@id", conn);
                    cmd.Parameters.AddWithValue("id", row.Id);
                    cmd.ExecuteNonQuery();
                }

                AuditService.Log("DELETE", "FixedAsset", row.Id,
                    "Fixed asset deleted: " + row.Code + " - " + row.Name + ", cost=" + row.PurchaseCost.ToString("N2"));

                StatusMessage = "تم حذف الأصل الثابت";
                LoadAssets();
                LoadDepreciationHistory();
            }
            catch (Npgsql.PostgresException px)
            {
                System.Windows.MessageBox.Show("خطأ في قاعدة البيانات: " + px.Message, "خطأ",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("خطأ: " + ex.Message, "خطأ",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
