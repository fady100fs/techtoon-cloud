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
        public decimal OriginalQty { get; set; }
        public decimal AlreadyReturned { get; set; }
        public decimal Price { get; set; }

        public decimal AvailableToReturn { get { return OriginalQty - AlreadyReturned; } }

        private decimal _returnQty = 0m;
        public decimal ReturnQty
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

        public decimal ReturnTotal { get { return ReturnQty * Price; } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public partial class ReturnInvoiceWindow : Window
    {
        private readonly string _connectionString =
            Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();

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
                    lblTotal.Text = (r.IsDBNull(2) ? 0m : Convert.ToDecimal(r.GetValue(2))).ToString("0.00");
                    lblPaid.Text = (r.IsDBNull(3) ? 0m : Convert.ToDecimal(r.GetValue(3))).ToString("0.00");
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
                        OriginalQty = Convert.ToDecimal(r.GetValue(2)),
                        Price = Convert.ToDecimal(r.GetValue(3)),
                        ReturnQty = 0
                    });
                }
            }

            // Load already-returned quantities for each item
            // (نبحث عن فواتير المرتجع المرتبطة بهذه الفاتورة عبر reference_invoice_id)
            // M8: one grouped query instead of one query per line (N+1).
            decimal totalPrevReturn = 0m;
            var prevByItem = new System.Collections.Generic.Dictionary<int, decimal>();
            if (Lines.Count > 0)
            {
                var itemIds = Lines.Select(l => l.ItemId).Distinct().ToArray();
                using var cmd = new NpgsqlCommand(
                    "SELECT il.item_id, COALESCE(SUM(il.quantity), 0) " +
                    "FROM invoice_lines il " +
                    "JOIN invoices inv ON il.invoice_id = inv.id " +
                    "WHERE inv.reference_invoice_id = @ref AND il.item_id = ANY(@items) " +
                    "AND inv.type IN ('sales_return','purchase_return') GROUP BY il.item_id", conn);
                cmd.Parameters.AddWithValue("ref", invoiceId);
                cmd.Parameters.AddWithValue("items", itemIds);
                using var r = cmd.ExecuteReader();
                while (r.Read()) prevByItem[r.GetInt32(0)] = Convert.ToDecimal(r.GetValue(1));
            }
            foreach (var line in Lines)
            {
                decimal prev = prevByItem.TryGetValue(line.ItemId, out var p) ? p : 0;
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
            decimal total = Lines.Sum(l => l.ReturnTotal);
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
                    decimal total = returnLines.Sum(l => l.ReturnTotal);

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
                // M8: return movements now carry warehouse, before/after quantities and cost,
                // and stock_levels is updated in the same transaction.
                int warehouseId = 0;
                bool hasInvoiceWarehouse = ColumnExists(conn, tx, "invoices", "warehouse_id");
                if (hasInvoiceWarehouse)
                {
                    using var wcmd = new NpgsqlCommand("SELECT warehouse_id FROM invoices WHERE id=@id", conn, tx);
                    wcmd.Parameters.AddWithValue("@id", originalInvoiceId);
                    var w = wcmd.ExecuteScalar();
                    if (w != null && w != DBNull.Value) warehouseId = Convert.ToInt32(w);
                }

                bool movHasWarehouse = ColumnExists(conn, tx, "inventory_movements", "warehouse_id");
                bool movHasBefore = ColumnExists(conn, tx, "inventory_movements", "before_quantity");
                bool movHasAfter = ColumnExists(conn, tx, "inventory_movements", "after_quantity");
                bool movHasCost = ColumnExists(conn, tx, "inventory_movements", "unit_cost");
                bool movHasValue = ColumnExists(conn, tx, "inventory_movements", "total_value");
                bool movHasRefType = ColumnExists(conn, tx, "inventory_movements", "reference_type");
                bool movHasDate = ColumnExists(conn, tx, "inventory_movements", "date");
                bool hasStockLevels = TableExists(conn, tx, "stock_levels");

                var unitCosts = LoadUnitCosts(conn, tx, returnLines.Select(l => l.ItemId).Distinct().ToArray());

                foreach (var l in returnLines)
                {
                    string movType = returnType == "sales_return" ? "in" : "out";
                    decimal delta = returnType == "sales_return" ? (decimal)l.ReturnQty : -(decimal)l.ReturnQty;
                    decimal before = GetCurrentStock(conn, tx, l.ItemId, warehouseId, hasStockLevels, movHasWarehouse);
                    decimal after = before + delta;
                    decimal unitCost = unitCosts.TryGetValue(l.ItemId, out var uc) ? uc : 0m;
                    decimal totalValue = unitCost * (decimal)l.ReturnQty;

                    var cols = new System.Collections.Generic.List<string> { "item_id", "quantity", "type", "reference_id" };
                    var vals = new System.Collections.Generic.List<string> { "@item", "@qty", "@type", "@ref" };
                    var cmd = new NpgsqlCommand();
                    cmd.Connection = conn;
                    cmd.Transaction = tx;
                    cmd.Parameters.AddWithValue("@item", l.ItemId);
                    cmd.Parameters.AddWithValue("@qty", l.ReturnQty);
                    cmd.Parameters.AddWithValue("@type", movType);
                    cmd.Parameters.AddWithValue("@ref", newInvoiceId);
                    if (movHasRefType) { cols.Add("reference_type"); vals.Add("'return'"); }
                    if (movHasWarehouse && warehouseId > 0) { cols.Add("warehouse_id"); vals.Add("@wh"); cmd.Parameters.AddWithValue("@wh", warehouseId); }
                    if (movHasBefore) { cols.Add("before_quantity"); vals.Add("@before"); cmd.Parameters.AddWithValue("@before", before); }
                    if (movHasAfter) { cols.Add("after_quantity"); vals.Add("@after"); cmd.Parameters.AddWithValue("@after", after); }
                    if (movHasCost) { cols.Add("unit_cost"); vals.Add("@uc"); cmd.Parameters.AddWithValue("@uc", unitCost); }
                    if (movHasValue) { cols.Add("total_value"); vals.Add("@tv"); cmd.Parameters.AddWithValue("@tv", totalValue); }
                    if (movHasDate) { cols.Add("date"); vals.Add("@now"); cmd.Parameters.AddWithValue("@now", DateTime.Now); }
                    else { cols.Add("created_at"); vals.Add("@now2"); cmd.Parameters.AddWithValue("@now2", DateTime.Now); }
                    cmd.CommandText = "INSERT INTO inventory_movements (" + string.Join(",", cols) + ") VALUES (" + string.Join(",", vals) + ")";
                    cmd.ExecuteNonQuery();
                    cmd.Dispose();

                    if (hasStockLevels && warehouseId > 0)
                        ApplyStockLevelDelta(conn, tx, l.ItemId, warehouseId, delta);
                }

                tx.Commit();
                Saved = true;
                decimal totalSaved = returnLines.Sum(l => l.ReturnTotal);
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

        private static bool ColumnExists(NpgsqlConnection conn, NpgsqlTransaction tx, string table, string column)
        {
            using var cmd = new NpgsqlCommand(
                "SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name=@t AND column_name=@c)", conn, tx);
            cmd.Parameters.AddWithValue("@t", table.ToLowerInvariant());
            cmd.Parameters.AddWithValue("@c", column.ToLowerInvariant());
            return Convert.ToBoolean(cmd.ExecuteScalar());
        }

        private static bool TableExists(NpgsqlConnection conn, NpgsqlTransaction tx, string table)
        {
            using var cmd = new NpgsqlCommand(
                "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema='public' AND table_name=@t)", conn, tx);
            cmd.Parameters.AddWithValue("@t", table.ToLowerInvariant());
            return Convert.ToBoolean(cmd.ExecuteScalar());
        }

        private static System.Collections.Generic.Dictionary<int, decimal> LoadUnitCosts(
            NpgsqlConnection conn, NpgsqlTransaction tx, int[] itemIds)
        {
            var result = new System.Collections.Generic.Dictionary<int, decimal>();
            if (itemIds == null || itemIds.Length == 0) return result;

            bool hasAvg = ColumnExists(conn, tx, "items", "avg_cost_price");
            bool hasCost = ColumnExists(conn, tx, "items", "cost_price");
            if (!hasAvg && !hasCost) return result;

            string expr = hasAvg && hasCost
                ? "COALESCE(NULLIF(avg_cost_price,0), NULLIF(cost_price,0), 0)"
                : hasAvg ? "COALESCE(avg_cost_price,0)" : "COALESCE(cost_price,0)";

            using var cmd = new NpgsqlCommand("SELECT id, " + expr + " FROM items WHERE id = ANY(@ids)", conn, tx);
            cmd.Parameters.AddWithValue("@ids", itemIds);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                result[Convert.ToInt32(r.GetValue(0))] = Convert.ToDecimal(r.GetValue(1));
            return result;
        }

        private static decimal GetCurrentStock(NpgsqlConnection conn, NpgsqlTransaction tx, int itemId,
            int warehouseId, bool hasStockLevels, bool movHasWarehouse)
        {
            if (hasStockLevels && warehouseId > 0)
            {
                using var cmd = new NpgsqlCommand(
                    "SELECT COALESCE(quantity,0) FROM stock_levels WHERE item_id=@i AND warehouse_id=@w", conn, tx);
                cmd.Parameters.AddWithValue("@i", itemId);
                cmd.Parameters.AddWithValue("@w", warehouseId);
                var v = cmd.ExecuteScalar();
                if (v != null && v != DBNull.Value) return Convert.ToDecimal(v);
            }

            var sql = "SELECT COALESCE(SUM(CASE WHEN type IN ('in','opening','transfer_in') THEN ABS(quantity) " +
                      "WHEN type IN ('out','transfer_out') THEN -ABS(quantity) " +
                      "WHEN type='adjustment' THEN quantity ELSE 0 END),0) " +
                      "FROM inventory_movements WHERE item_id=@i";
            if (movHasWarehouse && warehouseId > 0) sql += " AND warehouse_id=@w";
            using var mcmd = new NpgsqlCommand(sql, conn, tx);
            mcmd.Parameters.AddWithValue("@i", itemId);
            if (movHasWarehouse && warehouseId > 0) mcmd.Parameters.AddWithValue("@w", warehouseId);
            var mv = mcmd.ExecuteScalar();
            return mv == null || mv == DBNull.Value ? 0m : Convert.ToDecimal(mv);
        }

        private static void ApplyStockLevelDelta(NpgsqlConnection conn, NpgsqlTransaction tx, int itemId, int warehouseId, decimal delta)
        {
            using (var upd = new NpgsqlCommand(
                "UPDATE stock_levels SET quantity=COALESCE(quantity,0)+@d, last_updated=NOW() WHERE item_id=@i AND warehouse_id=@w", conn, tx))
            {
                upd.Parameters.AddWithValue("@d", delta);
                upd.Parameters.AddWithValue("@i", itemId);
                upd.Parameters.AddWithValue("@w", warehouseId);
                if (upd.ExecuteNonQuery() > 0) return;
            }

            using var ins = new NpgsqlCommand(
                "INSERT INTO stock_levels (warehouse_id, item_id, quantity, last_updated) VALUES (@w, @i, @d, NOW())", conn, tx);
            ins.Parameters.AddWithValue("@w", warehouseId);
            ins.Parameters.AddWithValue("@i", itemId);
            ins.Parameters.AddWithValue("@d", delta);
            ins.ExecuteNonQuery();
        }
    }
}