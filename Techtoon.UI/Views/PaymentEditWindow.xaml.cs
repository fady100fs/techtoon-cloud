using System;
using System.Collections.ObjectModel;
using System.Windows;
using Npgsql;

namespace Techtoon.UI.Views
{
    public partial class PaymentEditWindow : Window
    {
        private readonly string _connectionString =
            "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;" +
            "Username=neondb_owner;Password=npg_K9nHVkY5OjRC;Database=neondb;" +
            "SSL Mode=Require;Trust Server Certificate=true";

        private int _paymentId;
        private bool _isEditMode;
        private bool _isReceipt = true;

        public bool Saved { get; private set; }
        public int SavedPaymentId { get; private set; }

        public ObservableCollection<PartyItem> Parties { get; } = new ObservableCollection<PartyItem>();
        public ObservableCollection<InvoiceItem> Invoices { get; } = new ObservableCollection<InvoiceItem>();
        public ObservableCollection<CashBoxItem> CashBoxes { get; } = new ObservableCollection<CashBoxItem>();
        public ObservableCollection<string> Methods { get; } = new ObservableCollection<string>();
        public ObservableCollection<AccountItem> Accounts { get; } = new ObservableCollection<AccountItem>();

        public PaymentEditWindow()
        {
            InitializeComponent();
            Parties = new ObservableCollection<PartyItem>();
            Invoices = new ObservableCollection<InvoiceItem>();
            CashBoxes = new ObservableCollection<CashBoxItem>();
            Methods = new ObservableCollection<string>();
            Accounts = new ObservableCollection<AccountItem>();
            
            Methods.Add("نقدي");
            Methods.Add("بطاقة");
            Methods.Add("تحويل");
            
            DataContext = this;

            if (cmbMethod != null) cmbMethod.ItemsSource = Methods;
            if (cmbMethod != null) cmbMethod.SelectedIndex = 0;
            if (dpDate != null) dpDate.SelectedDate = DateTime.Today;
            
            LoadLookups();
        }

        private void LoadLookups()
        {
            LoadParties();
            LoadAccounts();
            LoadCashBoxes();
        }

        private void LoadParties()
        {
            Parties.Clear();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            string sql = "SELECT id, name, type FROM parties ";
            bool hasIsActive = false;
            using (var chk = new NpgsqlCommand("SELECT column_name FROM information_schema.columns WHERE table_name='parties' AND column_name='is_active'", conn))
            using (var rdr = chk.ExecuteReader()) hasIsActive = rdr.Read();
            if (hasIsActive) sql += "WHERE is_active = TRUE ";
            sql += "ORDER BY name";

            using var cmd = new NpgsqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Parties.Add(new PartyItem { Id = reader.GetInt32(0), Name = reader.GetString(1), Type = reader.IsDBNull(2) ? "" : reader.GetString(2) });
            }
            if (cmbParty != null) cmbParty.ItemsSource = Parties;
        }

