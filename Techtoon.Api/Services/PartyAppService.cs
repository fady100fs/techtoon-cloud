using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Techtoon.Api.DTOs.Common;
using Techtoon.Api.DTOs.Parties;

namespace Techtoon.Api.Services;

public interface IPartyAppService
{
    Task<PagedResultDto<PartyListDto>> GetPartiesAsync(string? search, string? type, int page, int pageSize);
    Task<PartyDetailDto?> GetPartyAsync(int id);
    Task<PartyDetailDto> CreatePartyAsync(PartyCreateDto dto, int userId);
    Task<PartyDetailDto?> UpdatePartyAsync(int id, PartyUpdateDto dto, int userId);
    Task<bool> DeletePartyAsync(int id, int userId);
    Task<PartyStatementDto> GetStatementAsync(int partyId, DateTime? from, DateTime? to);
}

public class PartyAppService : IPartyAppService
{
    private readonly string _connStr;

    public PartyAppService(IConfiguration config)
    {
        _connStr = config.GetConnectionString("DefaultConnection") ?? "";
    }

    private static decimal SafeDecimal(object? v)
    {
        if (v == null || v == DBNull.Value) return 0m;
        return Convert.ToDecimal(v);
    }

    private static int SafeInt(object? v)
    {
        if (v == null || v == DBNull.Value) return 0;
        return Convert.ToInt32(v);
    }

    private static string SafeString(object? v)
    {
        if (v == null || v == DBNull.Value) return string.Empty;
        return v.ToString() ?? string.Empty;
    }

    public async Task<PagedResultDto<PartyListDto>> GetPartiesAsync(string? search, string? type, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 50;

        var result = new PagedResultDto<PartyListDto> { Page = page, PageSize = pageSize };
        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();

        var where = new List<string>();
        var parms = new List<NpgsqlParameter>();
        if (!string.IsNullOrEmpty(search))
        {
            where.Add("(p.name ILIKE @s OR p.phone ILIKE @s)");
            parms.Add(new NpgsqlParameter("s", "%" + search + "%"));
        }
        if (!string.IsNullOrEmpty(type))
        {
            where.Add("p.type = @t");
            parms.Add(new NpgsqlParameter("t", type));
        }
        var whereSql = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";

        using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM parties p " + whereSql, conn))
        {
            foreach (var p in parms) cmd.Parameters.Add(p);
            result.TotalCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        var sql = "SELECT p.id::bigint, p.name, COALESCE(p.type,'') AS type, COALESCE(p.phone,'') AS phone, " +
                  "COALESCE(p.opening_balance,0)::numeric AS opening_balance, " +
                  "COALESCE((SELECT SUM(amount) FROM payments WHERE party_id = p.id AND payment_type='receipt'), 0)::numeric - " +
                  "COALESCE((SELECT SUM(amount) FROM payments WHERE party_id = p.id AND payment_type='payment'), 0)::numeric + " +
                  "COALESCE((SELECT SUM(total) FROM invoices WHERE party_id = p.id AND type='sales'), 0)::numeric - " +
                  "COALESCE((SELECT SUM(total) FROM invoices WHERE party_id = p.id AND type='purchase'), 0)::numeric AS current_balance " +
                  "FROM parties p " + whereSql + " ORDER BY p.name LIMIT @limit OFFSET @offset";

        using (var cmd = new NpgsqlCommand(sql, conn))
        {
            foreach (var p in parms) cmd.Parameters.Add(p);
            cmd.Parameters.AddWithValue("limit", pageSize);
            cmd.Parameters.AddWithValue("offset", (page - 1) * pageSize);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                result.Items.Add(new PartyListDto
                {
                    Id = SafeInt(r["id"]),
                    Name = SafeString(r["name"]),
                    Type = r["type"] == DBNull.Value ? null : SafeString(r["type"]),
                    Phone = r["phone"] == DBNull.Value ? null : SafeString(r["phone"]),
                    OpeningBalance = SafeDecimal(r["opening_balance"]),
                    CurrentBalance = SafeDecimal(r["current_balance"])
                });
            }
        }
        return result;
    }

