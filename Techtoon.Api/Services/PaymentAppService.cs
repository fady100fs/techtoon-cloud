using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Techtoon.Api.DTOs.Common;
using Techtoon.Api.DTOs.Payments;

namespace Techtoon.Api.Services;

public interface IPaymentAppService
{
    Task<PagedResultDto<PaymentDto>> GetPaymentsAsync(string? type, int? partyId, DateTime? from, DateTime? to, int page, int pageSize);
    Task<PaymentDto?> GetPaymentAsync(int id);
    Task<PaymentDto> CreatePaymentAsync(PaymentCreateDto dto, int userId);
    Task<PaymentDto?> UpdatePaymentAsync(int id, PaymentCreateDto dto, int userId);
    Task<bool> DeletePaymentAsync(int id, int userId);
}

public class PaymentAppService : IPaymentAppService
{
    private readonly string _connStr;
    private readonly bool _hasInvoiceId;
    private readonly bool _hasInvoicePayments;
    private readonly bool _hasCurrencyId;

    public PaymentAppService(IConfiguration config)
    {
        _connStr = config.GetConnectionString("DefaultConnection") ?? "";
        using var conn = new NpgsqlConnection(_connStr);
        conn.Open();
        var payCols = LoadColumns(conn, "payments");
        _hasInvoiceId = payCols.Contains("invoice_id");
        _hasCurrencyId = payCols.Contains("currency_id");
        _hasInvoicePayments = TableExists(conn, "invoice_payments");
    }

    private static HashSet<string> LoadColumns(NpgsqlConnection conn, string table)
    {
        var cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var cmd = new NpgsqlCommand("SELECT column_name FROM information_schema.columns WHERE table_name=@t", conn);
        cmd.Parameters.AddWithValue("t", table);
        using var r = cmd.ExecuteReader();
        while (r.Read()) cols.Add(r.GetString(0));
        return cols;
    }

    private static bool TableExists(NpgsqlConnection conn, string table)
    {
        using var cmd = new NpgsqlCommand(
            "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema='public' AND table_name=@t)", conn);
        cmd.Parameters.AddWithValue("t", table);
        return Convert.ToBoolean(cmd.ExecuteScalar());
    }

    private static decimal SafeDecimal(object? v) => v == null || v == DBNull.Value ? 0m : Convert.ToDecimal(v);
    private static int SafeInt(object? v) => v == null || v == DBNull.Value ? 0 : Convert.ToInt32(v);
    private static string SafeString(object? v) => v == null || v == DBNull.Value ? string.Empty : v.ToString() ?? string.Empty;

    // H10: the canonical schema has `reference_number`; `voucher_no` does not exist.
    private string InvoiceIdSelect => _hasInvoiceId ? "p.invoice_id " : "NULL::bigint AS invoice_id ";

    public async Task<PagedResultDto<PaymentDto>> GetPaymentsAsync(string? type, int? partyId, DateTime? from, DateTime? to, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 50;
        var result = new PagedResultDto<PaymentDto> { Page = page, PageSize = pageSize };

        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();

        var where = new List<string>();
        var parms = new List<NpgsqlParameter>();
        if (!string.IsNullOrEmpty(type)) { where.Add("p.payment_type = @t"); parms.Add(new NpgsqlParameter("t", type)); }
        if (partyId.HasValue) { where.Add("p.party_id = @pid"); parms.Add(new NpgsqlParameter("pid", partyId.Value)); }
        if (from.HasValue) { where.Add("p.date >= @from"); parms.Add(new NpgsqlParameter("from", from.Value)); }
        if (to.HasValue) { where.Add("p.date <= @to"); parms.Add(new NpgsqlParameter("to", to.Value)); }
        var whereSql = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";

        using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM payments p " + whereSql, conn))
        {
            foreach (var p in parms) cmd.Parameters.Add(p);
            result.TotalCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        var sql = "SELECT p.id::bigint, p.date, p.party_id, COALESCE(pt.name,'') AS party_name, " +
                  "p.amount::numeric, COALESCE(p.payment_type,'') AS payment_type, COALESCE(p.payment_method,'') AS payment_method, " +
                  "COALESCE(p.reference_number,'') AS reference_number, COALESCE(p.notes,'') AS notes, p.cash_box_id, " +
                  InvoiceIdSelect +
                  "FROM payments p LEFT JOIN parties pt ON p.party_id = pt.id " + whereSql + " " +
                  "ORDER BY p.date DESC, p.id DESC LIMIT @limit OFFSET @offset";

        using (var cmd = new NpgsqlCommand(sql, conn))
        {
            foreach (var p in parms) cmd.Parameters.Add(p);
            cmd.Parameters.AddWithValue("limit", pageSize);
            cmd.Parameters.AddWithValue("offset", (page - 1) * pageSize);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                result.Items.Add(Map(r));
            }
        }
        return result;
    }

