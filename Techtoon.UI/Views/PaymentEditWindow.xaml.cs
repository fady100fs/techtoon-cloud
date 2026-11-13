using System;
using System.Collections.ObjectModel;
using System.Windows;
using Npgsql;
using Techtoon.UI.Services;

namespace Techtoon.UI.Views
{
    public partial class PaymentEditWindow : Window
    {
        private readonly string _connectionString =
            Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();

        private int _paymentId;
        private bool _isEditMode;
        private bool _isReceipt = true;
        // قيم السند قبل التعديل — لازم لتعديل بند الدفتر الموحّد بدل إضافته من جديد
        private decimal _originalAmount;
        private int _originalInvoiceId;

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

        /// <summary>
        /// تحميل سند قائم للتعديل.
        /// <para>بدون هذه الدالة كان زر «تعديل» يُنشئ سندًا جديدًا (تكرار) بدل تعديل السند.</para>
        /// </summary>
        public void LoadForEdit(int paymentId)
        {
            _isEditMode = true;
            _paymentId = paymentId;

            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(
                "SELECT id, party_id, account_id, invoice_id, amount, date, cash_box_id, " +
                "payment_method, reference_number, notes, payment_type FROM payments WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@id", paymentId);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return;

            int partyId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
            int accountId = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
            int invoiceId = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
            decimal amount = reader.IsDBNull(4) ? 0m : reader.GetDecimal(4);
            DateTime date = reader.IsDBNull(5) ? DateTime.Today : reader.GetDateTime(5);
            int cashBoxId = reader.IsDBNull(6) ? 0 : reader.GetInt32(6);
            string method = reader.IsDBNull(7) ? "نقدي" : reader.GetString(7);
            string reference = reader.IsDBNull(8) ? "" : reader.GetString(8);
            string notes = reader.IsDBNull(9) ? "" : reader.GetString(9);
            string pType = reader.IsDBNull(10) ? "" : reader.GetString(10);
            reader.Close();

            _originalAmount = amount;
            _originalInvoiceId = invoiceId;

            _isReceipt = pType != "payment";
            Title = _isReceipt ? "تعديل سند قبض" : "تعديل سند صرف";
            if (lblParty != null) lblParty.Text = _isReceipt ? "العميل" : "المورد";

            if (dpDate != null) dpDate.SelectedDate = date;
            if (txtAmount != null) txtAmount.Text = amount.ToString("0.##");
            if (txtRef != null) txtRef.Text = reference;
            if (txtNotes != null) txtNotes.Text = notes;

            SelectById(cmbParty, partyId);
            SelectById(cmbAccount, accountId);
            SelectById(cmbCashBox, cashBoxId);
            if (cmbMethod != null)
            {
                int idx = Methods.IndexOf(method);
                cmbMethod.SelectedIndex = idx >= 0 ? idx : 0;
            }

            // الفاتورة تُحمَّل بعد اختيار الطرف
            if (invoiceId > 0 && partyId > 0)
            {
                LoadInvoices(partyId);
                SelectById(cmbInvoice, invoiceId);
            }
        }

