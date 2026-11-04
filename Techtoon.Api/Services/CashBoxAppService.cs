using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Techtoon.Api.DTOs.CashBoxes;

namespace Techtoon.Api.Services;

public interface ICashBoxAppService
{
    Task<List<CashBoxDto>> GetCashBoxesAsync();
    Task<CashBoxDto?> GetCashBoxAsync(int id);
    Task<CashBoxDto> CreateCashBoxAsync(CashBoxCreateDto dto, int userId);
}

public class CashBoxAppService : ICashBoxAppService
{
    private readonly string _connStr;
    private readonly HashSet<string> _cols;

    public CashBoxAppService(IConfiguration config)
    {
        _connStr = config.GetConnectionString("DefaultConnection") ?? "";
        using var conn = new NpgsqlConnection(_connStr);
        conn.Open();
        _cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var cmd = new NpgsqlCommand("SELECT column_name FROM information_schema.columns WHERE table_name='cash_boxes'", conn);
        using var r = cmd.ExecuteReader();
        while (r.Read()) _cols.Add(r.GetString(0));
    }

    private bool Has(string col) => _cols.Contains(col);

    private static decimal SafeDecimal(object? v) => v == null || v == DBNull.Value ? 0m : Convert.ToDecimal(v);
    private static int SafeInt(object? v) => v == null || v == DBNull.Value ? 0 : Convert.ToInt32(v);
    private static string SafeString(object? v) => v == null || v == DBNull.Value ? string.Empty : v.ToString() ?? string.Empty;

    private string BuildBalanceExpr()
    {
        var opening = Has("opening_balance") ? "COALESCE(c.opening_balance,0)::numeric" : "0::numeric";
        return opening + " + " +
               "COALESCE((SELECT SUM(amount) FROM payments WHERE cash_box_id = c.id AND payment_type='receipt'), 0)::numeric - " +
               "COALESCE((SELECT SUM(amount) FROM payments WHERE cash_box_id = c.id AND payment_type='payment'), 0)::numeric - " +
               "COALESCE((SELECT SUM(amount) FROM expenses WHERE cash_box_id = c.id), 0)::numeric";
    }

    private string BuildSelectColumns()
    {
        return "c.id::bigint, c.name, " +
               (Has("opening_balance") ? "COALESCE(c.opening_balance,0)::numeric AS opening_balance, " : "0::numeric AS opening_balance, ") +
               (Has("currency") ? "COALESCE(c.currency,'EGP') AS currency, " : "'EGP' AS currency, ") +
               BuildBalanceExpr() + " AS current_balance";
    }

    public async Task<List<CashBoxDto>> GetCashBoxesAsync()
    {
        var list = new List<CashBoxDto>();
        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        var sql = "SELECT " + BuildSelectColumns() + " FROM cash_boxes c ORDER BY c.name";
        using var cmd = new NpgsqlCommand(sql, conn);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new CashBoxDto
            {
                Id = SafeInt(r["id"]),
                Name = SafeString(r["name"]),
                OpeningBalance = SafeDecimal(r["opening_balance"]),
                CurrentBalance = SafeDecimal(r["current_balance"]),
                Currency = SafeString(r["currency"])
            });
        }
        return list;
    }

    public async Task<CashBoxDto?> GetCashBoxAsync(int id)
    {
        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        var sql = "SELECT " + BuildSelectColumns() + " FROM cash_boxes c WHERE c.id = @id";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);
        using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        return new CashBoxDto
        {
            Id = SafeInt(r["id"]),
            Name = SafeString(r["name"]),
            OpeningBalance = SafeDecimal(r["opening_balance"]),
            CurrentBalance = SafeDecimal(r["current_balance"]),
            Currency = SafeString(r["currency"])
        };
    }

    public async Task<CashBoxDto> CreateCashBoxAsync(CashBoxCreateDto dto, int userId)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Cash box name is required");

        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        var cols = new List<string> { "name" };
        var vals = new List<string> { "@name" };
        var parms = new List<NpgsqlParameter> { new NpgsqlParameter("name", dto.Name) };

        if (Has("opening_balance")) { cols.Add("opening_balance"); vals.Add("@opening"); parms.Add(new NpgsqlParameter("opening", dto.OpeningBalance)); }
        if (Has("currency")) { cols.Add("currency"); vals.Add("@cur"); parms.Add(new NpgsqlParameter("cur", dto.Currency)); }
        if (Has("is_active")) { cols.Add("is_active"); vals.Add("TRUE"); }
        if (Has("created_at")) { cols.Add("created_at"); vals.Add("NOW()"); }

        int newId;
        using (var cmd = new NpgsqlCommand(
            "INSERT INTO cash_boxes (" + string.Join(",", cols) + ") VALUES (" + string.Join(",", vals) + ") RETURNING id", conn, tx))
        {
            foreach (var p in parms) cmd.Parameters.Add(p);
            newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        using (var cmd = new NpgsqlCommand(
            "INSERT INTO audit_log (user_id, action, entity_type, entity_id, details, created_at) VALUES (@uid, 'create_cashbox', 'cashbox', @eid, @details, NOW())",
            conn, tx))
        {
            cmd.Parameters.AddWithValue("uid", userId);
            cmd.Parameters.AddWithValue("eid", newId);
            cmd.Parameters.AddWithValue("details", "Created cashbox: " + dto.Name);
            await cmd.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();
        return (await GetCashBoxAsync(newId))!;
    }
}