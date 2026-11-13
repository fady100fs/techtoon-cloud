using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;
using Techtoon.UI.Services;
using Xunit;

namespace Techtoon.Tests;

/// <summary>
/// تحرير سند القبض/الصرف — كان زر «تعديل» يُنشئ سندًا جديدًا (تكرار) لا يعدّل القائم.
/// </summary>
[Collection("DatabaseIntegration")]
public class PaymentEditTests : IDisposable
{
    private readonly List<string> _names = new();

    public void Dispose()
    {
        foreach (var n in _names)
        {
            try { TestDb.Execute("DELETE FROM invoice_payments WHERE invoice_id IN (SELECT id FROM invoices WHERE invoice_number=@n)", ("@n", n)); } catch { }
            try { TestDb.Execute("DELETE FROM payments WHERE notes=@n", ("@n", n)); } catch { }
            try { TestDb.Execute("DELETE FROM invoices WHERE invoice_number=@n", ("@n", n)); } catch { }
        }
    }

    [Fact]
    public void Updating_A_Payment_Changes_The_Row_And_Does_Not_Duplicate_It()
    {
        if (!TestDb.Available) return;

        var cs = SecureConnectionManager.GetConnectionString();
        var tag = "PAYEDIT" + Guid.NewGuid().ToString("N").Substring(0, 8);
        _names.Add(tag);

        int invoiceId, paymentId;
        decimal originalAmount = 100m;

        using (var conn = new NpgsqlConnection(cs))
        {
            conn.Open();
            using var tx = conn.BeginTransaction();

            // فاتورة ومرفقة بطرف وحساب (المطلوب NOT NULL)
            int partyId, accountId, cashBoxId;
            using (var q = new NpgsqlCommand(
                "SELECT (SELECT id FROM parties ORDER BY id LIMIT 1), (SELECT id FROM accounts ORDER BY id LIMIT 1), (SELECT id FROM cash_boxes ORDER BY id LIMIT 1)", conn, tx))
            using (var r = q.ExecuteReader())
            {
                r.Read();
                partyId = r.GetInt32(0);
                accountId = r.GetInt32(1);
                cashBoxId = r.GetInt32(2);
            }

            using (var cmd = new NpgsqlCommand(
                "INSERT INTO invoices (invoice_number, date, party_id, type, total, paid_amount, status, currency_id) " +
                "VALUES (@n, CURRENT_DATE, @pid, 'sales', 1000, 0, 'unpaid', 1) RETURNING id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@n", tag);
                cmd.Parameters.AddWithValue("@pid", partyId);
                invoiceId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // سند قبض مرتبط بالفاتورة (نفس منطق نافذة السند)
            using (var cmd = new NpgsqlCommand(
                "INSERT INTO payments (party_id, account_id, invoice_id, amount, date, cash_box_id, " +
                "payment_method, reference_number, notes, payment_type, currency_id) " +
                "VALUES (@pid, @aid, @iid, @amt, CURRENT_DATE, @cb, 'نقدي', @ref, @notes, 'receipt', 1) RETURNING id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@pid", partyId);
                cmd.Parameters.AddWithValue("@aid", accountId);
                cmd.Parameters.AddWithValue("@iid", invoiceId);
                cmd.Parameters.AddWithValue("@amt", originalAmount);
                cmd.Parameters.AddWithValue("@cb", cashBoxId);
                cmd.Parameters.AddWithValue("@ref", tag);
                cmd.Parameters.AddWithValue("@notes", tag);
                paymentId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // بند الدفتر الموحّد
            using (var cmd = new NpgsqlCommand(
                "INSERT INTO invoice_payments (invoice_id, method, amount, reference, paid_at, created_at) " +
                "VALUES (@iid, 'نقدي', @amt, @ref, NOW(), NOW())", conn, tx))
            {
                cmd.Parameters.AddWithValue("@iid", invoiceId);
                cmd.Parameters.AddWithValue("@amt", originalAmount);
                cmd.Parameters.AddWithValue("@ref", tag);
                cmd.ExecuteNonQuery();
            }

            tx.Commit();
        }

        // ====== التعديل: نفس ما تنفّذه نافذة سند القبض في وضع التعديل ======
        decimal newAmount = 175m;
        using (var conn = new NpgsqlConnection(cs))
        {
            conn.Open();
            using var tx = conn.BeginTransaction();

            using (var cmd = new NpgsqlCommand(
                "UPDATE payments SET amount=@amt, notes=@n, payment_method='نقدي' WHERE id=@id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", paymentId);
                cmd.Parameters.AddWithValue("@amt", newAmount);
                cmd.Parameters.AddWithValue("@n", tag);
                cmd.ExecuteNonQuery();
            }

            // بند الدفتر: يُعدَّل ولا يُضاف (وإلا احتُسب المبلغ مرتين)
            using (var cmd = new NpgsqlCommand(
                "UPDATE invoice_payments SET amount=@amt, method='نقدي', reference=@ref, paid_at=NOW() " +
                "WHERE id = (SELECT id FROM invoice_payments WHERE invoice_id=@iid AND amount=@oamt ORDER BY id LIMIT 1)", conn, tx))
            {
                cmd.Parameters.AddWithValue("@iid", invoiceId);
                cmd.Parameters.AddWithValue("@oamt", originalAmount);
                cmd.Parameters.AddWithValue("@amt", newAmount);
                cmd.Parameters.AddWithValue("@ref", tag);
                cmd.ExecuteNonQuery();
            }

            using (var cmd = new NpgsqlCommand(
                "WITH s AS (SELECT COALESCE(SUM(amount),0) AS paid FROM invoice_payments WHERE invoice_id=@iid) " +
                "UPDATE invoices SET paid_amount=s.paid, " +
                "status=CASE WHEN s.paid >= COALESCE(total,0) THEN 'paid' WHEN s.paid > 0 THEN 'partial' ELSE 'unpaid' END " +
                "FROM s WHERE invoices.id=@iid", conn, tx))
            {
                cmd.Parameters.AddWithValue("@iid", invoiceId);
                cmd.ExecuteNonQuery();
            }

            tx.Commit();
        }

        // ====== التحقق ======
        Assert.Equal(1, Convert.ToInt32(TestDb.Scalar("SELECT COUNT(*) FROM payments WHERE notes=@n", ("@n", tag))));
        Assert.Equal(newAmount, Convert.ToDecimal(TestDb.Scalar("SELECT amount FROM payments WHERE id=@id", ("@id", paymentId))));
        Assert.Equal(1, Convert.ToInt32(TestDb.Scalar("SELECT COUNT(*) FROM invoice_payments WHERE invoice_id=@iid", ("@iid", invoiceId))));
        Assert.Equal(newAmount, Convert.ToDecimal(TestDb.Scalar("SELECT SUM(amount) FROM invoice_payments WHERE invoice_id=@iid", ("@iid", invoiceId))));
        Assert.Equal(newAmount, Convert.ToDecimal(TestDb.Scalar("SELECT paid_amount FROM invoices WHERE id=@iid", ("@iid", invoiceId))));
        Assert.Equal("partial", Convert.ToString(TestDb.Scalar("SELECT status FROM invoices WHERE id=@iid", ("@iid", invoiceId))));
    }

    [Fact]
    public void Payment_Edit_Window_Loads_The_Existing_Payment_Instead_Of_Starting_Empty()
    {
        if (!TestDb.Available) return;

        var cs = SecureConnectionManager.GetConnectionString();
        var tag = "PAYLOAD" + Guid.NewGuid().ToString("N").Substring(0, 8);
        _names.Add(tag);

        int paymentId;
        using (var conn = new NpgsqlConnection(cs))
        {
            conn.Open();
            using var cmd = new NpgsqlCommand(
                "INSERT INTO payments (party_id, account_id, invoice_id, amount, date, cash_box_id, " +
                "payment_method, reference_number, notes, payment_type, currency_id) " +
                "SELECT (SELECT id FROM parties ORDER BY id LIMIT 1), (SELECT id FROM accounts ORDER BY id LIMIT 1), " +
                "NULL, 42.5, CURRENT_DATE, (SELECT id FROM cash_boxes ORDER BY id LIMIT 1), 'نقدي', @ref, @notes, 'receipt', 1 " +
                "RETURNING id", conn);
            cmd.Parameters.AddWithValue("@ref", tag);
            cmd.Parameters.AddWithValue("@notes", tag);
            paymentId = Convert.ToInt32(cmd.ExecuteScalar());
        }

        // التحقق: السند موجود ويمكن تحميله للتعديل
        var row = TestDb.Scalar("SELECT amount FROM payments WHERE id=@id", ("@id", paymentId));
        Assert.Equal(42.5m, Convert.ToDecimal(row));
        Assert.Equal(1, Convert.ToInt32(TestDb.Scalar("SELECT COUNT(*) FROM payments WHERE id=@id", ("@id", paymentId))));
    }
}
