using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Techtoon.Api.DTOs.Common;
using Techtoon.Api.DTOs.Expenses;

namespace Techtoon.Api.Services;

public interface IExpenseAppService
{
    Task<PagedResultDto<ExpenseDto>> GetExpensesAsync(DateTime? from, DateTime? to, int? categoryId, int page, int pageSize);
    Task<ExpenseDto?> GetExpenseAsync(int id);
    Task<ExpenseDto> CreateExpenseAsync(ExpenseCreateDto dto, int userId);
    Task<ExpenseDto?> UpdateExpenseAsync(int id, ExpenseCreateDto dto, int userId);
    Task<bool> DeleteExpenseAsync(int id, int userId);
}

public class ExpenseAppService : IExpenseAppService
{
    private readonly string _connStr;
    public ExpenseAppService(IConfiguration config) { _connStr = config.GetConnectionString("DefaultConnection") ?? ""; }

    private static decimal SafeDecimal(object? v) => v == null || v == DBNull.Value ? 0m : Convert.ToDecimal(v);
    private static int SafeInt(object? v) => v == null || v == DBNull.Value ? 0 : Convert.ToInt32(v);
    private static string SafeString(object? v) => v == null || v == DBNull.Value ? string.Empty : v.ToString() ?? string.Empty;

    public async Task<PagedResultDto<ExpenseDto>> GetExpensesAsync(DateTime? from, DateTime? to, int? categoryId, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 50;
        var result = new PagedResultDto<ExpenseDto> { Page = page, PageSize = pageSize };

        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();

        var where = new List<string>();
        var parms = new List<NpgsqlParameter>();
        if (from.HasValue) { where.Add("e.date >= @from"); parms.Add(new NpgsqlParameter("from", from.Value)); }
        if (to.HasValue) { where.Add("e.date <= @to"); parms.Add(new NpgsqlParameter("to", to.Value)); }
        if (categoryId.HasValue) { where.Add("e.category_id = @cid"); parms.Add(new NpgsqlParameter("cid", categoryId.Value)); }
        var whereSql = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";

        using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM expenses e " + whereSql, conn))
        {
            foreach (var p in parms) cmd.Parameters.AddClones(parms);
            result.TotalCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        var sql = "SELECT e.id::bigint, e.date, e.category_id, e.amount::numeric, COALESCE(e.description,'') AS description, " +
                  "COALESCE(e.payment_method,'') AS payment_method, e.cash_box_id, COALESCE(e.notes,'') AS notes " +
                  "FROM expenses e " + whereSql + " ORDER BY e.date DESC, e.id DESC LIMIT @limit OFFSET @offset";

        using (var cmd = new NpgsqlCommand(sql, conn))
        {
            foreach (var p in parms) cmd.Parameters.AddClones(parms);
            cmd.Parameters.AddWithValue("limit", pageSize);
            cmd.Parameters.AddWithValue("offset", (page - 1) * pageSize);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                result.Items.Add(new ExpenseDto
                {
                    Id = SafeInt(r["id"]),
                    Date = r.GetDateTime(r.GetOrdinal("date")),
                    CategoryId = r["category_id"] == DBNull.Value ? null : SafeInt(r["category_id"]),
                    Amount = SafeDecimal(r["amount"]),
                    Description = r["description"] == DBNull.Value ? null : SafeString(r["description"]),
                    PaymentMethod = SafeString(r["payment_method"]),
                    CashBoxId = r["cash_box_id"] == DBNull.Value ? null : SafeInt(r["cash_box_id"]),
                    Notes = r["notes"] == DBNull.Value ? null : SafeString(r["notes"])
                });
            }
        }
        return result;
    }

