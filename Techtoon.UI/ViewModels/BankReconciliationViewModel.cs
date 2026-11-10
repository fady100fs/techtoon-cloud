using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Npgsql;

namespace Techtoon.UI.ViewModels
{
    public class ReconciliationRow : ViewModelBase
    {
        public int Id { get; set; }
        public string ReconciliationNumber { get; set; }
        public string CashBoxName { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal Difference { get; set; }
        public string Status { get; set; }
        public string StatusDisplay => Status switch
        {
            "draft" => "مسودة",
            "under_review" => "تحت المراجعة",
            "approved" => "معتمدة",
            "locked" => "مقفلة",
            _ => Status
        };
    }

    public class BankReconciliationViewModel : ViewModelBase
    {
        public ObservableCollection<ReconciliationRow> Reconciliations { get; } = new();

        private ReconciliationRow _selectedItem;
        public ReconciliationRow SelectedItem { get => _selectedItem; set => SetProperty(ref _selectedItem, value); }

        private string _searchText = "";
        public string SearchText { get => _searchText; set { SetProperty(ref _searchText, value); LoadReconciliations(); } }
        
        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public ICommand RefreshCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public BankReconciliationViewModel()
        {
            RefreshCommand = new RelayCommand(LoadReconciliations);
            AddCommand = new RelayCommand(() => OpenEditWindow(0));
            EditCommand = new RelayCommand(() => OpenEditWindow(SelectedItem?.Id ?? 0), () => SelectedItem != null);
            DeleteCommand = new RelayCommand(DeleteReconciliation, () => SelectedItem != null);
            LoadReconciliations();
        }

        public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();

        public void LoadReconciliations()
        {
            Reconciliations.Clear();
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                string sql = @"SELECT br.id, COALESCE(br.reconciliation_number,''), cb.name, 
                    br.period_start, br.period_end, br.difference, br.status
                    FROM bank_reconciliations br
                    LEFT JOIN cash_boxes cb ON cb.id = br.cash_box_id
                    WHERE 1=1";
                if (!string.IsNullOrWhiteSpace(SearchText))
                    sql += " AND (br.reconciliation_number ILIKE @q OR cb.name ILIKE @q)";
                sql += " ORDER BY br.created_at DESC LIMIT 500";
                using var cmd = new NpgsqlCommand(sql, conn);
                if (!string.IsNullOrWhiteSpace(SearchText))
                    cmd.Parameters.AddWithValue("q", "%" + SearchText + "%");
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    Reconciliations.Add(new ReconciliationRow
                    {
                        Id = r.GetInt32(0),
                        ReconciliationNumber = r.IsDBNull(1) ? "" : r.GetString(1),
                        CashBoxName = r.IsDBNull(2) ? "" : r.GetString(2),
                        PeriodStart = r.IsDBNull(3) ? DateTime.MinValue : Techtoon.Core.Data.DbValue.AsDateTime(r.GetValue(3)),
                        PeriodEnd = r.IsDBNull(4) ? DateTime.MinValue : Techtoon.Core.Data.DbValue.AsDateTime(r.GetValue(4)),
                        Difference = r.IsDBNull(5) ? 0 : (decimal)r.GetDouble(5),
                        Status = r.IsDBNull(6) ? "" : r.GetString(6)
                    });
                StatusMessage = "تم تحميل " + Reconciliations.Count + " تسوية";
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        private void OpenEditWindow(int id)
        {
            try
            {
                var win = new Views.BankReconciliationEditWindow(id);
                win.ShowDialog();
                LoadReconciliations();
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        private void DeleteReconciliation()
        {
            if (SelectedItem == null) return;
            if (System.Windows.MessageBox.Show("تأكيد الحذف؟", "تأكيد", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning) != System.Windows.MessageBoxResult.Yes) return;
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var cmd = new NpgsqlCommand("DELETE FROM bank_reconciliations WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("id", SelectedItem.Id);
                cmd.ExecuteNonQuery();
                StatusMessage = "تم الحذف";
                LoadReconciliations();
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }
    }
}