        private void LoadAccounts()
        {
            Accounts.Clear();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            
            // فحص وجود عمود is_active
            bool hasIsActive = false;
            using (var chk = new NpgsqlCommand("SELECT column_name FROM information_schema.columns WHERE table_name='accounts' AND column_name='is_active'", conn))
            using (var rdr = chk.ExecuteReader()) hasIsActive = rdr.Read();
            
            string sql = "SELECT id, name FROM accounts ";
            if (hasIsActive) sql += "WHERE is_active = TRUE ";
            sql += "ORDER BY name";
            
            using var cmd = new NpgsqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Accounts.Add(new AccountItem { Id = reader.GetInt32(0), Name = reader.GetString(1) });
            }
            if (cmbAccount != null) cmbAccount.ItemsSource = Accounts;
        }

        private void LoadCashBoxes()
        {
            CashBoxes.Clear();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            string sql = "SELECT id, name FROM cash_boxes ";
            bool hasIsActive = false;
            using (var chk = new NpgsqlCommand("SELECT column_name FROM information_schema.columns WHERE table_name='cash_boxes' AND column_name='is_active'", conn))
            using (var rdr = chk.ExecuteReader()) hasIsActive = rdr.Read();
            if (hasIsActive) sql += "WHERE is_active = TRUE ";
            sql += "ORDER BY name";

            using var cmd = new NpgsqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                CashBoxes.Add(new CashBoxItem { Id = reader.GetInt32(0), Name = reader.GetString(1) });
            }
            if (cmbCashBox != null)
            {
                cmbCashBox.ItemsSource = CashBoxes;
                if (cmbCashBox.Items.Count > 0) cmbCashBox.SelectedIndex = 0;
            }
        }

        public void SetMode(bool isReceipt)
        {
            _isReceipt = isReceipt;
            Title = isReceipt ? "سند قبض" : "سند صرف";
            if (lblParty != null) lblParty.Text = isReceipt ? "العميل" : "المورد";
        }

        private void CmbParty_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cmbParty == null || cmbParty.SelectedValue == null) return;
            if (cmbAccount != null && cmbAccount.SelectedIndex != -1) cmbAccount.SelectedIndex = -1; // Clear account
            LoadInvoices((int)cmbParty.SelectedValue);
        }

        private void CmbAccount_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cmbAccount == null || cmbAccount.SelectedValue == null) return;
            if (cmbParty != null && cmbParty.SelectedIndex != -1) cmbParty.SelectedIndex = -1; // Clear party
            if (cmbInvoice != null)
            {
                Invoices.Clear();
                Invoices.Add(new InvoiceItem { Id = 0, Number = "بدون فاتورة" });
                if (cmbInvoice.Items.Count > 0) cmbInvoice.SelectedIndex = 0;
                cmbInvoice.IsEnabled = false;
            }
        }

        private void LoadInvoices(int partyId)
        {
            Invoices.Clear();
            Invoices.Add(new InvoiceItem { Id = 0, Number = "بدون فاتورة" });
            if (cmbInvoice != null) cmbInvoice.IsEnabled = true;

            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(
                "SELECT id, invoice_number, total, paid_amount FROM invoices WHERE party_id = @pid AND status <> 'cancelled' ORDER BY date DESC", conn)
            {
                Parameters = { new NpgsqlParameter("@pid", partyId) }
            };
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                decimal total = reader.IsDBNull(2) ? 0m : reader.GetDecimal(2);
                decimal paid = reader.IsDBNull(3) ? 0m : reader.GetDecimal(3);
                Invoices.Add(new InvoiceItem { Id = reader.GetInt32(0), Number = reader.GetString(1) + " (" + (total - paid).ToString("0.00") + ")", Total = total, Paid = paid });
            }
            if (cmbInvoice != null)
            {
                cmbInvoice.ItemsSource = Invoices;
                if (cmbInvoice.Items.Count > 0) cmbInvoice.SelectedIndex = 0;
            }
        }

        private bool ValidateForm()
        {
            int? pid = cmbParty?.SelectedValue as int?;
            int? aid = cmbAccount?.SelectedValue as int?;

            if (!pid.HasValue && !aid.HasValue)
            {
                MessageBox.Show("اختر الطرف أو الحساب");
                return false;
            }
            if (!decimal.TryParse(txtAmount.Text, out decimal amount) || amount <= 0)
            {
                MessageBox.Show("المبلغ غير صحيح");
                return false;
            }
            return true;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm()) return;

            int partyId = (cmbParty.SelectedValue as int?) ?? 0;
            int accountId = (cmbAccount.SelectedValue as int?) ?? 0;
            int invoiceId = (cmbInvoice.SelectedValue as int?) ?? 0;
            decimal amount = decimal.Parse(txtAmount.Text);
            DateTime date = dpDate.SelectedDate ?? DateTime.Today;
            int cashBoxId = (cmbCashBox.SelectedValue as int?) ?? 0;
            string method = cmbMethod.SelectedItem?.ToString() ?? "نقدي";
            string reference = txtRef.Text.Trim();
            string notes = txtNotes.Text.Trim();
            string pType = _isReceipt ? "receipt" : "payment";

            NpgsqlConnection conn = null;
            try
            {
                conn = new NpgsqlConnection(_connectionString);
                conn.Open();
                using var tx = conn.BeginTransaction();

                string sql = _isEditMode ?
                    "UPDATE payments SET party_id=@pid, account_id=@aid, invoice_id=@iid, amount=@amt, date=@dt, cash_box_id=@cb, payment_method=@m, reference_number=@ref, notes=@n, payment_type=@t WHERE id=@id" :
                    "INSERT INTO payments (party_id, account_id, invoice_id, amount, date, cash_box_id, payment_method, reference_number, notes, payment_type, currency_id) VALUES (@pid, @aid, @iid, @amt, @dt, @cb, @m, @ref, @n, @t, 1) RETURNING id";

                using var cmd = new NpgsqlCommand(sql, conn, tx);
                cmd.Parameters.AddWithValue("@pid", partyId > 0 ? (object)partyId : DBNull.Value);
                cmd.Parameters.AddWithValue("@aid", accountId > 0 ? (object)accountId : DBNull.Value);
                cmd.Parameters.AddWithValue("@iid", invoiceId > 0 ? (object)invoiceId : DBNull.Value);
                cmd.Parameters.AddWithValue("@amt", (double)amount);
                cmd.Parameters.AddWithValue("@dt", date);
                cmd.Parameters.AddWithValue("@cb", cashBoxId > 0 ? (object)cashBoxId : DBNull.Value);
                cmd.Parameters.AddWithValue("@m", method);
                cmd.Parameters.AddWithValue("@ref", string.IsNullOrEmpty(reference) ? (object)DBNull.Value : reference);
                cmd.Parameters.AddWithValue("@n", string.IsNullOrEmpty(notes) ? (object)DBNull.Value : notes);
                cmd.Parameters.AddWithValue("@t", pType);

                if (_isEditMode)
                {
                    cmd.Parameters.AddWithValue("@id", _paymentId);
                    cmd.ExecuteNonQuery();
                    SavedPaymentId = _paymentId;
                }
                else
                {
                    SavedPaymentId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                tx.Commit();
                Saved = true;
                MessageBox.Show("تم الحفظ بنجاح");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ: " + ex.Message);
            }
            finally
            {
                if (conn != null) conn.Close();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => Close();
    }

    public class PartyItem { public int Id { get; set; } public string Name { get; set; } public string Type { get; set; } }
    public class InvoiceItem { public int Id { get; set; } public string Number { get; set; } public decimal Total { get; set; } public decimal Paid { get; set; } }
    public class CashBoxItem { public int Id { get; set; } public string Name { get; set; } }
    public class AccountItem { public int Id { get; set; } public string Name { get; set; } }
}