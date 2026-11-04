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
    Task<bool> DeletePaymentAsync(int id, int userId);
}

public class PaymentAppService : IPaymentAppService
{
    private readonly string _connStr;
    public PaymentAppService(IConfiguration config) { _connStr = config.GetConnectionString("DefaultConnection") ?? ""; }

    private static decimal SafeDecimal(object? v) => v == null || v == DBNull.Value ? 0m : Convert.ToDecimal(v);
    private static int SafeInt(object? v) => v == null || v == DBNull.Value ? 0 : Convert.ToInt32(v);
    private static string SafeString(object? v) => v == null || v == DBNull.Value ? string.Empty : v.ToString() ?? string.Empty;

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
                  "COALESCE(p.voucher_no,'') AS voucher_no, COALESCE(p.notes,'') AS notes, p.cash_box_id " +
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
                result.Items.Add(new PaymentDto
                {
                    Id = SafeInt(r["id"]),
                    Date = r.GetDateTime(r.GetOrdinal("date")),
                    PartyId = r["party_id"] == DBNull.Value ? null : SafeInt(r["party_id"]),
                    PartyName = SafeString(r["party_name"]),
                    Amount = SafeDecimal(r["amount"]),
                    PaymentType = SafeString(r["payment_type"]),
                    PaymentMethod = SafeString(r["payment_method"]),
                    VoucherNo = r["voucher_no"] == DBNull.Value ? null : SafeString(r["voucher_no"]),
                    Notes = r["notes"] == DBNull.Value ? null : SafeString(r["notes"]),
                    CashBoxId = r["cash_box_id"] == DBNull.Value ? null : SafeInt(r["cash_box_id"])
                });
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
                  "COALESCE(p.voucher_no,'') AS voucher_no, COALESCE(p.notes,'') AS notes, p.cash_box_id " +
                  "FROM payments p LEFT JOIN parties pt ON p.party_id = pt.id WHERE p.id = @id";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);
        using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        return new PaymentDto
        {
            Id = SafeInt(r["id"]),
            Date = r.GetDateTime(r.GetOrdinal("date")),
            PartyId = r["party_id"] == DBNull.Value ? null : SafeInt(r["party_id"]),
            PartyName = SafeString(r["party_name"]),
            Amount = SafeDecimal(r["amount"]),
            PaymentType = SafeString(r["payment_type"]),
            PaymentMethod = SafeString(r["payment_method"]),
            VoucherNo = r["voucher_no"] == DBNull.Value ? null : SafeString(r["voucher_no"]),
            Notes = r["notes"] == DBNull.Value ? null : SafeString(r["notes"]),
            CashBoxId = r["cash_box_id"] == DBNull.Value ? null : SafeInt(r["cash_box_id"])
        };
    }

    public async Task<PaymentDto> CreatePaymentAsync(PaymentCreateDto dto, int userId)
    {
        if (dto.Amount <= 0) throw new ArgumentException("Amount must be positive");
        if (string.IsNullOrEmpty(dto.PaymentType)) dto.PaymentType = "receipt";
        if (string.IsNullOrEmpty(dto.PaymentMethod)) dto.PaymentMethod = "cash";

        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        int newId;
        using (var cmd = new NpgsqlCommand(
            "INSERT INTO payments (date, party_id, amount, payment_type, payment_method, voucher_no, notes, cash_box_id) " +
            "VALUES (@date, @pid, @amount, @type, @method, @vno, @notes, @cb) RETURNING id", conn, tx))
        {
            cmd.Parameters.AddWithValue("date", dto.Date);
            cmd.Parameters.AddWithValue("pid", (object?)dto.PartyId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("amount", dto.Amount);
            cmd.Parameters.AddWithValue("type", dto.PaymentType);
            cmd.Parameters.AddWithValue("method", dto.PaymentMethod);
            cmd.Parameters.AddWithValue("vno", (object?)dto.VoucherNo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("notes", (object?)dto.Notes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("cb", (object?)dto.CashBoxId ?? DBNull.Value);
            newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
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

    public async Task<bool> DeletePaymentAsync(int id, int userId)
    {
        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();
        using var cmd = new NpgsqlCommand("DELETE FROM payments WHERE id = @id", conn, tx);
        cmd.Parameters.AddWithValue("id", id);
        var affected = await cmd.ExecuteNonQueryAsync();
        if (affected == 0) { await tx.RollbackAsync(); return false; }

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
}