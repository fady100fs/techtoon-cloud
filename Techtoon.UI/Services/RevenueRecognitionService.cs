using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using Techtoon.Core.Data;
using Techtoon.UI.Services;

namespace Techtoon.UI.Services
{
    /// <summary>
    /// B7: قاعدة الاعتراف بالإيراد — «يُعترف بالإيراد عند البيع، ويبقى معلّقًا حتى تحويله إلى سند قبض».
    ///
    /// <para><b>القاعدة المحاسبية المعتمدة:</b></para>
    /// <list type="bullet">
    /// <item>عند تسجيل فاتورة بيع: يُنشأ قيد (مدين: حساب الطرف/الذمم — دائن: الإيراد) ويُسجَّل في cash_recognition_log.</item>
    /// <item>يبقى السجل بحالة «معلّق» حتى تحويله إلى سند قبض (لا يُحصَّل الإيراد مرتين).</item>
    /// <item>عند إنشاء سند القبض: يُربط السجل بالسند (payment_id) ويُحدَّث قيد الاعتراف (مدين: النقدية — دائن: حساب الطرف) ويُحدَّث رصيد الفاتورة وحالتها.</item>
    /// <item>كل العمليات (فاتورة + قيد + سجل + سند + نقدية) تتم داخل معاملة واحدة.</item>
    /// </list>
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

        // ==================== الاعتراف عند البيع ====================