        /// <summary>يختار عنصر القائمة الذي مطابقه المعرّف.</summary>
        private static void SelectById(System.Windows.Controls.ComboBox combo, int id)
        {
            if (combo == null || id <= 0 || combo.ItemsSource == null) return;
            foreach (var item in combo.ItemsSource)
            {
                var prop = item.GetType().GetProperty("Id");
                if (prop == null) continue;
                var val = prop.GetValue(item);
                if (val is int i && i == id)
                {
                    combo.SelectedItem = item;
                    return;
                }
            }
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
                cmd.Parameters.AddWithValue("@amt", amount);
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

                // P1.2: ربط الدفعة بالفاتورة في الدفتر الموحّد + إعادة تسوية paid_amount
                // (كان BtnSave لا يكتب invoice_payments ولا يُحدّث paid_amount)
                if (_isEditMode && _originalInvoiceId > 0)
                {
                    // في وضع التعديل: بند الدفتر الموحّد موجود مسبقًا — نعدّله
                    // (إضافة بند جديد تُحتسب الدفعة مرتين في paid_amount)
                    using (var ipUpd = new NpgsqlCommand(
                        "UPDATE invoice_payments SET method=@wm, amount=@amt, reference=@ref, paid_at=@dt " +
                        "WHERE id = (SELECT id FROM invoice_payments WHERE invoice_id=@iid AND amount=@oamt ORDER BY id LIMIT 1)", conn, tx))
                    {
                        ipUpd.Parameters.AddWithValue("@iid", _originalInvoiceId);
                        ipUpd.Parameters.AddWithValue("@oamt", _originalAmount);
                        ipUpd.Parameters.AddWithValue("@amt", amount);
                        ipUpd.Parameters.AddWithValue("@dt", date);
                        ipUpd.Parameters.AddWithValue("@wm", method);
                        ipUpd.Parameters.AddWithValue("@ref", string.IsNullOrEmpty(reference) ? (object)DBNull.Value : reference);
                        ipUpd.ExecuteNonQuery();
                    }
                }
                else if (_isEditMode && _originalInvoiceId > 0 && invoiceId != _originalInvoiceId)
                {
                    // غُيّر ارتباط الفاتورة: احذف البند القديم
                    using (var ipDel = new NpgsqlCommand(
                        "DELETE FROM invoice_payments WHERE id = (SELECT id FROM invoice_payments WHERE invoice_id=@iid AND amount=@oamt ORDER BY id LIMIT 1)", conn, tx))
                    {
                        ipDel.Parameters.AddWithValue("@iid", _originalInvoiceId);
                        ipDel.Parameters.AddWithValue("@oamt", _originalAmount);
                        ipDel.ExecuteNonQuery();
                    }
                }

                if (invoiceId > 0)
                {
                    if (!_isEditMode)
                    {
                        // الدفتر الموحّد (إن فُرض) — أعمدة invoice_payments الفعلية:
                        // method / reference / paid_at (كانت date/payment_method/reference_number — أعمدة غير موجودة)
                        using (var ipCmd = new NpgsqlCommand(
                            "INSERT INTO invoice_payments (invoice_id, method, amount, reference, paid_at, created_at) " +
                            "VALUES (@iid, @wm, @amt, @ref, @dt, NOW())", conn, tx))
                        {
                            ipCmd.Parameters.AddWithValue("@iid", invoiceId);
                            ipCmd.Parameters.AddWithValue("@dt", date);
                            ipCmd.Parameters.AddWithValue("@amt", amount);
                            ipCmd.Parameters.AddWithValue("@wm", method);
                            ipCmd.Parameters.AddWithValue("@ref", string.IsNullOrEmpty(reference) ? (object)DBNull.Value : reference);
                            ipCmd.ExecuteNonQuery();
                        }
                    }

                    // إعادة حساب paid_amount من الدفتر الموحّد
                    using (var rec = new NpgsqlCommand(
                        "WITH s AS (SELECT COALESCE(SUM(amount),0) AS paid FROM invoice_payments WHERE invoice_id=@iid) " +
                        "UPDATE invoices SET paid_amount=s.paid, " +
                        "status=CASE WHEN s.paid >= COALESCE(total,0) THEN 'paid' WHEN s.paid > 0 THEN 'partial' ELSE 'unpaid' END, " +
                        "updated_at=NOW() FROM s WHERE invoices.id=@iid AND COALESCE(invoices.status,'') <> 'cancelled'", conn, tx))
                    {
                        rec.Parameters.AddWithValue("@iid", invoiceId);
                        rec.ExecuteNonQuery();
                    }
                }
                else if (_isEditMode && _originalInvoiceId > 0)
                {
                    // أُلغي ارتباط الفاتورة — احذف بند الدفتر وأعد تسوية الفاتورة الأصلية
                    using (var ipDel = new NpgsqlCommand(
                        "DELETE FROM invoice_payments WHERE id = (SELECT id FROM invoice_payments WHERE invoice_id=@iid AND amount=@oamt ORDER BY id LIMIT 1)", conn, tx))
                    {
                        ipDel.Parameters.AddWithValue("@iid", _originalInvoiceId);
                        ipDel.Parameters.AddWithValue("@oamt", _originalAmount);
                        ipDel.ExecuteNonQuery();
                    }
                    using (var rec = new NpgsqlCommand(
                        "WITH s AS (SELECT COALESCE(SUM(amount),0) AS paid FROM invoice_payments WHERE invoice_id=@iid) " +
                        "UPDATE invoices SET paid_amount=s.paid, " +
                        "status=CASE WHEN s.paid >= COALESCE(total,0) THEN 'paid' WHEN s.paid > 0 THEN 'partial' ELSE 'unpaid' END, " +
                        "updated_at=NOW() FROM s WHERE invoices.id=@iid", conn, tx))
                    {
                        rec.Parameters.AddWithValue("@iid", _originalInvoiceId);
                        rec.ExecuteNonQuery();
                    }
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