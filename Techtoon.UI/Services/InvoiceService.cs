using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using Techtoon.Core.Data;
using Techtoon.UI.Services;

namespace Techtoon.UI.Services
{
    /// <summary>
    /// P5: خدمة إنشاء الفاتورة المشتركة — تستخدمها نافذة الفاتورة وال��واتير المتكررة.
    /// تراعي: الفاتورة + السطور + حركات المخزون + المدفوعات، كل ذلك داخل Transaction واحدة.
    /// </summary>
    public class InvoiceLineInput
    {
        public int ItemId { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
    }

    public class InvoicePaymentInput
    {
        public decimal Amount { get; set; }
        public string Method { get; set; } = "cash";
        public string Reference { get; set; } = "";
        public DateTime PaidAt { get; set; } = DateTime.Now;
    }

    public class InvoiceInput
    {
        public string InvoiceNumber { get; set; } = "";
        public DateTime Date { get; set; } = DateTime.Now;
        public string Type { get; set; } = "sales";     // sales | purchase
        public int PartyId { get; set; }
        public int WarehouseId { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public string Status { get; set; } = "unpaid";
        public List<InvoiceLineInput> Lines { get; set; } = new();
        public List<InvoicePaymentInput> Payments { get; set; } = new();
        public string Notes { get; set; } = "";
        public string SourceType { get; set; } = "";   // recurring_invoice gibi
        public int? SourceId { get; set; }
    }

