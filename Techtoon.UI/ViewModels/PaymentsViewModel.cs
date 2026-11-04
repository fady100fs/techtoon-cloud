using System;
using System.Collections.ObjectModel;
using System.Windows;
using Npgsql;
using Techtoon.UI.Views;

namespace Techtoon.UI.ViewModels
{
    public class PaymentRow
    {
        public int Id { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PartyName { get; set; }
        public string AccountName { get; set; }
        public string VoucherType { get; set; }
        public decimal Amount { get; set; }
        public string Method { get; set; }
        public string Reference { get; set; }
        public string Notes { get; set; }
        
        public string TargetName { get { return string.IsNullOrEmpty(PartyName) ? AccountName : PartyName; } }
    }

    public class PaymentsViewModel : ViewModelBase
    {
        private readonly string _connectionString =
            "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;" +
            "Username=neondb_owner;Password=npg_K9nHVkY5OjRC;Database=neondb;" +
            "SSL Mode=Require;Trust Server Certificate=true";

        private ObservableCollection<PaymentRow> _payments;
        private PaymentRow _selectedPayment;
        private string _searchText;

        public ObservableCollection<PaymentRow> Payments
        {
            get => _payments;
            set => SetProperty(ref _payments, value);
        }

        public PaymentRow SelectedPayment
        {
            get => _selectedPayment;
            set => SetProperty(ref _selectedPayment, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                    LoadPayments();
            }
        }

        public UiRelayCommand AddReceiptCommand { get; }
        public UiRelayCommand AddPaymentCommand { get; }
        public UiRelayCommand EditCommand { get; }
        public UiRelayCommand DeleteCommand { get; }
        public UiRelayCommand RefreshCommand { get; }

        public PaymentsViewModel()
        {
            AddReceiptCommand = new UiRelayCommand(_ => AddReceipt(), null);
            AddPaymentCommand = new UiRelayCommand(_ => AddPayment(), null);
            EditCommand = new UiRelayCommand(_ => Edit(), _ => SelectedPayment != null);
            DeleteCommand = new UiRelayCommand(_ => Delete(), _ => SelectedPayment != null);
            RefreshCommand = new UiRelayCommand(_ => LoadPayments(), null);
            LoadPayments();
        }

        public void LoadPayments()
        {
            Payments = new ObservableCollection<PaymentRow>();
            NpgsqlConnection conn = null;
            try
            {
                conn = new NpgsqlConnection(_connectionString);
                conn.Open();
                string sql =
                    "SELECT p.id, p.date, COALESCE(pt.name,''), COALESCE(a.name,''), p.payment_type, p.amount, " +
                    "p.payment_method, p.reference_number, p.notes " +
                    "FROM payments p LEFT JOIN parties pt ON p.party_id = pt.id " +
                    "LEFT JOIN accounts a ON p.account_id = a.id ";

                if (!string.IsNullOrWhiteSpace(SearchText))
                    sql += "WHERE pt.name ILIKE @q OR a.name ILIKE @q OR p.reference_number ILIKE @q ";
                sql += "ORDER BY p.date DESC LIMIT 500";

                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    if (!string.IsNullOrWhiteSpace(SearchText))
                        cmd.Parameters.AddWithValue("@q", "%" + SearchText + "%");
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Payments.Add(new PaymentRow
                            {
                                Id = reader.GetInt32(0),
                                PaymentDate = reader.GetDateTime(1),
                                PartyName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                AccountName = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                VoucherType = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                Amount = reader.IsDBNull(5) ? 0m : (decimal)reader.GetDouble(5),
                                Method = reader.IsDBNull(6) ? "" : reader.GetString(6),
                                Reference = reader.IsDBNull(7) ? "" : reader.GetString(7),
                                Notes = reader.IsDBNull(8) ? "" : reader.GetString(8)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading payments: " + ex.Message);
            }
            finally
            {
                if (conn != null) conn.Close();
            }
        }

        private void AddReceipt()
        {
            var win = new PaymentEditWindow();
            win.SetMode(true);
            win.Owner = Application.Current.MainWindow;
            if (win.ShowDialog() == true) LoadPayments();
        }

        private void AddPayment()
        {
            var win = new PaymentEditWindow();
            win.SetMode(false);
            win.Owner = Application.Current.MainWindow;
            if (win.ShowDialog() == true) LoadPayments();
        }

        private void Edit()
        {
            if (SelectedPayment == null) return;
            var win = new PaymentEditWindow();
            win.Owner = Application.Current.MainWindow;
            if (win.ShowDialog() == true) LoadPayments();
        }

        private void Delete()
        {
            if (SelectedPayment == null) return;
            var result = MessageBox.Show("Delete payment #" + SelectedPayment.Id + "?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            NpgsqlConnection conn = null;
            try
            {
                conn = new NpgsqlConnection(_connectionString);
                conn.Open();
                using (var cmd = new NpgsqlCommand("DELETE FROM payments WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", SelectedPayment.Id);
                    cmd.ExecuteNonQuery();
                }
                LoadPayments();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
            finally
            {
                if (conn != null) conn.Close();
            }
        }
    }
}