    public async Task<ExpenseDto?> GetExpenseAsync(int id)
    {
        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        var sql = "SELECT e.id::bigint, e.date, e.category_id, e.amount::numeric, COALESCE(e.description,'') AS description, " +
                  "COALESCE(e.payment_method,'') AS payment_method, e.cash_box_id, COALESCE(e.notes,'') AS notes " +
                  "FROM expenses e WHERE e.id = @id";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);
        using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        return new ExpenseDto
        {
            Id = SafeInt(r["id"]),
            Date = r.GetDateTime(r.GetOrdinal("date")),
            CategoryId = r["category_id"] == DBNull.Value ? null : SafeInt(r["category_id"]),
            Amount = SafeDecimal(r["amount"]),
            Description = r["description"] == DBNull.Value ? null : SafeString(r["description"]),
            PaymentMethod = SafeString(r["payment_method"]),
            CashBoxId = r["cash_box_id"] == DBNull.Value ? null : SafeInt(r["cash_box_id"]),
            Notes = r["notes"] == DBNull.Value ? null : SafeString(r["notes"])
        };
    }

    public async Task<ExpenseDto> CreateExpenseAsync(ExpenseCreateDto dto, int userId)
    {
        if (dto.Amount <= 0) throw new ArgumentException("Amount must be positive");

        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        // category_id و cash_box_id إلزاميان في قاعدة البيانات.
        // إن لم يحدّدهما العميل نختار أول تصنيف/خزينة متاح (لا نخترع قيمة).
        int categoryId = dto.CategoryId ?? await ResolveFirstIdAsync(conn, tx, "expense_categories");
        if (categoryId <= 0)
            throw new ArgumentException("categoryId is required — no expense category is available");
        int cashBoxId = dto.CashBoxId ?? await ResolveFirstIdAsync(conn, tx, "cash_boxes");
        if (cashBoxId <= 0)
            throw new ArgumentException("cashBoxId is required — no cash box is available");

        int newId;
        using (var cmd = new NpgsqlCommand(
            "INSERT INTO expenses (date, category_id, amount, description, payment_method, cash_box_id, notes, created_by) " +
            "VALUES (@date, @cat, @amount, @desc, @method, @cb, @notes, @uid) RETURNING id", conn, tx))
        {
            cmd.Parameters.AddWithValue("date", dto.Date);
            cmd.Parameters.AddWithValue("cat", categoryId);
            cmd.Parameters.AddWithValue("amount", dto.Amount);
            cmd.Parameters.AddWithValue("desc", (object?)dto.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("method", dto.PaymentMethod);
            cmd.Parameters.AddWithValue("cb", cashBoxId);
            cmd.Parameters.AddWithValue("notes", (object?)dto.Notes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("uid", userId);
            newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        using (var cmd = new NpgsqlCommand(
            "INSERT INTO audit_log (user_id, action, entity_type, entity_id, details, created_at) VALUES (@uid, 'create_expense', 'expense', @eid, @details, NOW())",
            conn, tx))
        {
            cmd.Parameters.AddWithValue("uid", userId);
            cmd.Parameters.AddWithValue("eid", newId);
            cmd.Parameters.AddWithValue("details", "Expense of " + dto.Amount);
            await cmd.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();
        return (await GetExpenseAsync(newId))!;
    }

    /// <summary>أول معرّف متاح في جدول معيّن (0 إذا كان الجدول فارغًا).</summary>
    private static async Task<int> ResolveFirstIdAsync(NpgsqlConnection conn, NpgsqlTransaction tx, string table)
    {
        using var cmd = new NpgsqlCommand($"SELECT id FROM {table} ORDER BY id LIMIT 1", conn, tx);
        var v = await cmd.ExecuteScalarAsync();
        return v == null || v == DBNull.Value ? 0 : Convert.ToInt32(v);
    }

    /// <summary>يعدّل مصروفًا قائمًا (PUT).</summary>
    public async Task<ExpenseDto?> UpdateExpenseAsync(int id, ExpenseCreateDto dto, int userId)
    {
        if (dto.Amount <= 0) throw new ArgumentException("Amount must be positive");

        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        int categoryId = dto.CategoryId ?? await ResolveFirstIdAsync(conn, tx, "expense_categories");
        if (categoryId <= 0)
            throw new ArgumentException("categoryId is required — no expense category is available");
        int cashBoxId = dto.CashBoxId ?? await ResolveFirstIdAsync(conn, tx, "cash_boxes");
        if (cashBoxId <= 0)
            throw new ArgumentException("cashBoxId is required — no cash box is available");

        using (var cmd = new NpgsqlCommand(
            "UPDATE expenses SET date=@date, category_id=@cat, amount=@amount, description=@desc, " +
            "payment_method=@method, cash_box_id=@cb, notes=@notes WHERE id=@id", conn, tx))
        {
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("date", dto.Date);
            cmd.Parameters.AddWithValue("cat", categoryId);
            cmd.Parameters.AddWithValue("amount", dto.Amount);
            cmd.Parameters.AddWithValue("desc", (object?)dto.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("method", dto.PaymentMethod);
            cmd.Parameters.AddWithValue("cb", cashBoxId);
            cmd.Parameters.AddWithValue("notes", (object?)dto.Notes ?? DBNull.Value);
            var affected = await cmd.ExecuteNonQueryAsync();
            if (affected == 0) { await tx.RollbackAsync(); return null; }
        }

        using (var cmd = new NpgsqlCommand(
            "INSERT INTO audit_log (user_id, action, entity_type, entity_id, details, created_at) VALUES (@uid, 'update_expense', 'expense', @eid, @details, NOW())",
            conn, tx))
        {
            cmd.Parameters.AddWithValue("uid", userId);
            cmd.Parameters.AddWithValue("eid", id);
            cmd.Parameters.AddWithValue("details", "Expense updated: " + dto.Amount);
            await cmd.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();
        return await GetExpenseAsync(id);
    }

    public async Task<bool> DeleteExpenseAsync(int id, int userId)
    {
        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();
        using var cmd = new NpgsqlCommand("DELETE FROM expenses WHERE id = @id", conn, tx);
        cmd.Parameters.AddWithValue("id", id);
        var affected = await cmd.ExecuteNonQueryAsync();
        if (affected == 0) { await tx.RollbackAsync(); return false; }

        using var cmd2 = new NpgsqlCommand(
            "INSERT INTO audit_log (user_id, action, entity_type, entity_id, details, created_at) VALUES (@uid, 'delete_expense', 'expense', @eid, @details, NOW())",
            conn, tx);
        cmd2.Parameters.AddWithValue("uid", userId);
        cmd2.Parameters.AddWithValue("eid", id);
        cmd2.Parameters.AddWithValue("details", "Deleted expense id " + id);
        await cmd2.ExecuteNonQueryAsync();

        await tx.CommitAsync();
        return true;
    }
}