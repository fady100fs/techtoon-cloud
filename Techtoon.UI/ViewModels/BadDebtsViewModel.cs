using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Npgsql;

namespace Techtoon.UI.ViewModels
{
    public class BadDebtRow : ViewModelBase
    {
        public int Id { get; set; }
        public string WriteoffNumber { get; set; }
        public string PartyName { get; set; }
        public decimal WriteoffAmount { get; set; }
        public DateTime WriteoffDate { get; set; }
        public string Status { get; set; }
        public string StatusDisplay => Status switch
        {
            "pending" => "بانتظار الاعتماد",
            "approved" => "معتمد",
            "posted" => "مرحّل",
            "reversed" => "معكوس",
            _ => Status
        };
    }

    public class BadDebtsViewModel : ViewModelBase
    {
        public ObservableCollection<BadDebtRow> Writeoffs { get; } = new();

        private BadDebtRow _selectedItem;
        public BadDebtRow SelectedItem { get => _selectedItem; set => SetProperty(ref _selectedItem, value); }

        private string _searchText = "";
        public string SearchText { get => _searchText; set { SetProperty(ref _searchText, value); LoadWriteoffs(); } }
        
        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public ICommand RefreshCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public BadDebtsViewModel()
        {
            RefreshCommand = new RelayCommand(LoadWriteoffs);
            AddCommand = new RelayCommand(() => OpenEditWindow(0));
            EditCommand = new RelayCommand(() => OpenEditWindow(SelectedItem?.Id ?? 0), () => SelectedItem != null);
            DeleteCommand = new RelayCommand(DeleteWriteoff, () => SelectedItem != null);
            LoadWriteoffs();
        }

        public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();

        public void LoadWriteoffs()
        {
            Writeoffs.Clear();
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                string sql = @"SELECT bd.id, COALESCE(bd.writeoff_number,''), p.name, 
                    bd.writeoff_amount, bd.writeoff_date, bd.status
                    FROM bad_debt_writeoffs bd
                    LEFT JOIN parties p ON p.id = bd.party_id
                    WHERE 1=1";
                if (!string.IsNullOrWhiteSpace(SearchText))
                    sql += " AND (bd.writeoff_number ILIKE @q OR p.name ILIKE @q)";
                sql += " ORDER BY bd.requested_at DESC LIMIT 500";
                using var cmd = new NpgsqlCommand(sql, conn);
                if (!string.IsNullOrWhiteSpace(SearchText))
                    cmd.Parameters.AddWithValue("q", "%" + SearchText + "%");
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    Writeoffs.Add(new BadDebtRow
                    {
                        Id = r.GetInt32(0),
                        WriteoffNumber = r.IsDBNull(1) ? "" : r.GetString(1),
                        PartyName = r.IsDBNull(2) ? "" : r.GetString(2),
                        WriteoffAmount = r.IsDBNull(3) ? 0 : (decimal)r.GetDouble(3),
                        WriteoffDate = r.IsDBNull(4) ? DateTime.MinValue : r.GetDateTime(4),
                        Status = r.IsDBNull(5) ? "" : r.GetString(5)
                    });
                StatusMessage = "تم تحميل " + Writeoffs.Count + " شطب";
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        private void OpenEditWindow(int id)
        {
            try
            {
                var win = new Views.BadDebtsEditWindow(id);
                win.ShowDialog();
                LoadWriteoffs();
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        private void DeleteWriteoff()
        {
            if (SelectedItem == null) return;
            if (System.Windows.MessageBox.Show("تأكيد الحذف؟", "تأكيد", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning) != System.Windows.MessageBoxResult.Yes) return;
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var cmd = new NpgsqlCommand("DELETE FROM bad_debt_writeoffs WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("id", SelectedItem.Id);
                cmd.ExecuteNonQuery();
                StatusMessage = "تم الحذف";
                LoadWriteoffs();
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }
    }
}