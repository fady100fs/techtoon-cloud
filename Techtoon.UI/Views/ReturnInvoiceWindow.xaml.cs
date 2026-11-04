using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Npgsql;

namespace Techtoon.UI.Views
{
    public class ReturnInvoiceOption
    {
        public int Id { get; set; }
        public string Display { get; set; }
    }

    public class ReturnLineItem : INotifyPropertyChanged
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public double OriginalQty { get; set; }
        public double AlreadyReturned { get; set; }
        public double Price { get; set; }

        public double AvailableToReturn { get { return OriginalQty - AlreadyReturned; } }

        private double _returnQty = 0;
        public double ReturnQty
        {
            get { return _returnQty; }
            set
            {
                // منع تجاوز الكمية المتاحة
                if (value < 0) value = 0;
                if (value > AvailableToReturn) value = AvailableToReturn;
                _returnQty = value;
                OnPropertyChanged("ReturnQty");
                OnPropertyChanged("ReturnTotal");
            }
        }

        public double ReturnTotal { get { return ReturnQty * Price; } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public partial class ReturnInvoiceWindow : Window
    {
        private readonly string _connectionString =
            "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;" +
            "Username=neondb_owner;Password=npg_K9nHVkY5OjRC;Database=neondb;" +
            "SSL Mode=Require;Trust Server Certificate=true";

        public ObservableCollection<ReturnInvoiceOption> Invoices { get; } = new ObservableCollection<ReturnInvoiceOption>();
        public ObservableCollection<ReturnLineItem> Lines { get; } = new ObservableCollection<ReturnLineItem>();

        public bool Saved { get; private set; }

        public ReturnInvoiceWindow()
        {
            InitializeComponent();
            DataContext = this;
            dpDate.SelectedDate = DateTime.Today;
            dgItems.ItemsSource = Lines;
            LoadInvoices();
        }

        private void LoadInvoices()
        {
            Invoices.Clear();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(
                "SELECT id, invoice_number, date, type FROM invoices " +
                "WHERE status <> 'cancelled' AND type IN ('sales','purchase') " +
                "ORDER BY date DESC LIMIT 100", conn);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                Invoices.Add(new ReturnInvoiceOption
                {
                    Id = r.GetInt32(0),
                    Display = r.GetString(1) + " (" + r.GetDateTime(2).ToString("yyyy-MM-dd") + ") - " + r.GetString(3)
                });
            }
            cmbInvoice.ItemsSource = Invoices;
        }

        private void CmbInvoice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbInvoice.SelectedValue == null) return;
            int invoiceId = (int)cmbInvoice.SelectedValue;
            LoadInvoiceDetails(invoiceId);
        }

        private void LoadInvoiceDetails(int invoiceId)
        {
            Lines.Clear();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            // Original invoice summary
            int partyId = 0;
            using (var cmd = new NpgsqlCommand(
                "SELECT i.party_id, p.name, i.total, i.paid_amount, i.type FROM invoices i " +
                "LEFT JOIN parties p ON i.party_id = p.id WHERE i.id = @id", conn))
            {
                cmd.Parameters.AddWithValue("id", invoiceId);
                using var r = cmd.ExecuteReader();
                if (r.Read())
                {
                    partyId = r.IsDBNull(0) ? 0 : r.GetInt32(0);
                    lblParty.Text = r.IsDBNull(1) ? "-" : r.GetString(1);
                    lblTotal.Text = (r.IsDBNull(2) ? 0 : r.GetDouble(2)).ToString("0.00");
                    lblPaid.Text = (r.IsDBNull(3) ? 0 : r.GetDouble(3)).ToString("0.00");
                    string origType = r.IsDBNull(4) ? "sales" : r.GetString(4);
                    cmbType.SelectedIndex = origType == "purchase" ? 1 : 0;
                }
            }

            // Load original invoice lines
            using (var cmd = new NpgsqlCommand(
                "SELECT il.item_id, i.name, il.quantity, il.price " +
                "FROM invoice_lines il JOIN items i ON il.item_id = i.id " +
                "WHERE il.invoice_id = @id", conn))
            {
                cmd.Parameters.AddWithValue("id", invoiceId);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    Lines.Add(new ReturnLineItem
                    {
                        ItemId = r.GetInt32(0),
                        ItemName = r.GetString(1),
                        OriginalQty = r.GetDouble(2),
                        Price = r.GetDouble(3),
                        ReturnQty = 0
                    });
                }
            }

            // Load already-returned quantities for each item
            // (نبحث عن فواتير المرتجع المرتبطة بهذه الفاتورة عبر reference_invoice_id)
            double totalPrevReturn = 0;
            foreach (var line in Lines)
            {
                using var cmd = new NpgsqlCommand(
                    "SELECT COALESCE(SUM(il.quantity), 0) " +
                    "FROM invoice_lines il " +
                    "JOIN invoices inv ON il.invoice_id = inv.id " +
                    "WHERE inv.reference_invoice_id = @ref AND il.item_id = @item " +
                    "AND inv.type IN ('sales_return','purchase_return')", conn);
                cmd.Parameters.AddWithValue("ref", invoiceId);
                cmd.Parameters.AddWithValue("item", line.ItemId);
                var prev = Convert.ToDouble(cmd.ExecuteScalar());
                line.AlreadyReturned = prev;
                totalPrevReturn += prev * line.Price;
            }

            lblPrevReturn.Text = totalPrevReturn.ToString("0.00");
            UpdateReturnTotal();
        }

        private void DgItems_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // Update total when user edits ReturnQty
            Dispatcher.BeginInvoke(new Action(UpdateReturnTotal));
        }

        private void UpdateReturnTotal()
        {
            double total = Lines.Sum(l => l.ReturnTotal);
            lblReturnTotal.Text = total.ToString("0.00");
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (cmbInvoice.SelectedValue == null)
            {
                MessageBox.Show("اختر الفاتورة الأصلية");
                return;
            }

            var returnLines = Lines.Where(l => l.ReturnQty > 0).ToList();
            if (returnLines.Count == 0)
            {
                MessageBox.Show("حدد كمية مرتجعة لصنف واحد على الأقل\n\nملاحظة: يمكنك إرجاع أصناف محددة فقط بتحديد كمية لها وترك الباقي = 0");
                return;
            }

            // تحقق نهائي من الكميات المتاحة
            foreach (var l in returnLines)
            {
                if (l.ReturnQty > l.AvailableToReturn)
                {
                    MessageBox.Show($"الكمية المطلوب إرجاعها للصنف '{l.ItemName}' ({l.ReturnQty}) تتجاوز المتاح ({l.AvailableToReturn})\nالكمية الأصلية: {l.OriginalQty} - المرتجع سابقاً: {l.AlreadyReturned}");
                    return;
                }
            }

            int originalInvoiceId = (int)cmbInvoice.SelectedValue;
            DateTime date = dpDate.SelectedDate ?? DateTime.Today;
            string returnType = cmbType.SelectedIndex == 0 ? "sales_return" : "purchase_return";

            try
            {
                using var conn = new NpgsqlConnection(_connectionString);
                conn.Open();
                using var tx = conn.BeginTransaction();

                // 1) Create return invoice
                int newInvoiceId;
                using (var cmd = new NpgsqlCommand(
                    "INSERT INTO invoices (invoice_number, date, party_id, type, total, paid_amount, " +
                    "status, currency_id, reference_invoice_id, created_at) " +
                    "VALUES (@num, @date, @pid, @type, @total, 0, 'completed', 1, @ref, @now) RETURNING id", conn, tx))
                {
                    string retNum = "RET-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                    int partyId = 0;
                    using (var c2 = new NpgsqlCommand("SELECT party_id FROM invoices WHERE id = @id", conn, tx))
                    {
                        c2.Parameters.AddWithValue("id", originalInvoiceId);
                        var res = c2.ExecuteScalar();
                        if (res != null && res != DBNull.Value) partyId = Convert.ToInt32(res);
                    }
                    double total = (double)returnLines.Sum(l => l.ReturnTotal);

                    cmd.Parameters.AddWithValue("num", retNum);
                    cmd.Parameters.AddWithValue("date", date);
                    cmd.Parameters.AddWithValue("pid", partyId > 0 ? (object)partyId : DBNull.Value);
                    cmd.Parameters.AddWithValue("type", returnType);
                    cmd.Parameters.AddWithValue("total", total);
                    cmd.Parameters.AddWithValue("ref", originalInvoiceId);
                    cmd.Parameters.AddWithValue("now", DateTime.Now);
                    newInvoiceId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // 2) Insert return lines
                foreach (var l in returnLines)
                {
                    using var cmd = new NpgsqlCommand(
                        "INSERT INTO invoice_lines (invoice_id, item_id, quantity, price, total, discount) " +
                        "VALUES (@inv, @item, @qty, @price, @total, 0)", conn, tx);
                    cmd.Parameters.AddWithValue("inv", newInvoiceId);
                    cmd.Parameters.AddWithValue("item", l.ItemId);
                    cmd.Parameters.AddWithValue("qty", l.ReturnQty);
                    cmd.Parameters.AddWithValue("price", l.Price);
                    cmd.Parameters.AddWithValue("total", l.ReturnTotal);
                    cmd.ExecuteNonQuery();
                }

                // 3) Update inventory
                foreach (var l in returnLines)
                {
                    string movType = returnType == "sales_return" ? "in" : "out";
                    using var cmd = new NpgsqlCommand(
                        "INSERT INTO inventory_movements (item_id, quantity, type, reference_id, reference_type, created_at) " +
                        "VALUES (@item, @qty, @type, @ref, 'return', @now)", conn, tx);
                    cmd.Parameters.AddWithValue("item", l.ItemId);
                    cmd.Parameters.AddWithValue("qty", l.ReturnQty);
                    cmd.Parameters.AddWithValue("type", movType);
                    cmd.Parameters.AddWithValue("ref", newInvoiceId);
                    cmd.Parameters.AddWithValue("now", DateTime.Now);
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
                Saved = true;
                double totalSaved = returnLines.Sum(l => l.ReturnTotal);
                MessageBox.Show($"تم حفظ المرتجع بنجاح\n\nرقم المرتجع: RET-{newInvoiceId}\nعدد الأصناف المرتجعة: {returnLines.Count}\nإجمالي قيمة المرتجع: {totalSaved:0.00}\n\nملاحظة: تم تحديث المخزون تلقائياً.");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ الحفظ: " + ex.Message);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}