using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Npgsql;

namespace Techtoon.UI.ViewModels
{
    public class LateFeePolicyRow : ViewModelBase
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string FeeType { get; set; }
        public decimal FeeValue { get; set; }
        public int GracePeriodDays { get; set; }
        public string CalculationFrequency { get; set; }
        public bool IsActive { get; set; }
        public string FeeTypeDisplay => FeeType == "percentage" ? "نسبة %" : "مبلغ ثابت";
        public string FrequencyDisplay => CalculationFrequency switch
        {
            "one_time" => "مرة واحدة",
            "daily" => "يومي",
            "weekly" => "أسبوعي",
            "monthly" => "شهري",
            _ => CalculationFrequency
        };
    }

    public class LatePaymentFeesViewModel : ViewModelBase
    {
        public ObservableCollection<LateFeePolicyRow> Policies { get; } = new();

        private LateFeePolicyRow _selectedItem;
        public LateFeePolicyRow SelectedItem { get => _selectedItem; set => SetProperty(ref _selectedItem, value); }

        private string _searchText = "";
        public string SearchText { get => _searchText; set { SetProperty(ref _searchText, value); LoadPolicies(); } }
        
        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public ICommand RefreshCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public LatePaymentFeesViewModel()
        {
            RefreshCommand = new RelayCommand(LoadPolicies);
            AddCommand = new RelayCommand(() => OpenEditWindow(0));
            EditCommand = new RelayCommand(() => OpenEditWindow(SelectedItem?.Id ?? 0), () => SelectedItem != null);
            DeleteCommand = new RelayCommand(DeletePolicy, () => SelectedItem != null);
            LoadPolicies();
        }

        public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();

        public void LoadPolicies()
        {
            Policies.Clear();
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                string sql = @"SELECT id, name, fee_type, fee_value, grace_period_days, calculation_frequency, is_active
                    FROM late_fee_policies WHERE 1=1";
                if (!string.IsNullOrWhiteSpace(SearchText))
                    sql += " AND name ILIKE @q";
                sql += " ORDER BY created_at DESC LIMIT 500";
                using var cmd = new NpgsqlCommand(sql, conn);
                if (!string.IsNullOrWhiteSpace(SearchText))
                    cmd.Parameters.AddWithValue("q", "%" + SearchText + "%");
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    Policies.Add(new LateFeePolicyRow
                    {
                        Id = r.GetInt32(0),
                        Name = r.IsDBNull(1) ? "" : r.GetString(1),
                        FeeType = r.IsDBNull(2) ? "" : r.GetString(2),
                        FeeValue = r.IsDBNull(3) ? 0 : (decimal)r.GetDouble(3),
                        GracePeriodDays = r.IsDBNull(4) ? 0 : r.GetInt32(4),
                        CalculationFrequency = r.IsDBNull(5) ? "" : r.GetString(5),
                        IsActive = r.IsDBNull(6) ? false : r.GetBoolean(6)
                    });
                StatusMessage = "تم تحميل " + Policies.Count + " سياسة";
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        private void OpenEditWindow(int id)
        {
            try
            {
                var win = new Views.LatePaymentFeesEditWindow(id);
                win.ShowDialog();
                LoadPolicies();
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        private void DeletePolicy()
        {
            if (SelectedItem == null) return;
            if (System.Windows.MessageBox.Show("تأكيد الحذف؟", "تأكيد", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning) != System.Windows.MessageBoxResult.Yes) return;
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var cmd = new NpgsqlCommand("DELETE FROM late_fee_policies WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("id", SelectedItem.Id);
                cmd.ExecuteNonQuery();
                StatusMessage = "تم الحذف";
                LoadPolicies();
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }
    }
}