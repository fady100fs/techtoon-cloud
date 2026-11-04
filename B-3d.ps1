$ErrorActionPreference = "Stop"
$root = "D:\PROG\Accounting Prog\Techtoon Accounting C+"
$apiDir = Join-Path $root "Techtoon.Api"
$utf8 = New-Object System.Text.UTF8Encoding($false)

function WF($path, $content) {
    [System.IO.File]::WriteAllText($path, $content, $utf8)
    $rel = $path.Replace($root + '\', '')
    Write-Host "  + $rel" -ForegroundColor Green
}

Write-Host "=== B-3d: Payments + Expenses + CashBoxes ===" -ForegroundColor Cyan
Write-Host ""

# =============================================
# 1. PAYMENTS (سندات القبض والصرف)
# =============================================
Write-Host "[1/3] Creating Payments..." -ForegroundColor Yellow

$paymentDto = @'
namespace Techtoon.Api.DTOs.Payments;

public class PaymentDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public int? PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentType { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string? VoucherNo { get; set; }
    public string? Notes { get; set; }
    public int? CashBoxId { get; set; }
}

public class PaymentCreateDto
{
    public DateTime Date { get; set; } = DateTime.Now;
    public int? PartyId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentType { get; set; } = "receipt";
    public string PaymentMethod { get; set; } = "cash";
    public string? VoucherNo { get; set; }
    public string? Notes { get; set; }
    public int? CashBoxId { get; set; }
}
'@
WF "$apiDir\DTOs\Payments\PaymentDto.cs" $paymentDto

$paymentSvc = @'
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
'@
WF "$apiDir\Services\PaymentAppService.cs" $paymentSvc

$paymentsCtrl = @'
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Techtoon.Api.DTOs.Payments;
using Techtoon.Api.Services;

namespace Techtoon.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentAppService _svc;
    private readonly ICurrentUserService _current;

    public PaymentsController(IPaymentAppService svc, ICurrentUserService current)
    {
        _svc = svc;
        _current = current;
    }

    [HttpGet]
    public async Task<IActionResult> GetPayments(
        [FromQuery] string? type,
        [FromQuery] int? partyId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _svc.GetPaymentsAsync(type, partyId, from, to, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetPayment(int id)
    {
        var p = await _svc.GetPaymentAsync(id);
        if (p == null) return NotFound();
        return Ok(p);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePayment([FromBody] PaymentCreateDto dto)
    {
        if (_current.UserId == null) return Unauthorized();
        try
        {
            var created = await _svc.CreatePaymentAsync(dto, _current.UserId.Value);
            return CreatedAtAction(nameof(GetPayment), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeletePayment(int id)
    {
        if (_current.UserId == null) return Unauthorized();
        var deleted = await _svc.DeletePaymentAsync(id, _current.UserId.Value);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
'@
WF "$apiDir\Controllers\PaymentsController.cs" $paymentsCtrl


# =============================================
# 2. EXPENSES (المصروفات)
# =============================================
Write-Host "[2/3] Creating Expenses..." -ForegroundColor Yellow

$expenseDto = @'
namespace Techtoon.Api.DTOs.Expenses;

public class ExpenseDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public int? CategoryId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public int? CashBoxId { get; set; }
    public string? Notes { get; set; }
}

public class ExpenseCreateDto
{
    public DateTime Date { get; set; } = DateTime.Now;
    public int? CategoryId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string PaymentMethod { get; set; } = "cash";
    public int? CashBoxId { get; set; }
    public string? Notes { get; set; }
}
'@
WF "$apiDir\DTOs\Expenses\ExpenseDto.cs" $expenseDto

$expenseSvc = @'
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
            foreach (var p in parms) cmd.Parameters.Add(p);
            result.TotalCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        var sql = "SELECT e.id::bigint, e.date, e.category_id, e.amount::numeric, COALESCE(e.description,'') AS description, " +
                  "COALESCE(e.payment_method,'') AS payment_method, e.cash_box_id, COALESCE(e.notes,'') AS notes " +
                  "FROM expenses e " + whereSql + " ORDER BY e.date DESC, e.id DESC LIMIT @limit OFFSET @offset";

        using (var cmd = new NpgsqlCommand(sql, conn))
        {
            foreach (var p in parms) cmd.Parameters.Add(p);
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

        int newId;
        using (var cmd = new NpgsqlCommand(
            "INSERT INTO expenses (date, category_id, amount, description, payment_method, cash_box_id, notes, created_by) " +
            "VALUES (@date, @cat, @amount, @desc, @method, @cb, @notes, @uid) RETURNING id", conn, tx))
        {
            cmd.Parameters.AddWithValue("date", dto.Date);
            cmd.Parameters.AddWithValue("cat", (object?)dto.CategoryId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("amount", dto.Amount);
            cmd.Parameters.AddWithValue("desc", (object?)dto.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("method", dto.PaymentMethod);
            cmd.Parameters.AddWithValue("cb", (object?)dto.CashBoxId ?? DBNull.Value);
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
'@
WF "$apiDir\Services\ExpenseAppService.cs" $expenseSvc

$expensesCtrl = @'
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Techtoon.Api.DTOs.Expenses;
using Techtoon.Api.Services;

namespace Techtoon.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseAppService _svc;
    private readonly ICurrentUserService _current;

    public ExpensesController(IExpenseAppService svc, ICurrentUserService current)
    {
        _svc = svc;
        _current = current;
    }

    [HttpGet]
    public async Task<IActionResult> GetExpenses(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? categoryId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _svc.GetExpensesAsync(from, to, categoryId, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetExpense(int id)
    {
        var e = await _svc.GetExpenseAsync(id);
        if (e == null) return NotFound();
        return Ok(e);
    }

    [HttpPost]
    public async Task<IActionResult> CreateExpense([FromBody] ExpenseCreateDto dto)
    {
        if (_current.UserId == null) return Unauthorized();
        try
        {
            var created = await _svc.CreateExpenseAsync(dto, _current.UserId.Value);
            return CreatedAtAction(nameof(GetExpense), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteExpense(int id)
    {
        if (_current.UserId == null) return Unauthorized();
        var deleted = await _svc.DeleteExpenseAsync(id, _current.UserId.Value);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
'@
WF "$apiDir\Controllers\ExpensesController.cs" $expensesCtrl


# =============================================
# 3. CASHBOXES (الخزائن)
# =============================================
Write-Host "[3/3] Creating CashBoxes..." -ForegroundColor Yellow

$cashBoxDto = @'
namespace Techtoon.Api.DTOs.CashBoxes;

public class CashBoxDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public string Currency { get; set; } = string.Empty;
}

public class CashBoxCreateDto
{
    public string Name { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public string Currency { get; set; } = "EGP";
}
'@
WF "$apiDir\DTOs\CashBoxes\CashBoxDto.cs" $cashBoxDto

$cashBoxSvc = @'
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
    public CashBoxAppService(IConfiguration config) { _connStr = config.GetConnectionString("DefaultConnection") ?? ""; }

    private static decimal SafeDecimal(object? v) => v == null || v == DBNull.Value ? 0m : Convert.ToDecimal(v);
    private static int SafeInt(object? v) => v == null || v == DBNull.Value ? 0 : Convert.ToInt32(v);
    private static string SafeString(object? v) => v == null || v == DBNull.Value ? string.Empty : v.ToString() ?? string.Empty;

    private string BuildBalanceExpr()
    {
        // Dynamic balance = opening + receipts - payments - expenses
        return "COALESCE(c.opening_balance,0)::numeric + " +
               "COALESCE((SELECT SUM(amount) FROM payments WHERE cash_box_id = c.id AND payment_type='receipt'), 0)::numeric - " +
               "COALESCE((SELECT SUM(amount) FROM payments WHERE cash_box_id = c.id AND payment_type='payment'), 0)::numeric - " +
               "COALESCE((SELECT SUM(amount) FROM expenses WHERE cash_box_id = c.id), 0)::numeric";
    }

    public async Task<List<CashBoxDto>> GetCashBoxesAsync()
    {
        var list = new List<CashBoxDto>();
        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        var sql = "SELECT c.id::bigint, c.name, COALESCE(c.opening_balance,0)::numeric AS opening_balance, " +
                  "COALESCE(c.currency,'EGP') AS currency, " + BuildBalanceExpr() + " AS current_balance " +
                  "FROM cash_boxes c ORDER BY c.name";
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
        var sql = "SELECT c.id::bigint, c.name, COALESCE(c.opening_balance,0)::numeric AS opening_balance, " +
                  "COALESCE(c.currency,'EGP') AS currency, " + BuildBalanceExpr() + " AS current_balance " +
                  "FROM cash_boxes c WHERE c.id = @id";
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

        int newId;
        using (var cmd = new NpgsqlCommand(
            "INSERT INTO cash_boxes (name, opening_balance, currency, is_active, created_at) " +
            "VALUES (@name, @opening, @cur, TRUE, NOW()) RETURNING id", conn, tx))
        {
            cmd.Parameters.AddWithValue("name", dto.Name);
            cmd.Parameters.AddWithValue("opening", dto.OpeningBalance);
            cmd.Parameters.AddWithValue("cur", dto.Currency);
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
'@
WF "$apiDir\Services\CashBoxAppService.cs" $cashBoxSvc

$cashBoxesCtrl = @'
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Techtoon.Api.DTOs.CashBoxes;
using Techtoon.Api.Services;

namespace Techtoon.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CashBoxesController : ControllerBase
{
    private readonly ICashBoxAppService _svc;
    private readonly ICurrentUserService _current;

    public CashBoxesController(ICashBoxAppService svc, ICurrentUserService current)
    {
        _svc = svc;
        _current = current;
    }

    [HttpGet]
    public async Task<IActionResult> GetCashBoxes()
    {
        var list = await _svc.GetCashBoxesAsync();
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCashBox(int id)
    {
        var c = await _svc.GetCashBoxAsync(id);
        if (c == null) return NotFound();
        return Ok(c);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCashBox([FromBody] CashBoxCreateDto dto)
    {
        if (_current.UserId == null) return Unauthorized();
        try
        {
            var created = await _svc.CreateCashBoxAsync(dto, _current.UserId.Value);
            return CreatedAtAction(nameof(GetCashBox), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
'@
WF "$apiDir\Controllers\CashBoxesController.cs" $cashBoxesCtrl


# =============================================
# 4. Register in Program.cs
# =============================================
Write-Host ""
Write-Host "Updating Program.cs..." -ForegroundColor Yellow
$program = Get-Content "$apiDir\Program.cs" -Raw -Encoding UTF8
if ($program -notmatch 'IPaymentAppService') {
    $program = $program -replace 'builder\.Services\.AddScoped<IInvoiceAppService, InvoiceAppService>\(\);',
        "builder.Services.AddScoped<IInvoiceAppService, InvoiceAppService>();`nbuilder.Services.AddScoped<IPaymentAppService, PaymentAppService>();`nbuilder.Services.AddScoped<IExpenseAppService, ExpenseAppService>();`nbuilder.Services.AddScoped<ICashBoxAppService, CashBoxAppService>();"
    [System.IO.File]::WriteAllText("$apiDir\Program.cs", $program, $utf8)
    Write-Host "  + Techtoon.Api\Program.cs (3 services registered)" -ForegroundColor Green
} else {
    Write-Host "  (Services already registered)" -ForegroundColor DarkGray
}

# 5. Build
Write-Host ""
Write-Host "Building..." -ForegroundColor Yellow
Push-Location $root
try {
    $buildOutput = & dotnet build Techtoon.Api 2>&1
    $buildResult = $LASTEXITCODE
    $buildOutput | ForEach-Object { Write-Host $_ }
    if ($buildResult -eq 0) {
        Write-Host ""
        Write-Host "=== B-3d BUILD SUCCEEDED ===" -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "=== B-3d BUILD FAILED ===" -ForegroundColor Red
    }
} finally {
    Pop-Location
}