    public static class InvoiceService
    {
        /// <summary>
        /// ينشئ فاتورة كاملة (فاتورة + سطور + حركات مخزون + مدفوعات) في Transaction واحدة.
        /// يرجع رقم الفاتورة (id). يرمي استثناءً مع رسالة عربية واضحة عند الفشل.
        /// </summary>
        public static async Task<int> CreateInvoiceAsync(InvoiceInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.PartyId <= 0) throw new InvalidOperationException("يجب اختيار العميل/المورد.");
            if (input.Lines == null || input.Lines.Count == 0) throw new InvalidOperationException("لا توجد أصناف في الفاتورة.");
            if (string.IsNullOrWhiteSpace(input.InvoiceNumber)) throw new InvalidOperationException("رقم الفاتورة مطلوب.");

            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            await conn.OpenAsync();
            using var tx = await conn.BeginTransactionAsync();
            try
            {
                decimal subtotal = input.Lines.Sum(l => l.Total);
                decimal total = Math.Round(subtotal - input.DiscountAmount, 2);

                int invoiceId;
                using (var cmd = new NpgsqlCommand(@"INSERT INTO invoices
                    (invoice_number, date, type, party_id, warehouse_id, subtotal, discount_amount, total, paid_amount, status, currency_id, created_at, updated_at)
                    VALUES (@sn, @dt, @tp, @pid, @wid, @st, @da, @tt, @pa, @status, 1, NOW(), NOW()) RETURNING id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@sn", input.InvoiceNumber);
                    cmd.Parameters.AddWithValue("@dt", input.Date);
                    cmd.Parameters.AddWithValue("@tp", input.Type);
                    cmd.Parameters.AddWithValue("@pid", input.PartyId);
                    cmd.Parameters.AddWithValue("@wid", input.WarehouseId > 0 ? (object)input.WarehouseId : DBNull.Value);
                    cmd.Parameters.AddWithValue("@st", subtotal);
                    cmd.Parameters.AddWithValue("@da", input.DiscountAmount);
                    cmd.Parameters.AddWithValue("@tt", total);
                    cmd.Parameters.AddWithValue("@pa", input.PaidAmount);
                    cmd.Parameters.AddWithValue("@status", input.Status);
                    invoiceId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                bool isSale = string.Equals(input.Type, "sales", StringComparison.OrdinalIgnoreCase)
                           || string.Equals(input.Type, "sale", StringComparison.OrdinalIgnoreCase);
                string movType = isSale ? "out" : "in";

                foreach (var line in input.Lines)
                {
                    if (line.ItemId <= 0) continue;
                    using (var cmd = new NpgsqlCommand("INSERT INTO invoice_lines (invoice_id, item_id, quantity, price, discount, total) VALUES (@inv, @iid, @qty, @prc, @dsc, @tot)", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@inv", invoiceId);
                        cmd.Parameters.AddWithValue("@iid", line.ItemId);
                        cmd.Parameters.AddWithValue("@qty", line.Quantity);
                        cmd.Parameters.AddWithValue("@prc", line.Price);
                        cmd.Parameters.AddWithValue("@dsc", line.Discount);
                        cmd.Parameters.AddWithValue("@tot", line.Total);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // حركة المخزون (نفس قاعدة الـ API: النوع يحدّد الإشارة والمقدار موجب)
                    var cols = new List<string> { "item_id", "type", "quantity" };
                    var vals = new List<string> { "@item", "@mtype", "@qty" };
                    var pars = new List<NpgsqlParameter>
                    {
                        new NpgsqlParameter("item", line.ItemId),
                        new NpgsqlParameter("mtype", movType),
                        new NpgsqlParameter("qty", Math.Abs(line.Quantity))
                    };
                    if (HasColumn(conn, tx, "inventory_movements", "date")) { cols.Add("date"); vals.Add("NOW()"); }
                    if (HasColumn(conn, tx, "inventory_movements", "reference_id")) { cols.Add("reference_id"); vals.Add("@refId"); pars.Add(new NpgsqlParameter("refId", invoiceId)); }
                    if (HasColumn(conn, tx, "inventory_movements", "reference_type")) { cols.Add("reference_type"); vals.Add("'invoice'"); }
                    if (HasColumn(conn, tx, "inventory_movements", "warehouse_id") && input.WarehouseId > 0) { cols.Add("warehouse_id"); vals.Add("@wh"); pars.Add(new NpgsqlParameter("wh", input.WarehouseId)); }
                    if (HasColumn(conn, tx, "inventory_movements", "created_at")) { cols.Add("created_at"); vals.Add("NOW()"); }
                    if (HasColumn(conn, tx, "inventory_movements", "status")) { cols.Add("status"); vals.Add("'posted'"); }
                    using (var cmd = new NpgsqlCommand("INSERT INTO inventory_movements (" + string.Join(",", cols) + ") VALUES (" + string.Join(",", vals) + ")", conn, tx))
                    {
                        foreach (var p in pars) cmd.Parameters.Add(p);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                foreach (var pay in input.Payments)
                {
                    if (pay.Amount <= 0) continue;
                    using var cmd = new NpgsqlCommand(@"INSERT INTO invoice_payments (invoice_id, method, amount, reference, paid_at, created_at)
                        VALUES (@inv, @m, @a, @r, @p, NOW())", conn, tx);
                    cmd.Parameters.AddWithValue("@inv", invoiceId);
                    cmd.Parameters.AddWithValue("@m", pay.Method);
                    cmd.Parameters.AddWithValue("@a", pay.Amount);
                    cmd.Parameters.AddWithValue("@r", (object?)pay.Reference ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@p", pay.PaidAt);
                    await cmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                AuditService.Log("CREATE", "Invoice", invoiceId,
                    "Invoice " + input.InvoiceNumber + " (" + input.Type + ") total=" + total +
                    (string.IsNullOrEmpty(input.SourceType) ? "" : " source=" + input.SourceType + ":" + input.SourceId));
                return invoiceId;
            }
            catch
            {
                try { await tx.RollbackAsync(); } catch { }
                throw;
            }
        }

        /// <summary>يتحقق من وجود عمود في جدول (لبناء جمل INSERT مرنة).</summary>
        private static bool HasColumn(NpgsqlConnection conn, NpgsqlTransaction tx, string table, string column)
        {
            try
            {
                using var cmd = new NpgsqlCommand(@"SELECT 1 FROM information_schema.columns
                    WHERE table_schema='public' AND table_name=@t AND column_name=@c LIMIT 1", conn, tx);
                cmd.Parameters.AddWithValue("@t", table);
                cmd.Parameters.AddWithValue("@c", column);
                return cmd.ExecuteScalar() != null;
            }
            catch { return false; }
        }

        /// <summary>يولّد رقم فاتورة تسلسليًا (نفس نمط الفاتورة اليدوية).</summary>
        public static async Task<string> NextInvoiceNumberAsync(string type)
        {
            string prefix = string.Equals(type, "purchase", StringComparison.OrdinalIgnoreCase) ? "PUR" : "SAL";
            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand("SELECT COALESCE(MAX(id),0) FROM invoices", conn);
            var maxId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return $"{prefix}-{DateTime.Now:yyyy}-{maxId + 1:0000}";
        }
    }
}
