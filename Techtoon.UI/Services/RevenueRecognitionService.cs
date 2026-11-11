using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;
using Techtoon.Core.Data;
using Techtoon.UI.Services;

namespace Techtoon.UI.Services
{
    /// <summary>
    /// P5: قاعدة الاعتراف بالإيراد — "يُعترف بالإيراد عند البيع، ويبقى معلّقًا حتى تحويله إلى سند قبض".
    /// تُنشأ القيود عند البيع (مدين: حساب الطرف/الذمم — دائن: الإيراد) ويُسجَّل في cash_recognition_log.
    /// عند إنشاء سند القبض يرتبط بالسجل ويُعلَّم بأنه محصّل.
    /// </summary>
    public static class RevenueRecognitionService
    {
        public class RecognitionRow
        {
            public int InvoiceId { get; set; }
            public string InvoiceNumber { get; set; } = "";
            public string PartyName { get; set; } = "";
            public DateTime InvoiceDate { get; set; }
            public decimal Total { get; set; }
            public decimal Paid { get; set; }
            public decimal Remaining => Total - Paid;
            public bool Recognized { get; set; }
            public bool Collected { get; set; }
            public int? RecognitionLogId { get; set; }
            public string StatusDisplay => Collected ? "محصّل (سند قبض)" : Recognized ? "مُعترف به — بانتظار سند قبض" : "غير مُعترف به";
        }

        public static string GetCS() => SecureConnectionManager.GetConnectionString();

        /// <summary>كل فواتير البيع مع حالة الاعتراف والتحصيل (للواجهة).</summary>
        public static List<RecognitionRow> GetRecognitions(string? search = null, int? status = null)
        {
            var list = new List<RecognitionRow>();
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                SELECT i.id, i.invoice_number, COALESCE(p.name,''), i.date, COALESCE(i.total,0), COALESCE(i.paid_amount,0),
                       (SELECT COUNT(*) FROM cash_recognition_log c JOIN journal_entries je ON je.id = c.journal_entry_id WHERE je.source_invoice_id = i.id),
                       (SELECT c.payment_id FROM cash_recognition_log c JOIN journal_entries je ON je.id = c.journal_entry_id WHERE je.source_invoice_id = i.id LIMIT 1)
                FROM invoices i LEFT JOIN parties p ON p.id = i.party_id
                WHERE i.type = 'sales' AND i.status <> 'cancelled'
                ORDER BY i.date DESC, i.id DESC LIMIT 500", conn);
            if (!string.IsNullOrWhiteSpace(search))
            {
                cmd.CommandText = @"
                SELECT i.id, i.invoice_number, COALESCE(p.name,''), i.date, COALESCE(i.total,0), COALESCE(i.paid_amount,0),
                       (SELECT COUNT(*) FROM cash_recognition_log c JOIN journal_entries je ON je.id = c.journal_entry_id WHERE je.source_invoice_id = i.id),
                       (SELECT c.payment_id FROM cash_recognition_log c JOIN journal_entries je ON je.id = c.journal_entry_id WHERE je.source_invoice_id = i.id LIMIT 1)
                FROM invoices i LEFT JOIN parties p ON p.id = i.party_id
                WHERE i.type = 'sales' AND i.status <> 'cancelled' AND (i.invoice_number ILIKE @q OR p.name ILIKE @q)
                ORDER BY i.date DESC, i.id DESC LIMIT 500";
                cmd.Parameters.AddWithValue("@q", "%" + search + "%");
            }
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var row = new RecognitionRow
                {
                    InvoiceId = r.GetInt32(0),
                    InvoiceNumber = r.GetString(1),
                    PartyName = r.GetString(2),
                    InvoiceDate = DbValue.AsDateTime(r.GetValue(3)),
                    Total = Convert.ToDecimal(r.GetValue(4)),
                    Paid = Convert.ToDecimal(r.GetValue(5)),
                    Recognized = r.GetInt32(6) > 0,
                    Collected = r.IsDBNull(7) ? false : r.GetInt32(7) > 0
                };
                if (status.HasValue)
                {
                    if (status.Value == 1 && !row.Recognized) continue;
                    if (status.Value == 2 && !row.Collected) continue;
                }
                list.Add(row);
            }
            return list;
        }

