using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;
using Techtoon.UI.Services;
using Xunit;

namespace Techtoon.Tests;

/// <summary>
/// B7: الاعتراف بالإيراد عند البيع والتحصيل بسند قبض — اختبار تكامل حقيقي على قاعدة الاختبار المعزولة.
/// الحلقة: بيع → (تلقائيًا) قيد + سجل اعتراف معلّق → سند قبض → (تلقائيًا) تحصيل + تحديث الفاتورة + رصيد الخزينة.
/// </summary>
[Collection("DatabaseIntegration")]
public class RevenueRecognitionIntegrationTests
{
    private const string SkipReason = "يتطلب TESTTOON_PG_CONNECTION يشير إلى techtoon_test (قاعدة معزولة).";

    [Fact]
    public void Test_Database_Is_Isolated()
    {
        // حماية: لا نسمح لأي اختبار بيكتب على الإنتاج.
        Assert.True(TestDb.Available, SkipReason);
        var cs = TestDb.ConnectionString!;
        Assert.Contains("techtoon_test", cs, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sale_Then_Receipt_Closes_Recognition_Loop()
    {
        if (!TestDb.Available) return;

        using var conn = TestDb.Open();
        using var tx = conn.BeginTransaction();

        try
        {
            // ---------- تجهيز البيانات ----------
            int partyId = CreatePartyWithAccount(conn, tx, "عميل B7 اختبار");
            int cashBoxId = GetCashBox(conn, tx);
            int revenueAccountId = GetRevenueAccount(conn, tx);
            int partyAccountId = GetPartyAccount(conn, tx, partyId);

            // ---------- 1) إنشاء فاتورة بيع ----------
            decimal total = 1500m;
            int invoiceId = CreateSalesInvoice(conn, tx, partyId, total);

            // ---------- 2) الاعتراف التلقائي عند البيع (نفس المعاملة) ----------
            int? logId = RevenueRecognitionService.TryRecognizeInTransaction(conn, tx, invoiceId);
            Assert.True(logId.HasValue, "لم يتم الاعتراف بالإيراد عند البيع.");

            // القيد موجود ومتوازن
            var entry = GetRecognitionEntry(conn, tx, invoiceId);
            Assert.NotNull(entry);
            Assert.Equal(total, entry!.Value.Debit);
            Assert.Equal(total, entry.Value.Credit);
            Assert.Equal(partyAccountId, entry.Value.DebitAccountId);
            Assert.Equal(revenueAccountId, entry.Value.CreditAccountId);

            // السجل «معلّق» — لا payment_id بعد
            var log = GetRecognitionLog(conn, tx, logId.Value);
            Assert.NotNull(log);
            Assert.Null(log!.Value.PaymentId);      // معلّق حتى سند القبض
            Assert.Equal(total, log.Value.Amount);

            // ---------- 3) سند القبض (تحصيل) ----------
            decimal receiptAmount = 900m;
            int cashBoxBefore = GetCashBoxBalance(conn, tx, cashBoxId);

            int paymentId = RevenueRecognitionService.CreateReceiptInTransaction(
                conn, tx, invoiceId, receiptAmount, cashBoxId,
                DateTime.Today, "cash", "TEST-B7-" + invoiceId);
            Assert.True(paymentId > 0);

            // السجل أصبح «محصّل»
            var logAfter = GetRecognitionLog(conn, tx, logId.Value);
            Assert.NotNull(logAfter);
            Assert.Equal(paymentId, logAfter!.Value.PaymentId);

            // الفاتورة تحدّثت
            var invoice = GetInvoice(conn, tx, invoiceId);
            Assert.Equal(receiptAmount, invoice.Paid);
            Assert.Equal(total - receiptAmount, invoice.Remaining);
            Assert.Equal("partial", invoice.Status);

            // الخزينة استقبلت النقد
            int cashBoxAfter = GetCashBoxBalance(conn, tx, cashBoxId);
            Assert.Equal(cashBoxBefore + receiptAmount, cashBoxAfter);

            // قيد التحصيل متوازن
            var cashEntry = GetPaymentEntry(conn, tx, paymentId);
            Assert.NotNull(cashEntry);
            Assert.Equal(receiptAmount, cashEntry!.Value.Debit);
            Assert.Equal(receiptAmount, cashEntry.Value.Credit);
            Assert.Equal(cashBoxAccountId(conn, tx, cashBoxId), cashEntry.Value.DebitAccountId);
            Assert.Equal(partyAccountId, cashEntry.Value.CreditAccountId);

            // ---------- 4) تحصيل المتبقي كاملًا ----------
            int paymentId2 = RevenueRecognitionService.CreateReceiptInTransaction(
                conn, tx, invoiceId, total - receiptAmount, cashBoxId,
                DateTime.Today, "cash", "TEST-B7-" + invoiceId + "-2");
            Assert.True(paymentId2 > 0);

            var invoiceFinal = GetInvoice(conn, tx, invoiceId);
            Assert.Equal(total, invoiceFinal.Paid);
            Assert.Equal(0m, invoiceFinal.Remaining);
            Assert.Equal("paid", invoiceFinal.Status);   // لم تبقَ الفاتورة 'unpaid'

            // لا تحصيل مزدوج: لا يمكن تجاوز المتبقي
            var ex = Assert.Throws<InvalidOperationException>(() =>
                RevenueRecognitionService.CreateReceiptInTransaction(
                    conn, tx, invoiceId, 10m, cashBoxId, DateTime.Today, "cash", "TEST-B7-over"));
            Assert.Contains("المتبقي", ex.Message);

            // لا تكرار في الاعتراف
            int? logIdAgain = RevenueRecognitionService.TryRecognizeInTransaction(conn, tx, invoiceId);
            Assert.Equal(logId, logIdAgain);

            tx.Rollback(); // تنظيف — لا أثر في قاعدة الاختبار
        }
        catch
        {
            try { tx.Rollback(); } catch { }
            throw;
        }
    }

    [Fact]
    public void Purchase_Invoice_Is_Not_Recognized()
    {
        if (!TestDb.Available) return;

        using var conn = TestDb.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            int partyId = CreatePartyWithAccount(conn, tx, "مورد B7 اختبار");
            int invoiceId = CreatePurchaseInvoice(conn, tx, partyId, 300m);

            var logId = RevenueRecognitionService.TryRecognizeInTransaction(conn, tx, invoiceId);
            Assert.Null(logId);   // الاعتراف خاص بالمبيعات فقط

            tx.Rollback();
        }
        catch
        {
            try { tx.Rollback(); } catch { }
            throw;
        }
    }

    // ==================== مساعدات ====================

    private static int CreatePartyWithAccount(NpgsqlConnection conn, NpgsqlTransaction tx, string name)
    {
        int accountId;
        using (var acc = new NpgsqlCommand(
            "INSERT INTO accounts (name, type, code) VALUES (@n, 'LIABILITY', @c) RETURNING id", conn, tx))
        {
            acc.Parameters.AddWithValue("@n", "حساب " + name);
            acc.Parameters.AddWithValue("@c", "T" + Guid.NewGuid().ToString("N").Substring(0, 8));
            accountId = Convert.ToInt32(acc.ExecuteScalar());
        }
        int partyId;
        using (var p = new NpgsqlCommand(
            "INSERT INTO parties (name, type, account_id) VALUES (@n, 'customer', @a) RETURNING id", conn, tx))
        {
            p.Parameters.AddWithValue("@n", name + " " + Guid.NewGuid().ToString("N").Substring(0, 6));
            p.Parameters.AddWithValue("@a", accountId);
            partyId = Convert.ToInt32(p.ExecuteScalar());
        }
        return partyId;
    }

    private static int GetPartyAccount(NpgsqlConnection conn, NpgsqlTransaction tx, int partyId)
    {
        using var cmd = new NpgsqlCommand("SELECT account_id FROM parties WHERE id=@p", conn, tx);
        cmd.Parameters.AddWithValue("@p", partyId);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static int GetCashBox(NpgsqlConnection conn, NpgsqlTransaction tx)
    {
        using var cmd = new NpgsqlCommand("SELECT id FROM cash_boxes WHERE COALESCE(is_active,TRUE)=TRUE ORDER BY id LIMIT 1", conn, tx);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static int cashBoxAccountId(NpgsqlConnection conn, NpgsqlTransaction tx, int cashBoxId)
    {
        using var cmd = new NpgsqlCommand("SELECT account_id FROM cash_boxes WHERE id=@id", conn, tx);
        cmd.Parameters.AddWithValue("@id", cashBoxId);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static int GetRevenueAccount(NpgsqlConnection conn, NpgsqlTransaction tx)
    {
        using var cmd = new NpgsqlCommand("SELECT id FROM accounts WHERE UPPER(COALESCE(type,''))='REVENUE' ORDER BY id LIMIT 1", conn, tx);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static int CreateSalesInvoice(NpgsqlConnection conn, NpgsqlTransaction tx, int partyId, decimal total)
    {
        using var cmd = new NpgsqlCommand(@"INSERT INTO invoices
            (invoice_number, date, type, party_id, subtotal, discount_amount, total, paid_amount, status, currency_id, created_at, updated_at)
            VALUES (@n, NOW(), 'sales', @p, @t, 0, @t, 0, 'unpaid', 1, NOW(), NOW()) RETURNING id", conn, tx);
        cmd.Parameters.AddWithValue("@n", "TEST-B7-S-" + Guid.NewGuid().ToString("N").Substring(0, 8));
        cmd.Parameters.AddWithValue("@p", partyId);
        cmd.Parameters.AddWithValue("@t", total);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static int CreatePurchaseInvoice(NpgsqlConnection conn, NpgsqlTransaction tx, int partyId, decimal total)
    {
        using var cmd = new NpgsqlCommand(@"INSERT INTO invoices
            (invoice_number, date, type, party_id, subtotal, discount_amount, total, paid_amount, status, currency_id, created_at, updated_at)
            VALUES (@n, NOW(), 'purchase', @p, @t, 0, @t, 0, 'unpaid', 1, NOW(), NOW()) RETURNING id", conn, tx);
        cmd.Parameters.AddWithValue("@n", "TEST-B7-P-" + Guid.NewGuid().ToString("N").Substring(0, 8));
        cmd.Parameters.AddWithValue("@p", partyId);
        cmd.Parameters.AddWithValue("@t", total);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static (decimal Debit, decimal Credit, int DebitAccountId, int CreditAccountId)? GetRecognitionEntry(
        NpgsqlConnection conn, NpgsqlTransaction tx, int invoiceId)
    {
        using var cmd = new NpgsqlCommand(@"
            SELECT COALESCE(SUM(jl.debit),0), COALESCE(SUM(jl.credit),0),
                   COALESCE(MIN(jl.account_id) FILTER (WHERE jl.debit > 0), 0),
                   COALESCE(MIN(jl.account_id) FILTER (WHERE jl.credit > 0), 0)
            FROM journal_entries je
            JOIN journal_lines jl ON jl.entry_id = je.id
            WHERE je.source_invoice_id = @i AND je.reference_type = 'invoice'", conn, tx);
        cmd.Parameters.AddWithValue("@i", invoiceId);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        return (Convert.ToDecimal(r.GetValue(0)), Convert.ToDecimal(r.GetValue(1)),
                Convert.ToInt32(r.GetValue(2)), Convert.ToInt32(r.GetValue(3)));
    }

    private static (decimal Debit, decimal Credit, int DebitAccountId, int CreditAccountId)? GetPaymentEntry(
        NpgsqlConnection conn, NpgsqlTransaction tx, int paymentId)
    {
        using var cmd = new NpgsqlCommand(@"
            SELECT COALESCE(SUM(jl.debit),0), COALESCE(SUM(jl.credit),0),
                   COALESCE(MIN(jl.account_id) FILTER (WHERE jl.debit > 0), 0),
                   COALESCE(MIN(jl.account_id) FILTER (WHERE jl.credit > 0), 0)
            FROM journal_entries je
            JOIN journal_lines jl ON jl.entry_id = je.id
            WHERE je.source_payment_id = @p", conn, tx);
        cmd.Parameters.AddWithValue("@p", paymentId);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        return (Convert.ToDecimal(r.GetValue(0)), Convert.ToDecimal(r.GetValue(1)),
                Convert.ToInt32(r.GetValue(2)), Convert.ToInt32(r.GetValue(3)));
    }

    private static (int? PaymentId, decimal Amount)? GetRecognitionLog(NpgsqlConnection conn, NpgsqlTransaction tx, int logId)
    {
        using var cmd = new NpgsqlCommand(
            "SELECT payment_id, recognized_amount FROM cash_recognition_log WHERE id=@id", conn, tx);
        cmd.Parameters.AddWithValue("@id", logId);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        int? pid = r.IsDBNull(0) ? (int?)null : r.GetInt32(0);
        return (pid, Convert.ToDecimal(r.GetValue(1)));
    }

    private static (decimal Paid, decimal Remaining, string Status) GetInvoice(NpgsqlConnection conn, NpgsqlTransaction tx, int invoiceId)
    {
        using var cmd = new NpgsqlCommand(
            "SELECT COALESCE(paid_amount,0), COALESCE(total,0), COALESCE(status,'') FROM invoices WHERE id=@i", conn, tx);
        cmd.Parameters.AddWithValue("@i", invoiceId);
        using var r = cmd.ExecuteReader();
        r.Read();
        var paid = Convert.ToDecimal(r.GetValue(0));
        var total = Convert.ToDecimal(r.GetValue(1));
        return (paid, total - paid, r.GetString(2));
    }

    private static int GetCashBoxBalance(NpgsqlConnection conn, NpgsqlTransaction tx, int cashBoxId)
    {
        using var cmd = new NpgsqlCommand(@"
            SELECT COALESCE((SELECT opening_balance FROM cash_boxes WHERE id=@id),0)
                 + COALESCE((SELECT SUM(amount) FROM payments WHERE cash_box_id=@id AND payment_type='receipt'),0)
                 - COALESCE((SELECT SUM(amount) FROM payments WHERE cash_box_id=@id AND payment_type='payment'),0)
                 - COALESCE((SELECT SUM(amount) FROM expenses WHERE cash_box_id=@id),0)
                 - COALESCE((SELECT SUM(amount) FROM cash_transfers WHERE from_cash_box_id=@id AND status='completed'),0)
                 + COALESCE((SELECT SUM(amount) FROM cash_transfers WHERE to_cash_box_id=@id AND status='completed'),0)", conn, tx);
        cmd.Parameters.AddWithValue("@id", cashBoxId);
        return Convert.ToInt32(Convert.ToDecimal(cmd.ExecuteScalar()));
    }
}