    public async Task<PartyDetailDto?> GetPartyAsync(int id)
    {
        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        var sql = "SELECT p.id::bigint, p.name, COALESCE(p.type,'') AS type, COALESCE(p.phone,'') AS phone, " +
                  "COALESCE(p.email,'') AS email, COALESCE(p.address,'') AS address, COALESCE(p.tax_number,'') AS tax_number, " +
                  "COALESCE(p.opening_balance,0)::numeric AS opening_balance, " +
                  "COALESCE((SELECT SUM(amount) FROM payments WHERE party_id = p.id AND payment_type='receipt'), 0)::numeric - " +
                  "COALESCE((SELECT SUM(amount) FROM payments WHERE party_id = p.id AND payment_type='payment'), 0)::numeric + " +
                  "COALESCE((SELECT SUM(total) FROM invoices WHERE party_id = p.id AND type='sales'), 0)::numeric - " +
                  "COALESCE((SELECT SUM(total) FROM invoices WHERE party_id = p.id AND type='purchase'), 0)::numeric AS current_balance " +
                  "FROM parties p WHERE p.id = @id";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);
        using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;

        return new PartyDetailDto
        {
            Id = SafeInt(r["id"]),
            Name = SafeString(r["name"]),
            Type = r["type"] == DBNull.Value ? null : SafeString(r["type"]),
            Phone = r["phone"] == DBNull.Value ? null : SafeString(r["phone"]),
            Email = r["email"] == DBNull.Value ? null : SafeString(r["email"]),
            Address = r["address"] == DBNull.Value ? null : SafeString(r["address"]),
            TaxNumber = r["tax_number"] == DBNull.Value ? null : SafeString(r["tax_number"]),
            OpeningBalance = SafeDecimal(r["opening_balance"]),
            CurrentBalance = SafeDecimal(r["current_balance"])
        };
    }

    public async Task<PartyDetailDto> CreatePartyAsync(PartyCreateDto dto, int userId)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Party name is required");
        if (string.IsNullOrEmpty(dto.Type))
            dto.Type = "customer";

        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        int newId;
        using (var cmd = new NpgsqlCommand(
            "INSERT INTO parties (name, type, phone, email, address, tax_number, opening_balance) " +
            "VALUES (@name, @type, @phone, @email, @address, @tax, @opening) RETURNING id", conn, tx))
        {
            cmd.Parameters.AddWithValue("name", dto.Name);
            cmd.Parameters.AddWithValue("type", dto.Type);
            cmd.Parameters.AddWithValue("phone", (object?)dto.Phone ?? DBNull.Value);
            cmd.Parameters.AddWithValue("email", (object?)dto.Email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("address", (object?)dto.Address ?? DBNull.Value);
            cmd.Parameters.AddWithValue("tax", (object?)dto.TaxNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("opening", dto.OpeningBalance);
            newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        await LogAuditAsync(conn, tx, userId, "create_party", "party", newId, "Created party: " + dto.Name + " (" + dto.Type + ")");
        await tx.CommitAsync();

        return (await GetPartyAsync(newId))!;
    }

    public async Task<PartyDetailDto?> UpdatePartyAsync(int id, PartyUpdateDto dto, int userId)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Party name is required");

        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        using (var cmd = new NpgsqlCommand(
            "UPDATE parties SET name=@name, type=@type, phone=@phone, email=@email, address=@address, tax_number=@tax WHERE id=@id", conn, tx))
        {
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("name", dto.Name);
            cmd.Parameters.AddWithValue("type", (object?)dto.Type ?? "customer");
            cmd.Parameters.AddWithValue("phone", (object?)dto.Phone ?? DBNull.Value);
            cmd.Parameters.AddWithValue("email", (object?)dto.Email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("address", (object?)dto.Address ?? DBNull.Value);
            cmd.Parameters.AddWithValue("tax", (object?)dto.TaxNumber ?? DBNull.Value);
            var affected = await cmd.ExecuteNonQueryAsync();
            if (affected == 0) { await tx.RollbackAsync(); return null; }
        }

        await LogAuditAsync(conn, tx, userId, "update_party", "party", id, "Updated party: " + dto.Name);
        await tx.CommitAsync();
        return await GetPartyAsync(id);
    }

    public async Task<bool> DeletePartyAsync(int id, int userId)
    {
        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM invoices WHERE party_id=@id", conn, tx))
        {
            cmd.Parameters.AddWithValue("id", id);
            var invCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            if (invCount > 0)
                throw new InvalidOperationException("Cannot delete party with existing invoices.");
        }

        using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM payments WHERE party_id=@id", conn, tx))
        {
            cmd.Parameters.AddWithValue("id", id);
            var payCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            if (payCount > 0)
                throw new InvalidOperationException("Cannot delete party with existing payments.");
        }

        using (var cmd = new NpgsqlCommand("DELETE FROM parties WHERE id=@id", conn, tx))
        {
            cmd.Parameters.AddWithValue("id", id);
            var affected = await cmd.ExecuteNonQueryAsync();
            if (affected == 0) { await tx.RollbackAsync(); return false; }
        }

        await LogAuditAsync(conn, tx, userId, "delete_party", "party", id, "Deleted party id " + id);
        await tx.CommitAsync();
        return true;
    }

    public async Task<PartyStatementDto> GetStatementAsync(int partyId, DateTime? from, DateTime? to)
    {
        var party = await GetPartyAsync(partyId);
        if (party == null) throw new ArgumentException("Party not found");

        var statement = new PartyStatementDto
        {
            PartyId = partyId,
            PartyName = party.Name,
            OpeningBalance = party.OpeningBalance,
            CurrentBalance = party.CurrentBalance
        };

        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();

        var dateFilter = "";
        var parms = new List<NpgsqlParameter> { new NpgsqlParameter("pid", partyId) };
        if (from.HasValue)
        {
            dateFilter += " AND date >= @from";
            parms.Add(new NpgsqlParameter("from", from.Value));
        }
        if (to.HasValue)
        {
            dateFilter += " AND date <= @to";
            parms.Add(new NpgsqlParameter("to", to.Value));
        }

        // Invoices (sales = debit for customer, purchase = credit for supplier)
        var sqlInv = "SELECT date, type, invoice_number, total::numeric, COALESCE(notes,'') FROM invoices WHERE party_id=@pid" + dateFilter + " ORDER BY date";
        using (var cmd = new NpgsqlCommand(sqlInv, conn))
        {
            foreach (var p in parms) cmd.Parameters.Add(p);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                var invType = SafeString(r["type"]);
                var total = SafeDecimal(r["total"]);
                statement.Entries.Add(new StatementEntryDto
                {
                    Date = r.GetDateTime(0),
                    EntryType = "invoice_" + invType,
                    Reference = "INV-" + SafeString(r["invoice_number"]),
                    Debit = invType == "sales" ? total : 0,
                    Credit = invType == "purchase" ? total : 0,
                    Notes = SafeString(r["notes"])
                });
            }
        }

        // Payments (receipt = credit for customer, payment = debit for supplier)
        var sqlPay = "SELECT date, payment_type, COALESCE(voucher_no,''), amount::numeric, COALESCE(notes,'') FROM payments WHERE party_id=@pid" + dateFilter + " ORDER BY date";
        using (var cmd = new NpgsqlCommand(sqlPay, conn))
        {
            foreach (var p in parms) cmd.Parameters.Add(p);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                var payType = SafeString(r["payment_type"]);
                var amount = SafeDecimal(r["amount"]);
                statement.Entries.Add(new StatementEntryDto
                {
                    Date = r.GetDateTime(0),
                    EntryType = "payment_" + payType,
                    Reference = "PAY-" + SafeString(r["voucher_no"]),
                    Debit = payType == "payment" ? amount : 0,
                    Credit = payType == "receipt" ? amount : 0,
                    Notes = SafeString(r["notes"])
                });
            }
        }

        statement.Entries.Sort((a, b) => a.Date.CompareTo(b.Date));
        return statement;
    }

    private async Task LogAuditAsync(NpgsqlConnection conn, NpgsqlTransaction tx, int userId, string action, string entityType, int entityId, string details)
    {
        using var cmd = new NpgsqlCommand(
            "INSERT INTO audit_log (user_id, action, entity_type, entity_id, details, created_at) VALUES (@uid, @action, @etype, @eid, @details, NOW())",
            conn, tx);
        cmd.Parameters.AddWithValue("uid", userId);
        cmd.Parameters.AddWithValue("action", action);
        cmd.Parameters.AddWithValue("etype", entityType);
        cmd.Parameters.AddWithValue("eid", entityId);
        cmd.Parameters.AddWithValue("details", details);
        await cmd.ExecuteNonQueryAsync();
    }
}