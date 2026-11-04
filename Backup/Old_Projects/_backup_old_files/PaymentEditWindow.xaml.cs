using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Npgsql;

namespace Techtoon.UI.Views
{
    public class InvoiceOption
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; }
        public decimal Total { get; set; }
        public decimal Paid { get; set; }
        public decimal Remaining { get { return Total - Paid; } }
        public string Display
        {
            get { return InvoiceNumber + " - Rem: " + Remaining.ToString("N2"); }
        }
    }

    public class CashBoxOption
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public override string ToString() { return Name ?? ""; }
    }

    public class AccountOption
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public override string ToString() { return Name ?? ""; }
    }

    public partial class PaymentEditWindow : Window
    {
        private readonly string _paymentType;
        private readonly int? _editId;
        private List<PartyOption> _parties = new List<PartyOption>();
        private List<InvoiceOption> _invoices = new List<InvoiceOption>();
        private List<CashBoxOption> _cashBoxes = new List<CashBoxOption>();
        private List<AccountOption> _accounts = new List<AccountOption>();

        public static string GetCS()
        {
            return "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=npg_K9nHVkY5OjRC;Database=neondb;SSL Mode=Require;Trust Server Certificate=true";
        }

        public PaymentEditWindow(string paymentType, int? editId = null)
        {
            InitializeComponent();
            _paymentType = paymentType;
            _editId = editId;
            SetArabicTexts();
        }

        private void SetArabicTexts()
        {
            try
            {
                if (_paymentType == "receipt")
                {
                    Title = "\u0633\u0646\u062F \u0642\u0628\u0636";
                    LblTitle.Text = "\u0633\u0646\u062F \u0642\u0628\u0636 \u062C\u062F\u064A\u062F";
                }
                else
                {
                    Title = "\u0633\u0646\u062F \u0635\u0631\u0641";
                    LblTitle.Text = "\u0633\u0646\u062F \u0635\u0631\u0641 \u062C\u062F\u064A\u062F";
                }
                LblVoucher.Text = "\u0631\u0642\u0645 \u0627\u0644\u0633\u0646\u062F:";
                LblDate.Text = "\u0627\u0644\u062A\u0627\u0631\u064A\u062E:";
                LblParty.Text = "\u0627\u0644\u0637\u0631\u0641:";
                LblInvoice.Text = "\u0641\u0627\u062A\u0648\u0631\u0629 \u0645\u0631\u062A\u0628\u0637\u0629:";
                LblAmount.Text = "\u0627\u0644\u0645\u0628\u0644\u063A:";
                LblMethod.Text = "\u0637\u0631\u064A\u0642\u0629 \u0627\u0644\u062F\u0641\u0639:";
                LblCashBox.Text = "\u0627\u0644\u062E\u0632\u064A\u0646\u0629:";
                LblAccount.Text = "\u0627\u0644\u062D\u0633\u0627\u0628:";
                LblNotes.Text = "\u0645\u0644\u0627\u062D\u0638\u0627\u062A:";
                BtnSave.Content = "\uD83D\uDCBE \u062D\u0641\u0638";
                BtnCancel.Content = "\u2716 \u0625\u0644\u063A\u0627\u0621";

                CmbMethod.Items.Clear();
                CmbMethod.Items.Add(new ComboBoxItem { Content = "\u0646\u0642\u062F\u064A", Tag = "cash" });
                CmbMethod.Items.Add(new ComboBoxItem { Content = "\u0628\u0637\u0627\u0642\u0629", Tag = "card" });
                CmbMethod.Items.Add(new ComboBoxItem { Content = "\u062A\u062D\u0648\u064A\u0644 \u0628\u0646\u0643\u064A", Tag = "bank" });
                CmbMethod.Items.Add(new ComboBoxItem { Content = "\u0634\u064A\u0643", Tag = "check" });
                CmbMethod.SelectedIndex = 0;
            }
            catch { }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DpDate.SelectedDate = DateTime.Today;
            TxtVoucher.Text = GenerateVoucherNumber();
            LoadParties();
            LoadCashBoxes();
            LoadAccounts();
            await System.Threading.Tasks.Task.CompletedTask;
            if (_editId.HasValue) LoadForEdit();
        }

        
        private void LoadForEdit()
        {
            try
            {
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    string sql = @"SELECT voucher_no, date, party_id, type, amount, method, cash_box_id, account_id, notes
                        FROM payments WHERE id = @id";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("id", _editId.Value);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                TxtVoucher.Text = r.IsDBNull(0) ? "" : r.GetString(0);
                                DpDate.SelectedDate = r.IsDBNull(1) ? DateTime.Today : r.GetDateTime(1);
                                int partyId = r.IsDBNull(2) ? 0 : r.GetInt32(2);
                                // حدد الطرف
                                foreach (var p in _parties) { if (p.Id == partyId) { CmbParty.SelectedItem = p; break; } }
                                TxtAmount.Text = (r.IsDBNull(4) ? 0 : (decimal)r.GetDouble(4)).ToString("N2");
                                string method = r.IsDBNull(5) ? "cash" : r.GetString(5);
                                foreach (ComboBoxItem mi in CmbMethod.Items) { if (mi.Tag?.ToString() == method) { CmbMethod.SelectedItem = mi; break; } }
                                TxtNotes.Text = r.IsDBNull(8) ? "" : r.GetString(8);
                            }
                        }
                    }
                }
            }
            catch { }
        }
        private string GenerateVoucherNumber()
        {
            try
            {
                string prefix = _paymentType == "receipt" ? "RCV" : "PAY";
                int year = DateTime.Now.Year;
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    string sql = "SELECT voucher_no FROM payments WHERE voucher_no LIKE @p ORDER BY voucher_no DESC LIMIT 1";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("p", prefix + "-" + year + "-%");
                        var o = cmd.ExecuteScalar();
                        if (o != null && o != DBNull.Value)
                        {
                            string last = o.ToString();
                            int dashIdx = last.LastIndexOf('-');
                            if (dashIdx >= 0 && int.TryParse(last.Substring(dashIdx + 1), out int seq))
                                return prefix + "-" + year + "-" + (seq + 1).ToString("D4");
                        }
                    }
                }
            }
            catch { }
            string pf = _paymentType == "receipt" ? "RCV" : "PAY";
            return pf + "-" + DateTime.Now.Year + "-0001";
        }

        private void LoadParties()
        {
            try
            {
                _parties.Clear();
                CmbParty.Items.Clear();
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT id, name FROM parties ORDER BY name LIMIT 500", conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            var p = new PartyOption { Id = r.GetInt32(0), Name = r.GetString(1) };
                            _parties.Add(p);
                            CmbParty.Items.Add(p);
                        }
                    }
                }
            }
            catch { }
        }

        private void LoadCashBoxes()
        {
            try
            {
                _cashBoxes.Clear();
                CmbCashBox.Items.Clear();
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT id, name FROM cash_boxes ORDER BY name LIMIT 100", conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            var c = new CashBoxOption { Id = r.GetInt32(0), Name = r.GetString(1) };
                            _cashBoxes.Add(c);
                            CmbCashBox.Items.Add(c);
                        }
                    }
                }
                if (CmbCashBox.Items.Count > 0) CmbCashBox.SelectedIndex = 0;
            }
            catch { }
        }

        private void LoadAccounts()
        {
            try
            {
                _accounts.Clear();
                CmbAccount.Items.Clear();
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT id, name FROM accounts ORDER BY name LIMIT 500", conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            var a = new AccountOption { Id = r.GetInt32(0), Name = r.GetString(1) };
                            _accounts.Add(a);
                            CmbAccount.Items.Add(a);
                        }
                    }
                }
                if (CmbAccount.Items.Count > 0) CmbAccount.SelectedIndex = 0;
            }
            catch { }
        }

        private void CmbParty_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                CmbInvoice.Items.Clear();
                _invoices.Clear();
                if (CmbParty.SelectedItem is PartyOption party)
                {
                    string invoiceType = _paymentType == "receipt" ? "sales" : "purchase";
                    using (var conn = new NpgsqlConnection(GetCS()))
                    {
                        conn.Open();
                        string sql = @"SELECT id, invoice_number, total, paid_amount
                            FROM invoices
                            WHERE party_id = @pid AND type = @type AND paid_amount < total
                            ORDER BY id DESC LIMIT 50";
                        using (var cmd = new NpgsqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("pid", party.Id);
                            cmd.Parameters.AddWithValue("type", invoiceType);
                            using (var r = cmd.ExecuteReader())
                            {
                                while (r.Read())
                                {
                                    var inv = new InvoiceOption
                                    {
                                        Id = r.GetInt32(0),
                                        InvoiceNumber = r.IsDBNull(1) ? "" : r.GetString(1),
                                        Total = r.IsDBNull(2) ? 0 : (decimal)r.GetDouble(2),
                                        Paid = r.IsDBNull(3) ? 0 : (decimal)r.GetDouble(3)
                                    };
                                    _invoices.Add(inv);
                                    CmbInvoice.Items.Add(inv);
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void CmbInvoice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (CmbInvoice.SelectedItem is InvoiceOption inv)
                {
                    if (string.IsNullOrWhiteSpace(TxtAmount.Text) || TxtAmount.Text == "0")
                    {
                        TxtAmount.Text = inv.Remaining.ToString("N2");
                    }
                }
            }
            catch { }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!(CmbParty.SelectedItem is PartyOption party))
                {
                    LblStatus.Text = "\u0627\u062E\u062A\u0631 \u0627\u0644\u0637\u0631\u0641";
                    return;
                }
                if (!decimal.TryParse(TxtAmount.Text, out decimal amount) || amount <= 0)
                {
                    LblStatus.Text = "\u0627\u0644\u0645\u0628\u0644\u063A \u063A\u064A\u0631 \u0635\u062D\u064A\u062D";
                    return;
                }

                string method = "cash";
                if (CmbMethod.SelectedItem is ComboBoxItem mi && mi.Tag != null) method = mi.Tag.ToString();

                int? cashBoxId = null;
                if (CmbCashBox.SelectedItem is CashBoxOption cb) cashBoxId = cb.Id;

                int? accountId = null;
                if (CmbAccount.SelectedItem is AccountOption ac) accountId = ac.Id;

                int? linkedInvoiceId = null;
                if (CmbInvoice.SelectedItem is InvoiceOption inv) linkedInvoiceId = inv.Id;

                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    string sql = @"INSERT INTO payments (voucher_no, date, party_id, type, amount, method, cash_box_id, account_id, notes)
                        VALUES (@vno, @dt, @pid, @tp, @amt, @mth, @cbid, @acid, @nt)";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("vno", TxtVoucher.Text);
                        cmd.Parameters.AddWithValue("dt", DpDate.SelectedDate ?? DateTime.Today);
                        cmd.Parameters.AddWithValue("pid", party.Id);
                        cmd.Parameters.AddWithValue("tp", _paymentType);
                        cmd.Parameters.AddWithValue("amt", amount);
                        cmd.Parameters.AddWithValue("mth", method);
                        cmd.Parameters.AddWithValue("cbid", (object)cashBoxId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("acid", (object)accountId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("nt", (object)TxtNotes.Text ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }

                    if (linkedInvoiceId.HasValue)
                    {
                        string updSql = "UPDATE invoices SET paid_amount = paid_amount + @amt WHERE id = @pid";
                        using (var upCmd = new NpgsqlCommand(updSql, conn))
                        {
                            upCmd.Parameters.AddWithValue("amt", amount);
                            upCmd.Parameters.AddWithValue("pid", linkedInvoiceId.Value);
                            upCmd.ExecuteNonQuery();
                        }
                    }
                }

                LblStatus.Text = "\u062A\u0645 \u0627\u0644\u062D\u0641\u0638 \u0628\u0646\u062C\u0627\u062D";
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                LblStatus.Text = "\u062E\u0637\u0623: " + ex.Message;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}