        /// <summary>
        /// يسجّل الاعتراف بالإيراد لفاتورة بيع داخل معاملة قائمة (لا يفتح معاملة جديدة).
        /// تُستدعى من مسار حفظ الفاتورة حتى يكون الاعتراف جزءًا من العملية الذرّية نفسها.
        /// </summary>
        /// <param name="conn">اتصال مفتوح.</param>
        /// <param name="tx">المعاملة الجارية (تُستخدم كل الاستعلامات).</param>
        /// <param name="invoiceId">معرّف فاتورة البيع.</param>
        /// <returns>معرّف سجل الاعتراف، أو null إذا لم يكن هناك طرف/إيراد مُهيَّأ.</returns>
        public static int? TryRecognizeInTransaction(NpgsqlConnection conn, NpgsqlTransaction tx, int invoiceId)
        {
            // 1) هل سبق الاعتراف؟ (لا نكرّر القيد)
            int? existing;
            using (var chk = new NpgsqlCommand(
                "SELECT c.id FROM cash_recognition_log c JOIN journal_entries je ON je.id=c.journal_entry_id WHERE je.source_invoice_id=@i LIMIT 1", conn, tx))
            {
                chk.Parameters.AddWithValue("@i", invoiceId);
                var v = chk.ExecuteScalar();
                existing = v == null || v == DBNull.Value ? (int?)null : Convert.ToInt32(v);
            }
            if (existing.HasValue) return existing.Value;

            // 2) بيانات الفاتورة
            int partyId;
            decimal total;
            DateTime date;
            string number;
            using (var cmd = new NpgsqlCommand(
                "SELECT i.party_id, COALESCE(i.total,0), i.date, i.invoice_number FROM invoices i WHERE i.id=@i AND i.type='sales'", conn, tx))
            {
                cmd.Parameters.AddWithValue("@i", invoiceId);
                using var r = cmd.ExecuteReader();
                if (!r.Read()) return null; // ليست فاتورة بيع
                partyId = r.GetInt32(0);
                total = Convert.ToDecimal(r.GetValue(1));
                date = DbValue.AsDateTime(r.GetValue(2));
                number = r.GetString(3);
            }
            if (total <= 0) return null;

            // 3) حساب المدين (حساب الطرف)
            int partyAccountId;
            using (var acc = new NpgsqlCommand("SELECT COALESCE(account_id,0) FROM parties WHERE id=@p", conn, tx))
            {
                acc.Parameters.AddWithValue("@p", partyId);
                var v = acc.ExecuteScalar();
                partyAccountId = v == null || v == DBNull.Value ? 0 : Convert.ToInt32(v);
            }
            if (partyAccountId <= 0) return null; // لا حساب مرتبط بالطرف

            // 4) حساب الإيراد
            int revenueAccountId;
            using (var acc = new NpgsqlCommand(
                "SELECT id FROM accounts WHERE UPPER(COALESCE(type,''))='REVENUE' ORDER BY id LIMIT 1", conn, tx))
            {
                var v = acc.ExecuteScalar();
                if (v == null || v == DBNull.Value) return null;
                revenueAccountId = Convert.ToInt32(v);
            }

            // 5) القيد: مدين الذمم / دائن الإيراد
            int entryId;
            using (var ins = new NpgsqlCommand(@"INSERT INTO journal_entries
                (date, description, reference_type, reference_id, source_invoice_id, created_by, period_id, cash_basis_posted)
                VALUES (CAST(@d AS timestamp), @desc, 'invoice', @iid, @iid, 1, NULL, FALSE) RETURNING id", conn, tx))
            {
                ins.Parameters.AddWithValue("@d", date);
                ins.Parameters.AddWithValue("@desc", "إيراد فاتورة " + number);
                ins.Parameters.AddWithValue("@iid", invoiceId);
                entryId = Convert.ToInt32(ins.ExecuteScalar());
            }
            using (var ins = new NpgsqlCommand("INSERT INTO journal_lines (entry_id, account_id, debit, credit) VALUES (@e, @a, @v, 0)", conn, tx))
            {
                ins.Parameters.AddWithValue("@e", entryId);
                ins.Parameters.AddWithValue("@a", partyAccountId);
                ins.Parameters.AddWithValue("@v", total);
                ins.ExecuteNonQuery();
            }
            using (var ins = new NpgsqlCommand("INSERT INTO journal_lines (entry_id, account_id, debit, credit) VALUES (@e, @a, 0, @v)", conn, tx))
            {
                ins.Parameters.AddWithValue("@e", entryId);
                ins.Parameters.AddWithValue("@a", revenueAccountId);
                ins.Parameters.AddWithValue("@v", total);
                ins.ExecuteNonQuery();
            }

            // 6) سجل الاعتراف — يبقى بلا payment_id (معلّق حتى سند القبض)
            int logId;
            using (var ins = new NpgsqlCommand(@"INSERT INTO cash_recognition_log
                (journal_entry_id, recognition_date, recognized_amount, recognized_by, recognized_at)
                VALUES (@e, CAST(@d AS date), @v, 1, NOW()) RETURNING id", conn, tx))
            {
                ins.Parameters.AddWithValue("@e", entryId);
                ins.Parameters.AddWithValue("@d", date);
                ins.Parameters.AddWithValue("@v", total);
                logId = Convert.ToInt32(ins.ExecuteScalar());
            }
            return logId;
        }

        /// <summary>يسجّل الاعتراف بالإيراد لفاتورة بيع (معاملة مستقلة) — للاستخدام اليدوي.</summary>
        public static async Task<int> EnsureRecognizedAsync(int invoiceId)
        {
            using var conn = new NpgsqlConnection(GetCS());
            await conn.OpenAsync();
            using var tx = await conn.BeginTransactionAsync();
            try
            {
                var logId = TryRecognizeInTransaction(conn, tx, invoiceId);
                if (!logId.HasValue)
                    throw new InvalidOperationException("تعذّر الاعتراف بالإيراد — تأكد من وجود حساب للطرف وحساب إيراد في دليل الحسابات.");
                await tx.CommitAsync();
                AuditService.Log("RECOGNIZE", "Invoice", invoiceId, "Revenue recognized at sale (recognition log " + logId.Value + ")");
                return logId.Value;
            }
            catch
            {
                try { await tx.RollbackAsync(); } catch { }
                throw;
            }
        }

        // ==================== التحصيل (سند القبض) ====================

        /// <summary>
        /// يحوّل فاتورة بيع إلى سند قبض — كل شيء في معاملة واحدة:
        /// سند قبض + قيد نقدية + تحديث الفاتورة (المبلغ والحالة) + ربط سجل الاعتراف.
        /// </summary>
        /// <param name="conn">اتصال مفتوح.</param>
        /// <param name="tx">المعاملة الجارية.</param>
        /// <param name="invoiceId">معرّف فاتورة البيع.</param>
        /// <param name="amount">مبلغ التحصيل.</param>
        /// <param name="cashBoxId">الخزينة المستقبِلة.</param>
        /// <param name="date">تاريخ السند.</param>
        /// <param name="method">طريقة الدفع (نقدي / تحويل بنكي ...).</param>
        /// <param name="reference">مرجع السند.</param>
        /// <returns>معرّف سند القبض.</returns>
        public static int CreateReceiptInTransaction(NpgsqlConnection conn, NpgsqlTransaction tx, int invoiceId,
            decimal amount, int cashBoxId, DateTime date, string method, string reference)
        {
            if (amount <= 0) throw new InvalidOperationException("مبلغ السند يجب أن يكون أكبر من صفر.");

            // 1) بيانات الفاتورة (لحساب المتبقي)
            int partyId;
            decimal total, paid;
            using (var cmd = new NpgsqlCommand(
                "SELECT i.party_id, COALESCE(i.total,0), COALESCE(i.paid_amount,0) FROM invoices i WHERE i.id=@i AND i.type='sales' FOR UPDATE", conn, tx))
            {
                cmd.Parameters.AddWithValue("@i", invoiceId);
                using var r = cmd.ExecuteReader();
                if (!r.Read()) throw new InvalidOperationException("الفاتورة غير موجودة أو ليست فاتورة بيع.");
                partyId = r.GetInt32(0);
                total = Convert.ToDecimal(r.GetValue(1));
                paid = Convert.ToDecimal(r.GetValue(2));
            }
            var remaining = total - paid;
            if (amount > remaining + 0.005m)
                throw new InvalidOperationException("المبلغ أكبر من المتبقي على الفاتورة (" + remaining.ToString("N2") + ").");

            // 2) سند القبض (مقبوضات نقدية)
            int paymentId;
            using (var pay = new NpgsqlCommand(@"INSERT INTO payments
                (date, amount, payment_type, payment_method, reference_number, notes, cash_box_id, currency_id, invoice_id)
                VALUES (CAST(@d AS date), @a, 'receipt', @m, @r, @n, @cb, @cur, @inv) RETURNING id", conn, tx))
            {
                pay.Parameters.AddWithValue("@d", date);
                pay.Parameters.AddWithValue("@a", amount);
                pay.Parameters.AddWithValue("@m", method);
                pay.Parameters.AddWithValue("@r", reference);
                pay.Parameters.AddWithValue("@n", "تحويل فاتورة إلى سند قبض");
                pay.Parameters.AddWithValue("@cb", cashBoxId);
                pay.Parameters.AddWithValue("@cur", DbDefaults.GetDefaultCurrencyId(conn));
                pay.Parameters.AddWithValue("@inv", invoiceId);
                paymentId = Convert.ToInt32(pay.ExecuteScalar());
            }

            // 3) قيد التحصيل: مدين الخزينة / دائن حساب الطرف
            int cashAccountId;
            using (var cmd = new NpgsqlCommand("SELECT account_id FROM cash_boxes WHERE id=@cb", conn, tx))
            {
                cmd.Parameters.AddWithValue("@cb", cashBoxId);
                var v = cmd.ExecuteScalar();
                if (v == null || v == DBNull.Value) throw new InvalidOperationException("الخزينة غير مرتبطة بحساب محاسبي.");
                cashAccountId = Convert.ToInt32(v);
            }
            int partyAccountId;
            using (var cmd = new NpgsqlCommand("SELECT COALESCE(account_id,0) FROM parties WHERE id=@p", conn, tx))
            {
                cmd.Parameters.AddWithValue("@p", partyId);
                var v = cmd.ExecuteScalar();
                partyAccountId = v == null || v == DBNull.Value ? 0 : Convert.ToInt32(v);
            }
            if (partyAccountId <= 0)
                throw new InvalidOperationException("لا يوجد حساب محاسبي مرتبط بالطرف — عيّن حساب المدينين أولاً.");

            int entryId;
            using (var ins = new NpgsqlCommand(@"INSERT INTO journal_entries
                (date, description, reference_type, reference_id, source_invoice_id, source_payment_id, created_by, period_id, cash_basis_posted)
                VALUES (CAST(@d AS timestamp), @desc, 'payment', @pid, @inv, @pid, 1, NULL, FALSE) RETURNING id", conn, tx))
            {
                ins.Parameters.AddWithValue("@d", date);
                ins.Parameters.AddWithValue("@desc", "تحصيل فاتورة " + invoiceId + " بسند قبض");
                ins.Parameters.AddWithValue("@pid", paymentId);
                ins.Parameters.AddWithValue("@inv", invoiceId);
                entryId = Convert.ToInt32(ins.ExecuteScalar());
            }
            using (var ins = new NpgsqlCommand("INSERT INTO journal_lines (entry_id, account_id, debit, credit) VALUES (@e, @a, @v, 0)", conn, tx))
            {
                ins.Parameters.AddWithValue("@e", entryId);
                ins.Parameters.AddWithValue("@a", cashAccountId);
                ins.Parameters.AddWithValue("@v", amount);
                ins.ExecuteNonQuery();
            }
            using (var ins = new NpgsqlCommand("INSERT INTO journal_lines (entry_id, account_id, debit, credit) VALUES (@e, @a, 0, @v)", conn, tx))
            {
                ins.Parameters.AddWithValue("@e", entryId);
                ins.Parameters.AddWithValue("@a", partyAccountId);
                ins.Parameters.AddWithValue("@v", amount);
                ins.ExecuteNonQuery();
            }

            // 4) تحديث الفاتورة: المبلغ المدفوع + الحالة
            var newPaid = paid + amount;
            var newStatus = newPaid >= total - 0.005m ? "paid" : (newPaid > 0 ? "partial" : "unpaid");
            using (var up = new NpgsqlCommand(
                "UPDATE invoices SET paid_amount=@p, status=@s, updated_at=NOW() WHERE id=@i", conn, tx))
            {
                up.Parameters.AddWithValue("@p", newPaid);
                up.Parameters.AddWithValue("@s", newStatus);
                up.Parameters.AddWithValue("@i", invoiceId);
                up.ExecuteNonQuery();
            }

            // 5) ربط سجل الاعتراف بالسند (إقفال التعليق)
            using (var up = new NpgsqlCommand(@"UPDATE cash_recognition_log c
                SET payment_id=@p
                FROM journal_entries je
                WHERE je.id=c.journal_entry_id
                  AND je.source_invoice_id=@i
                  AND c.payment_id IS NULL", conn, tx))
            {
                up.Parameters.AddWithValue("@p", paymentId);
                up.Parameters.AddWithValue("@i", invoiceId);
                up.ExecuteNonQuery();
            }

            // 6) سطر دفعة الفاتورة
            using (var ins = new NpgsqlCommand(@"INSERT INTO invoice_payments (invoice_id, method, amount, reference, paid_at, created_at)
                VALUES (@i, @m, @a, @r, CAST(@d AS date), NOW())", conn, tx))
            {
                ins.Parameters.AddWithValue("@i", invoiceId);
                ins.Parameters.AddWithValue("@m", method);
                ins.Parameters.AddWithValue("@a", amount);
                ins.Parameters.AddWithValue("@r", reference);
                ins.Parameters.AddWithValue("@d", date);
                ins.ExecuteNonQuery();
            }

            return paymentId;
        }

        /// <summary>يربط سند القبض بسجل الاعتراف (تحصيل) — معاملة مستقلة.</summary>
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