    public async Task<PaymentDto?> GetPaymentAsync(int id)
    {
        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        var sql = "SELECT p.id::bigint, p.date, p.party_id, COALESCE(pt.name,'') AS party_name, " +
                  "p.amount::numeric, COALESCE(p.payment_type,'') AS payment_type, COALESCE(p.payment_method,'') AS payment_method, " +
                  "COALESCE(p.reference_number,'') AS reference_number, COALESCE(p.notes,'') AS notes, p.cash_box_id, " +
                  InvoiceIdSelect +
                  "FROM payments p LEFT JOIN parties pt ON p.party_id = pt.id WHERE p.id = @id";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);
        using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        return Map(r);
    }

    private static PaymentDto Map(NpgsqlDataReader r) => new PaymentDto
    {
        Id = SafeInt(r["id"]),
        Date = r.GetDateTime(r.GetOrdinal("date")),
        PartyId = r["party_id"] == DBNull.Value ? null : SafeInt(r["party_id"]),
        PartyName = SafeString(r["party_name"]),
        Amount = SafeDecimal(r["amount"]),
        PaymentType = SafeString(r["payment_type"]),
        PaymentMethod = SafeString(r["payment_method"]),
        VoucherNo = r["reference_number"] == DBNull.Value ? null : SafeString(r["reference_number"]),
        Notes = r["notes"] == DBNull.Value ? null : SafeString(r["notes"]),
        CashBoxId = r["cash_box_id"] == DBNull.Value ? null : SafeInt(r["cash_box_id"]),
        InvoiceId = r["invoice_id"] == DBNull.Value ? null : SafeInt(r["invoice_id"])
    };

    /// <summary>P4: العملة الافتراضية من جدول currencies (مصدر واحد للحقيقة بدل رقم ثابت).</summary>
    private static int DefaultCurrencyId(NpgsqlConnection conn)
    {
        try
        {
            using var cmd = new NpgsqlCommand("SELECT id FROM currencies ORDER BY id LIMIT 1", conn);
            var v = cmd.ExecuteScalar();
            return (v == null || v == DBNull.Value) ? 1 : Convert.ToInt32(v);
        }
        catch { return 1; }
    }

    public async Task<PaymentDto> CreatePaymentAsync(PaymentCreateDto dto, int userId)
    {
        if (dto.Amount <= 0) throw new ArgumentException("Amount must be positive");
        if (string.IsNullOrEmpty(dto.PaymentType)) dto.PaymentType = "receipt";
        if (string.IsNullOrEmpty(dto.PaymentMethod)) dto.PaymentMethod = "cash";
        // P4: payments.cash_box_id و currency_id إلزاميان (NOT NULL) — نتحقق بوضوح بدل 500
        if (!dto.CashBoxId.HasValue || dto.CashBoxId.Value <= 0)
            throw new ArgumentException("cashBoxId is required (the payment must affect a cash box / bank account)");

        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        var cols = new List<string> { "date", "party_id", "amount", "payment_type", "payment_method", "reference_number", "notes", "cash_box_id" };
        var vals = new List<string> { "@date", "@pid", "@amount", "@type", "@method", "@ref", "@notes", "@cb" };
        var parms = new List<NpgsqlParameter>
        {
            new("date", dto.Date),
            new("pid", (object?)dto.PartyId ?? DBNull.Value),
            new("amount", dto.Amount),
            new("type", dto.PaymentType),
            new("method", dto.PaymentMethod),
            new("ref", (object?)dto.VoucherNo ?? DBNull.Value),
            new("notes", (object?)dto.Notes ?? DBNull.Value),
            new("cb", dto.CashBoxId.Value)
        };
        if (_hasCurrencyId) { cols.Add("currency_id"); vals.Add("@cur"); parms.Add(new NpgsqlParameter("cur", DefaultCurrencyId(conn))); }
        if (_hasInvoiceId && dto.InvoiceId.HasValue) { cols.Add("invoice_id"); vals.Add("@iid"); parms.Add(new NpgsqlParameter("iid", dto.InvoiceId.Value)); }

        int newId;
        using (var cmd = new NpgsqlCommand(
            "INSERT INTO payments (" + string.Join(",", cols) + ") VALUES (" + string.Join(",", vals) + ") RETURNING id", conn, tx))
        {
            foreach (var p in parms) cmd.Parameters.Add(p);
            newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        // H10: an API payment linked to an invoice must be visible to invoice balances
        // exactly like a payment entered through the WPF invoice editor.
        if (dto.InvoiceId.HasValue)
        {
            if (_hasInvoicePayments)
            {
                using var cmd = new NpgsqlCommand(
                    "INSERT INTO invoice_payments (invoice_id, method, amount, reference, paid_at, created_at) " +
                    "VALUES (@iid, @method, @amount, @ref, @paidAt, NOW())", conn, tx);
                cmd.Parameters.AddWithValue("iid", dto.InvoiceId.Value);
                cmd.Parameters.AddWithValue("method", dto.PaymentMethod);
                cmd.Parameters.AddWithValue("amount", dto.Amount);
                cmd.Parameters.AddWithValue("ref", (object?)dto.VoucherNo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("paidAt", dto.Date);
                await cmd.ExecuteNonQueryAsync();
            }
            await ReconcileInvoiceAsync(conn, tx, dto.InvoiceId.Value, dto.Amount);
        }

        using (var cmd = new NpgsqlCommand(
            "INSERT INTO audit_log (user_id, action, entity_type, entity_id, details, created_at) VALUES (@uid, @action, 'payment', @eid, @details, NOW())",
            conn, tx))
        {
            cmd.Parameters.AddWithValue("uid", userId);
            cmd.Parameters.AddWithValue("action", "create_payment");
            cmd.Parameters.AddWithValue("eid", newId);
            cmd.Parameters.AddWithValue("details", dto.PaymentType + " of " + dto.Amount);
            await cmd.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();
        return (await GetPaymentAsync(newId))!;
    }

    /// <summary>Update payment + reconcile invoice balance (delta).</summary>
    public async Task<PaymentDto?> UpdatePaymentAsync(int id, PaymentCreateDto dto, int userId)
    {
        if (dto.Amount <= 0) throw new ArgumentException("Amount must be positive");
        // P4: cash_box_id إلزامي (NOT NULL) — نتحقق بوضوح بدل خطأ 500
        if (!dto.CashBoxId.HasValue || dto.CashBoxId.Value <= 0)
            throw new ArgumentException("cashBoxId is required (the payment must affect a cash box / bank account)");
        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        int? oldInvoiceId = null;
        decimal oldAmount = 0m;
        using (var read = new NpgsqlCommand("SELECT amount::numeric, " + (_hasInvoiceId ? "invoice_id" : "NULL::bigint") + " FROM payments WHERE id=@id", conn, tx))
        {
            read.Parameters.AddWithValue("id", id);
            using var r = await read.ExecuteReaderAsync();
            if (!await r.ReadAsync()) { await tx.RollbackAsync(); return null; }
            oldAmount = SafeDecimal(r[0]);
            oldInvoiceId = r[1] == DBNull.Value ? null : SafeInt(r[1]);
        }

        using (var cmd = new NpgsqlCommand(
            "UPDATE payments SET date=@date, party_id=@pid, amount=@amount, payment_type=@type, payment_method=@method, reference_number=@ref, notes=@notes, cash_box_id=@cb" + (_hasInvoiceId ? ", invoice_id=@iid" : "") + " WHERE id=@id", conn, tx))
        {
            cmd.Parameters.AddWithValue("date", dto.Date);
            cmd.Parameters.AddWithValue("pid", (object?)dto.PartyId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("amount", dto.Amount);
            cmd.Parameters.AddWithValue("type", string.IsNullOrEmpty(dto.PaymentType) ? "receipt" : dto.PaymentType);
            cmd.Parameters.AddWithValue("method", string.IsNullOrEmpty(dto.PaymentMethod) ? "cash" : dto.PaymentMethod);
            cmd.Parameters.AddWithValue("ref", (object?)dto.VoucherNo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("notes", (object?)dto.Notes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("cb", dto.CashBoxId.Value);
            if (_hasInvoiceId) cmd.Parameters.AddWithValue("iid", (object?)dto.InvoiceId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        // Reconcile old invoice (remove delta) and new invoice (add delta)
        if (_hasInvoicePayments)
        {
            if (oldInvoiceId.HasValue)
            {
                using var del = new NpgsqlCommand("DELETE FROM invoice_payments WHERE id IN (SELECT id FROM invoice_payments WHERE invoice_id=@iid AND amount=@amt ORDER BY id DESC LIMIT 1)", conn, tx);
                del.Parameters.AddWithValue("iid", oldInvoiceId.Value); del.Parameters.AddWithValue("amt", oldAmount);
                await del.ExecuteNonQueryAsync();
            }
            if (dto.InvoiceId.HasValue)
            {
                using var ins = new NpgsqlCommand("INSERT INTO invoice_payments (invoice_id, method, amount, reference, paid_at, created_at) VALUES (@iid,@method,@amount,@ref,@paidAt,NOW())", conn, tx);
                ins.Parameters.AddWithValue("iid", dto.InvoiceId.Value); ins.Parameters.AddWithValue("method", dto.PaymentMethod);
                ins.Parameters.AddWithValue("amount", dto.Amount); ins.Parameters.AddWithValue("ref", (object?)dto.VoucherNo ?? DBNull.Value);
                ins.Parameters.AddWithValue("paidAt", dto.Date);
                await ins.ExecuteNonQueryAsync();
            }
        }
        if (oldInvoiceId.HasValue) await ReconcileInvoiceAsync(conn, tx, oldInvoiceId.Value, -oldAmount);
        if (dto.InvoiceId.HasValue && dto.InvoiceId != oldInvoiceId) await ReconcileInvoiceAsync(conn, tx, dto.InvoiceId.Value, dto.Amount);
        else if (dto.InvoiceId.HasValue && dto.InvoiceId == oldInvoiceId) await ReconcileInvoiceAsync(conn, tx, dto.InvoiceId.Value, dto.Amount - oldAmount);

        using (var log = new NpgsqlCommand("INSERT INTO audit_log (user_id, action, entity_type, entity_id, details, created_at) VALUES (@uid, 'update_payment', 'payment', @eid, @details, NOW())", conn, tx))
        {
            log.Parameters.AddWithValue("uid", userId); log.Parameters.AddWithValue("eid", id);
            log.Parameters.AddWithValue("details", "Updated payment id " + id);
            await log.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();
        return await GetPaymentAsync(id);
    }

    public async Task<bool> DeletePaymentAsync(int id, int userId)
    {
        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        // H3: capture the invoice link before deleting so the invoice balance can be repaired.
        int? invoiceId = null;
        decimal amount = 0m;
        using (var read = new NpgsqlCommand(
            "SELECT amount::numeric, " + (_hasInvoiceId ? "invoice_id" : "NULL::bigint") + " FROM payments WHERE id = @id", conn, tx))
        {
            read.Parameters.AddWithValue("id", id);
            using var r = await read.ExecuteReaderAsync();
            if (!await r.ReadAsync()) { await tx.RollbackAsync(); return false; }
            amount = SafeDecimal(r[0]);
            invoiceId = r[1] == DBNull.Value ? null : SafeInt(r[1]);
        }

        using (var cmd = new NpgsqlCommand("DELETE FROM payments WHERE id = @id", conn, tx))
        {
            cmd.Parameters.AddWithValue("id", id);
            var affected = await cmd.ExecuteNonQueryAsync();
            if (affected == 0) { await tx.RollbackAsync(); return false; }
        }

        if (invoiceId.HasValue)
        {
            if (_hasInvoicePayments)
            {
                // Remove one matching ledger row (best effort: amounts are decimals in the UI).
                using var cmd = new NpgsqlCommand(
                    "DELETE FROM invoice_payments WHERE id IN (" +
                    "  SELECT id FROM invoice_payments WHERE invoice_id=@iid AND amount=@amt ORDER BY id DESC LIMIT 1)", conn, tx);
                cmd.Parameters.AddWithValue("iid", invoiceId.Value);
                cmd.Parameters.AddWithValue("amt", amount);
                await cmd.ExecuteNonQueryAsync();
            }
            await ReconcileInvoiceAsync(conn, tx, invoiceId.Value, -amount);
        }

        using var cmd2 = new NpgsqlCommand(
            "INSERT INTO audit_log (user_id, action, entity_type, entity_id, details, created_at) VALUES (@uid, 'delete_payment', 'payment', @eid, @details, NOW())",
            conn, tx);
        cmd2.Parameters.AddWithValue("uid", userId);
        cmd2.Parameters.AddWithValue("eid", id);
        cmd2.Parameters.AddWithValue("details", "Deleted payment id " + id);
        await cmd2.ExecuteNonQueryAsync();

        await tx.CommitAsync();
        return true;
    }

    /// <summary>
    /// H3/H10: recompute invoices.paid_amount and status from the authoritative ledger.
    /// When no ledger table exists the stored balance is adjusted by <paramref name="delta"/>.
    /// </summary>
    private async Task ReconcileInvoiceAsync(NpgsqlConnection conn, NpgsqlTransaction tx, int invoiceId, decimal delta)
    {
        decimal paid;
        if (_hasInvoicePayments)
        {
            using var cmd = new NpgsqlCommand("SELECT COALESCE(SUM(amount),0) FROM invoice_payments WHERE invoice_id = @id", conn, tx);
            cmd.Parameters.AddWithValue("id", invoiceId);
            paid = Convert.ToDecimal(await cmd.ExecuteScalarAsync());
        }
        else if (_hasInvoiceId)
        {
            using var cmd = new NpgsqlCommand("SELECT COALESCE(SUM(amount),0) FROM payments WHERE invoice_id = @id", conn, tx);
            cmd.Parameters.AddWithValue("id", invoiceId);
            paid = Convert.ToDecimal(await cmd.ExecuteScalarAsync());
        }
        else
        {
            using var cmd = new NpgsqlCommand("SELECT GREATEST(COALESCE(paid_amount,0) + @delta, 0) FROM invoices WHERE id = @id", conn, tx);
            cmd.Parameters.AddWithValue("delta", delta);
            cmd.Parameters.AddWithValue("id", invoiceId);
            var v = await cmd.ExecuteScalarAsync();
            if (v == null || v == DBNull.Value) return;
            paid = Convert.ToDecimal(v);
        }

        using (var cmd = new NpgsqlCommand(
            "UPDATE invoices SET paid_amount = @paid, " +
            "status = CASE WHEN @paid >= COALESCE(total,0) THEN 'paid' WHEN @paid > 0 THEN 'partial' ELSE 'unpaid' END, " +
            "updated_at = NOW() WHERE id = @id AND COALESCE(status,'') <> 'cancelled'", conn, tx))
        {
            cmd.Parameters.AddWithValue("paid", paid);
            cmd.Parameters.AddWithValue("id", invoiceId);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
