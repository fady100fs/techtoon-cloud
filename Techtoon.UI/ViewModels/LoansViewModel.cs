using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Npgsql;

namespace Techtoon.UI.ViewModels
{
    public class LoanRow : ViewModelBase
    {
        public int Id { get; set; }
        public string BorrowerName { get; set; }
        public decimal PrincipalAmount { get; set; }
        public DateTime LoanDate { get; set; }
        public string StatusDisplay { get; set; }
    }

    public class LoansViewModel : ViewModelBase
    {
        public ObservableCollection<LoanRow> Loans { get; } = new();
        private LoanRow _selectedItem;
        public LoanRow SelectedItem { get => _selectedItem; set => SetProperty(ref _selectedItem, value); }
        private string _searchText = "";
        public string SearchText { get => _searchText; set { SetProperty(ref _searchText, value); LoadLoans(); } }
        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public ICommand RefreshCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public LoansViewModel()
        {
            RefreshCommand = new RelayCommand(LoadLoans);
            AddCommand = new RelayCommand(() => OpenEditWindow(0));
            EditCommand = new RelayCommand(() => OpenEditWindow(SelectedItem?.Id ?? 0), () => SelectedItem != null);
            DeleteCommand = new RelayCommand(DeleteLoan, () => SelectedItem != null);
            LoadLoans();
        }

        public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();

        public void LoadLoans()
        {
            Loans.Clear();
            try {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                string sql = @"SELECT id, COALESCE(borrower_name,''), principal_amount, loan_date, COALESCE(status,'active') FROM loans WHERE 1=1";
                if (!string.IsNullOrWhiteSpace(SearchText)) sql = sql.Replace("ORDER BY", " AND borrower_name ILIKE @q ORDER BY");
                sql += " ORDER BY loan_date DESC LIMIT 500";
                using var cmd = new NpgsqlCommand(sql, conn);
                if (!string.IsNullOrWhiteSpace(SearchText)) cmd.Parameters.AddWithValue("q", "%" + SearchText + "%");
                using var r = cmd.ExecuteReader();
                while (r.Read()) Loans.Add(new LoanRow {
                    Id = r.GetInt32(0), BorrowerName = r.IsDBNull(1) ? "" : r.GetString(1),
                    PrincipalAmount = r.IsDBNull(2) ? 0 : (decimal)r.GetDouble(2),
                    LoanDate = r.IsDBNull(3) ? DateTime.MinValue : r.GetDateTime(3),
                    StatusDisplay = r.IsDBNull(4) ? "" : r.GetString(4)
                });
                StatusMessage = "Loaded " + Loans.Count;
            } catch (Exception ex) { StatusMessage = "Err: " + ex.Message; }
        }

        private void OpenEditWindow(int id) { try { new Views.LoansEditWindow(id).ShowDialog(); LoadLoans(); } catch { } }
        private void DeleteLoan() {
            if (SelectedItem == null) return;
            if (System.Windows.MessageBox.Show("Confirm Delete?", "Confirm", System.Windows.MessageBoxButton.YesNo) != System.Windows.MessageBoxResult.Yes) return;
            try { using var conn = new NpgsqlConnection(GetCS()); conn.Open();
                using var cmd = new NpgsqlCommand("DELETE FROM loans WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("id", SelectedItem.Id); cmd.ExecuteNonQuery(); LoadLoans();
            } catch (Exception ex) { StatusMessage = "Err: " + ex.Message; }
        }
    }
}