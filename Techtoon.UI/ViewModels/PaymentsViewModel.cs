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
            Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();

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
                    _ = LoadPaymentsAsync();
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
            RefreshCommand = new UiRelayCommand(_ => _ = LoadPaymentsAsync(), null);
            _ = LoadPaymentsAsync();
        }

        public async System.Threading.Tasks.Task LoadPaymentsAsync()
        {
            Payments = new ObservableCollection<PaymentRow>();
            NpgsqlConnection conn = null;
            try
            {
                conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
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
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            Payments.Add(new PaymentRow
                            {
                                Id = reader.GetInt32(0),
                                PaymentDate = reader.GetDateTime(1),
                                PartyName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                AccountName = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                VoucherType = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                Amount = reader.IsDBNull(5) ? 0m : Convert.ToDecimal(reader.GetValue(5)),
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
            if (win.ShowDialog() == true) _ = LoadPaymentsAsync();
        }

        private void AddPayment()
        {
            var win = new PaymentEditWindow();
            win.SetMode(false);
            win.Owner = Application.Current.MainWindow;
            if (win.ShowDialog() == true) _ = LoadPaymentsAsync();
        }

        private void Edit()
        {
            if (SelectedPayment == null) return;
            var win = new PaymentEditWindow();
            win.Owner = Application.Current.MainWindow;
            if (win.ShowDialog() == true) _ = LoadPaymentsAsync();
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
                // H3: deleting a receipt/payment must repair invoices.paid_amount for the
                // linked invoice; otherwise customer/supplier balances silently drift.
                using (var tx = conn.BeginTransaction())
                {
                    int? invoiceId = null;
                    decimal amount = 0m;
                    bool hasInvoiceId = ColumnExists(conn, tx, "payments", "invoice_id");
                    if (hasInvoiceId)
                    {
                        using var read = new NpgsqlCommand("SELECT invoice_id, COALESCE(amount,0) FROM payments WHERE id=@id", conn, tx);
                        read.Parameters.AddWithValue("@id", SelectedPayment.Id);
                        using var r = read.ExecuteReader();
                        if (r.Read())
                        {
                            invoiceId = r.IsDBNull(0) ? (int?)null : r.GetInt32(0);
                            amount = r.IsDBNull(1) ? 0m : Convert.ToDecimal(r.GetValue(1));
                        }
                    }

                    using (var cmd = new NpgsqlCommand("DELETE FROM payments WHERE id=@id", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@id", SelectedPayment.Id);
                        if (cmd.ExecuteNonQuery() == 0) { tx.Rollback(); return; }
                    }

                    if (invoiceId.HasValue)
                        ReconcileInvoicePaid(conn, tx, invoiceId.Value, amount);

                    tx.Commit();
                }
                _ = LoadPaymentsAsync();
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

        private static bool ColumnExists(NpgsqlConnection conn, NpgsqlTransaction tx, string table, string column)
        {
            using var cmd = new NpgsqlCommand(
                "SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name=@t AND column_name=@c)", conn, tx);
            cmd.Parameters.AddWithValue("@t", table);
            cmd.Parameters.AddWithValue("@c", column);
            return Convert.ToBoolean(cmd.ExecuteScalar());
        }

        private static bool TableExists(NpgsqlConnection conn, NpgsqlTransaction tx, string table)
        {
            using var cmd = new NpgsqlCommand(
                "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema='public' AND table_name=@t)", conn, tx);
            cmd.Parameters.AddWithValue("@t", table);
            return Convert.ToBoolean(cmd.ExecuteScalar());
        }

        /// <summary>
        /// Recomputes invoices.paid_amount from the ledger that owns this invoice.
        /// The WPF payment window writes payments.invoice_id; the invoice editor writes
        /// invoice_payments. Prefer invoice_payments when it has rows, otherwise use payments.
        /// </summary>
        private static void ReconcileInvoicePaid(NpgsqlConnection conn, NpgsqlTransaction tx, int invoiceId, decimal deletedAmount)
        {
            decimal paid;
            if (TableExists(conn, tx, "invoice_payments"))
            {
                using (var check = new NpgsqlCommand("SELECT COUNT(*) FROM invoice_payments WHERE invoice_id=@id", conn, tx))
                {
                    check.Parameters.AddWithValue("@id", invoiceId);
                    if (Convert.ToInt64(check.ExecuteScalar()) > 0)
                    {
                        using var sum = new NpgsqlCommand("SELECT COALESCE(SUM(amount),0) FROM invoice_payments WHERE invoice_id=@id", conn, tx);
                        sum.Parameters.AddWithValue("@id", invoiceId);
                        paid = Convert.ToDecimal(sum.ExecuteScalar());
                        UpdateInvoicePaid(conn, tx, invoiceId, paid);
                        return;
                    }
                }
            }

            if (ColumnExists(conn, tx, "payments", "invoice_id"))
            {
                using var sum = new NpgsqlCommand("SELECT COALESCE(SUM(amount),0) FROM payments WHERE invoice_id=@id", conn, tx);
                sum.Parameters.AddWithValue("@id", invoiceId);
                paid = Convert.ToDecimal(sum.ExecuteScalar());
            }
            else
            {
                using var cur = new NpgsqlCommand("SELECT GREATEST(COALESCE(paid_amount,0) - @amt, 0) FROM invoices WHERE id=@id", conn, tx);
                cur.Parameters.AddWithValue("@amt", deletedAmount);
                cur.Parameters.AddWithValue("@id", invoiceId);
                var v = cur.ExecuteScalar();
                if (v == null || v == DBNull.Value) return;
                paid = Convert.ToDecimal(v);
            }

            UpdateInvoicePaid(conn, tx, invoiceId, paid);
        }

        private static void UpdateInvoicePaid(NpgsqlConnection conn, NpgsqlTransaction tx, int invoiceId, decimal paid)
        {
            using var cmd = new NpgsqlCommand(
                "UPDATE invoices SET paid_amount=@paid, " +
                "status=CASE WHEN @paid >= COALESCE(total,0) THEN 'paid' WHEN @paid > 0 THEN 'partial' ELSE 'unpaid' END, " +
                "updated_at=NOW() WHERE id=@id AND COALESCE(status,'') <> 'cancelled'", conn, tx);
            cmd.Parameters.AddWithValue("@paid", paid);
            cmd.Parameters.AddWithValue("@id", invoiceId);
            cmd.ExecuteNonQuery();
        }
    }
}