        /// <summary>يسجل الاعتراف بالإيراد لفاتورة بيع (قيد + سجل) إن لم يكن مسجلاً.</summary>
        public static async System.Threading.Tasks.Task<int> EnsureRecognizedAsync(int invoiceId)
        {
            using var conn = new NpgsqlConnection(GetCS());
            await conn.OpenAsync();
            using var tx = await conn.BeginTransactionAsync();
            try
            {
                int? existing;
                using (var chk = new NpgsqlCommand("SELECT c.id FROM cash_recognition_log c JOIN journal_entries je ON je.id=c.journal_entry_id WHERE je.source_invoice_id=@i LIMIT 1", conn, tx))
                {
                    chk.Parameters.AddWithValue("@i", invoiceId);
                    var v = chk.ExecuteScalar();
                    existing = v == null || v == DBNull.Value ? (int?)null : Convert.ToInt32(v);
                }
                if (existing.HasValue) { await tx.RollbackAsync(); return existing.Value; }

                int partyAccountId, invoicePartyId;
                decimal total;
                DateTime date;
                string number;
                using (var cmd = new NpgsqlCommand("SELECT i.party_id, COALESCE(i.total,0), i.date, i.invoice_number FROM invoices i WHERE i.id=@i AND i.type='sales'", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@i", invoiceId);
                    using var r = cmd.ExecuteReader();
                    if (!r.Read()) throw new InvalidOperationException("الفاتورة غير موجودة أو ليست فاتورة بيع.");
                    invoicePartyId = r.GetInt32(0);
                    total = Convert.ToDecimal(r.GetValue(1));
                    date = DbValue.AsDateTime(r.GetValue(2));
                    number = r.GetString(3);
                }
                using (var acc = new NpgsqlCommand("SELECT COALESCE(account_id,0) FROM parties WHERE id=@p", conn, tx))
                {
                    acc.Parameters.AddWithValue("@p", invoicePartyId);
                    partyAccountId = Convert.ToInt32(await acc.ExecuteScalarAsync());
                }
                if (partyAccountId <= 0) throw new InvalidOperationException("لا يوجد حساب مرتبط بالطرف — عيّن حساب المدينين للعميل أولاً.");
                int revenueAccountId;
                using (var acc = new NpgsqlCommand("SELECT id FROM accounts WHERE UPPER(COALESCE(type,''))='REVENUE' ORDER BY id LIMIT 1", conn, tx))
                {
                    var v = await acc.ExecuteScalarAsync();
                    if (v == null || v == DBNull.Value) throw new InvalidOperationException("لا يوجد حساب إيراد في دليل الحسابات.");
                    revenueAccountId = Convert.ToInt32(v);
                }

                int entryId;
                using (var ins = new NpgsqlCommand(@"INSERT INTO journal_entries
                    (date, description, reference_type, reference_id, source_invoice_id, created_by, period_id, cash_basis_posted)
                    VALUES (CAST(@d AS timestamp), @desc, 'invoice', @iid, @iid, 1, NULL, FALSE) RETURNING id", conn, tx))
                {
                    ins.Parameters.AddWithValue("@d", date);
                    ins.Parameters.AddWithValue("@desc", "إيراد فاتورة " + number);
                    ins.Parameters.AddWithValue("@iid", invoiceId);
                    entryId = Convert.ToInt32(await ins.ExecuteScalarAsync());
                }
                using (var ins = new NpgsqlCommand("INSERT INTO journal_lines (entry_id, account_id, debit, credit) VALUES (@e, @a, @v, 0)", conn, tx))
                {
                    ins.Parameters.AddWithValue("@e", entryId);
                    ins.Parameters.AddWithValue("@a", partyAccountId);
                    ins.Parameters.AddWithValue("@v", total);
                    await ins.ExecuteNonQueryAsync();
                }
                using (var ins = new NpgsqlCommand("INSERT INTO journal_lines (entry_id, account_id, debit, credit) VALUES (@e, @a, 0, @v)", conn, tx))
                {
                    ins.Parameters.AddWithValue("@e", entryId);
                    ins.Parameters.AddWithValue("@a", revenueAccountId);
                    ins.Parameters.AddWithValue("@v", total);
                    await ins.ExecuteNonQueryAsync();
                }
                int logId;
                using (var ins = new NpgsqlCommand(@"INSERT INTO cash_recognition_log
                    (journal_entry_id, recognition_date, recognized_amount, recognized_by, recognized_at)
                    VALUES (@e, CAST(@d AS date), @v, 1, NOW()) RETURNING id", conn, tx))
                {
                    ins.Parameters.AddWithValue("@e", entryId);
                    ins.Parameters.AddWithValue("@d", date);
                    ins.Parameters.AddWithValue("@v", total);
                    ins.Parameters.AddWithValue("@i", invoiceId);
                    logId = Convert.ToInt32(await ins.ExecuteScalarAsync());
                }
                tx.Commit();
                AuditService.Log("RECOGNIZE", "Invoice", invoiceId, "Revenue recognized at sale (journal entry " + entryId + ", amount " + total.ToString("N2") + ")");
                return logId;
            }
            catch
            {
                try { await tx.RollbackAsync(); } catch { }
                throw;
            }
        }

        /// <summary>يربط سند القبض بسجل الاعتراف (تحصيل).</summary>
        public static void MarkCollected(int invoiceId, int paymentId)
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var cmd = new NpgsqlCommand("UPDATE cash_recognition_log c SET payment_id=@p FROM journal_entries je WHERE je.id=c.journal_entry_id AND (je.source_invoice_id=@i OR je.reference_id=@i) AND c.payment_id IS NULL", conn);
            cmd.Parameters.AddWithValue("@p", paymentId);
            cmd.Parameters.AddWithValue("@i", invoiceId);
            cmd.ExecuteNonQuery();
        }
    }
}
