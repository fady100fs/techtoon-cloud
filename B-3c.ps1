 $ErrorActionPreference = "Stop"
 $root = "D:\PROG\Accounting Prog\Techtoon Accounting C+"
 $apiDir = Join-Path $root "Techtoon.Api"
 $utf8 = New-Object System.Text.UTF8Encoding($false)

function WF($path, $content) {
    [System.IO.File]::WriteAllText($path, $content, $utf8)
    $rel = $path.Replace($root + '\', '')
    Write-Host "  + $rel" -ForegroundColor Green
}

Write-Host "=== B-3c: Invoices Controller + DTOs + Service ===" -ForegroundColor Cyan
Write-Host ""

# 1. DTOs
Write-Host "[1/4] Creating Invoice DTOs..." -ForegroundColor Yellow

 $invoiceLineDto = @'
namespace Techtoon.Api.DTOs.Invoices;

public class InvoiceLineDto
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public decimal CostPrice { get; set; }
}
'@
WF "$apiDir\DTOs\Invoices\InvoiceLineDto.cs" $invoiceLineDto

 $invoiceListDto = @'
namespace Techtoon.Api.DTOs.Invoices;

public class InvoiceListDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public decimal PaidAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public int? WarehouseId { get; set; }
}
'@
WF "$apiDir\DTOs\Invoices\InvoiceListDto.cs" $invoiceListDto

 $invoiceDetailDto = @'
namespace Techtoon.Api.DTOs.Invoices;

public class InvoiceDetailDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public DateTime? DueDate { get; set; }
    public string Type { get; set; } = string.Empty;
    public int? PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public int? WarehouseId { get; set; }
    public int? CashBoxId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public decimal PaidAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<InvoiceLineDto> Lines { get; set; } = new();
    public List<InvoicePaymentDto> Payments { get; set; } = new();
}

public class InvoicePaymentDto
{
    public int Id { get; set; }
    public string Method { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public DateTime PaidAt { get; set; }
}
'@
WF "$apiDir\DTOs\Invoices\InvoiceDetailDto.cs" $invoiceDetailDto

 $invoiceCreateDto = @'
namespace Techtoon.Api.DTOs.Invoices;

public class InvoiceCreateDto
{
    public string Type { get; set; } = "sales";
    public int? PartyId { get; set; }
    public int? WarehouseId { get; set; }
    public int? CashBoxId { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
    public DateTime? DueDate { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxRate { get; set; }
    public string? Notes { get; set; }
    public List<InvoiceLineInputDto> Lines { get; set; } = new();
    public List<InvoicePaymentInputDto> Payments { get; set; } = new();
}

public class InvoiceLineInputDto
{
    public int ItemId { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Discount { get; set; }
}

public class InvoicePaymentInputDto
{
    public string Method { get; set; } = "cash";
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public DateTime? PaidAt { get; set; }
}
'@
WF "$apiDir\DTOs\Invoices\InvoiceCreateDto.cs" $invoiceCreateDto

# 2. InvoiceAppService
Write-Host "[2/4] Creating InvoiceAppService..." -ForegroundColor Yellow
 $invoiceAppSvc = @'
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Techtoon.Api.DTOs.Common;
using Techtoon.Api.DTOs.Invoices;

namespace Techtoon.Api.Services;

public interface IInvoiceAppService
{
    Task<PagedResultDto<InvoiceListDto>> GetInvoicesAsync(string? type, string? status, int? partyId, DateTime? from, DateTime? to, string? search, int page, int pageSize);
    Task<InvoiceDetailDto?> GetInvoiceAsync(int id);
    Task<InvoiceDetailDto> CreateInvoiceAsync(InvoiceCreateDto dto, int userId);
    Task<bool> DeleteInvoiceAsync(int id, int userId);
    Task<bool> CancelInvoiceAsync(int id, int userId);
}

public class InvoiceAppService : IInvoiceAppService
{
    private readonly string _connStr;

    public InvoiceAppService(IConfiguration config)
    {
        _connStr = config.GetConnectionString("DefaultConnection") ?? "";
    }

    private static decimal SafeDecimal(object? v) => v == null || v == DBNull.Value ? 0m : Convert.ToDecimal(v);
    private static int SafeInt(object? v) => v == null || v == DBNull.Value ? 0 : Convert.ToInt32(v);
    private static string SafeString(object? v) => v == null || v == DBNull.Value ? string.Empty : v.ToString() ?? string.Empty;

    public async Task<PagedResultDto<InvoiceListDto>> GetInvoicesAsync(string? type, string? status, int? partyId, DateTime? from, DateTime? to, string? search, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 50;

        var result = new PagedResultDto<InvoiceListDto> { Page = page, PageSize = pageSize };
        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();

        var where = new List<string>();
        var parms = new List<NpgsqlParameter>();
        if (!string.IsNullOrEmpty(type)) { where.Add("i.type = @t"); parms.Add(new NpgsqlParameter("t", type)); }
        if (!string.IsNullOrEmpty(status)) { where.Add("i.status = @s"); parms.Add(new NpgsqlParameter("s", status)); }
        if (partyId.HasValue) { where.Add("i.party_id = @pid"); parms.Add(new NpgsqlParameter("pid", partyId.Value)); }
        if (from.HasValue) { where.Add("i.date >= @from"); parms.Add(new NpgsqlParameter("from", from.Value)); }
        if (to.HasValue) { where.Add("i.date <= @to"); parms.Add(new NpgsqlParameter("to", to.Value)); }
        if (!string.IsNullOrEmpty(search)) {
            where.Add("(i.invoice_number ILIKE @q OR COALESCE(p.name,'') ILIKE @q)");
            parms.Add(new NpgsqlParameter("q", "%" + search + "%"));
        }
        var whereSql = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";

        using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM invoices i LEFT JOIN parties p ON i.party_id = p.id " + whereSql, conn))
        {
            foreach (var p in parms) cmd.Parameters.Add(p);
            result.TotalCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        var sql = "SELECT i.id::bigint, COALESCE(i.invoice_number,'') AS invoice_number, i.date, COALESCE(i.type,'') AS type, " +
                  "COALESCE(i.total,0)::numeric AS total, COALESCE(i.paid_amount,0)::numeric AS paid_amount, " +
                  "COALESCE(i.status,'') AS status, i.party_id, COALESCE(p.name,'') AS party_name, i.warehouse_id " +
                  "FROM invoices i LEFT JOIN parties p ON i.party_id = p.id " + whereSql + " " +
                  "ORDER BY i.date DESC, i.id DESC LIMIT @limit OFFSET @offset";

        using (var cmd = new NpgsqlCommand(sql, conn))
        {
            foreach (var p in parms) cmd.Parameters.Add(p);
            cmd.Parameters.AddWithValue("limit", pageSize);
            cmd.Parameters.AddWithValue("offset", (page - 1) * pageSize);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                result.Items.Add(new InvoiceListDto
                {
                    Id = SafeInt(r["id"]),
                    InvoiceNumber = SafeString(r["invoice_number"]),
                    Date = r.GetDateTime(r.GetOrdinal("date")),
                    Type = SafeString(r["type"]),
                    Total = SafeDecimal(r["total"]),
                    PaidAmount = SafeDecimal(r["paid_amount"]),
                    Status = SafeString(r["status"]),
                    PartyId = r["party_id"] == DBNull.Value ? null : SafeInt(r["party_id"]),
                    PartyName = SafeString(r["party_name"]),
                    WarehouseId = r["warehouse_id"] == DBNull.Value ? null : SafeInt(r["warehouse_id"])
                });
            }
        }
        return result;
    }

    public async Task<InvoiceDetailDto?> GetInvoiceAsync(int id)
    {
        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();

        var sqlHdr = "SELECT i.id::bigint, COALESCE(i.invoice_number,'') AS invoice_number, i.date, i.due_date, " +
                     "COALESCE(i.type,'') AS type, i.party_id, COALESCE(p.name,'') AS party_name, " +
                     "i.warehouse_id, i.cash_box_id, COALESCE(i.subtotal,0)::numeric AS subtotal, " +
                     "COALESCE(i.discount_amount,0)::numeric AS discount_amount, COALESCE(i.tax_rate,0)::numeric AS tax_rate, " +
                     "COALESCE(i.tax_amount,0)::numeric AS tax_amount, COALESCE(i.total,0)::numeric AS total, " +
                     "COALESCE(i.paid_amount,0)::numeric AS paid_amount, COALESCE(i.status,'') AS status, " +
                     "COALESCE(i.notes,'') AS notes, COALESCE(i.created_by,0)::bigint AS created_by, i.created_at " +
                     "FROM invoices i LEFT JOIN parties p ON i.party_id = p.id WHERE i.id = @id";

        InvoiceDetailDto? inv = null;
        using (var cmd = new NpgsqlCommand(sqlHdr, conn))
        {
            cmd.Parameters.AddWithValue("id", id);
            using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync()) return null;
            inv = new InvoiceDetailDto
            {
                Id = SafeInt(r["id"]),
                InvoiceNumber = SafeString(r["invoice_number"]),
                Date = r.GetDateTime(r.GetOrdinal("date")),
                DueDate = r["due_date"] == DBNull.Value ? null : r.GetDateTime(r.GetOrdinal("due_date")),
                Type = SafeString(r["type"]),
                PartyId = r["party_id"] == DBNull.Value ? null : SafeInt(r["party_id"]),
                PartyName = SafeString(r["party_name"]),
                WarehouseId = r["warehouse_id"] == DBNull.Value ? null : SafeInt(r["warehouse_id"]),
                CashBoxId = r["cash_box_id"] == DBNull.Value ? null : SafeInt(r["cash_box_id"]),
                Subtotal = SafeDecimal(r["subtotal"]),
                DiscountAmount = SafeDecimal(r["discount_amount"]),
                TaxRate = SafeDecimal(r["tax_rate"]),
                TaxAmount = SafeDecimal(r["tax_amount"]),
                Total = SafeDecimal(r["total"]),
                PaidAmount = SafeDecimal(r["paid_amount"]),
                Status = SafeString(r["status"]),
                Notes = r["notes"] == DBNull.Value ? null : SafeString(r["notes"]),
                CreatedBy = SafeInt(r["created_by"]),
                CreatedAt = r.GetDateTime(r.GetOrdinal("created_at"))
            };
        }

        // Lines
        var sqlLines = "SELECT il.id::bigint, il.item_id, COALESCE(it.name,'') AS item_name, " +
                       "il.quantity::numeric, il.price::numeric, COALESCE(il.discount,0)::numeric AS discount, " +
                       "il.total::numeric, COALESCE(il.cost_price,0)::numeric AS cost_price " +
                       "FROM invoice_lines il LEFT JOIN items it ON il.item_id = it.id WHERE il.invoice_id = @id";
        using (var cmd = new NpgsqlCommand(sqlLines, conn))
        {
            cmd.Parameters.AddWithValue("id", id);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                inv.Lines.Add(new InvoiceLineDto
                {
                    Id = SafeInt(r["id"]),
                    ItemId = SafeInt(r["item_id"]),
                    ItemName = SafeString(r["item_name"]),
                    Quantity = SafeDecimal(r["quantity"]),
                    Price = SafeDecimal(r["price"]),
                    Discount = SafeDecimal(r["discount"]),
                    Total = SafeDecimal(r["total"]),
                    CostPrice = SafeDecimal(r["cost_price"])
                });
            }
        }

        // Payments
        var sqlPay = "SELECT id::bigint, COALESCE(method,'') AS method, amount::numeric, COALESCE(reference,'') AS reference, paid_at " +
                     "FROM invoice_payments WHERE invoice_id = @id ORDER BY paid_at";
        using (var cmd = new NpgsqlCommand(sqlPay, conn))
        {
            cmd.Parameters.AddWithValue("id", id);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                inv.Payments.Add(new InvoicePaymentDto
                {
                    Id = SafeInt(r["id"]),
                    Method = SafeString(r["method"]),
                    Amount = SafeDecimal(r["amount"]),
                    Reference = r["reference"] == DBNull.Value ? null : SafeString(r["reference"]),
                    PaidAt = r.GetDateTime(r.GetOrdinal("paid_at"))
                });
            }
        }

        return inv;
    }

    public async Task<InvoiceDetailDto> CreateInvoiceAsync(InvoiceCreateDto dto, int userId)
    {
        if (dto.Lines == null || dto.Lines.Count == 0)
            throw new ArgumentException("Invoice must have at least one line");
        if (string.IsNullOrEmpty(dto.Type)) dto.Type = "sales";

        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        try
        {
            // Generate invoice number
            var invNumber = await GenerateInvoiceNumberAsync(conn, tx, dto.Type);

            // Calculate totals
            decimal subtotal = 0;
            foreach (var line in dto.Lines)
            {
                var lineTotal = line.Quantity * line.Price - line.Discount;
                if (lineTotal < 0) lineTotal = 0;
                subtotal += lineTotal;
            }
            var discountAmount = dto.DiscountAmount;
            var taxableBase = subtotal - discountAmount;
            if (taxableBase < 0) taxableBase = 0;
            var taxAmount = taxableBase * (dto.TaxRate / 100m);
            var total = taxableBase + taxAmount;

            decimal paidAmount = 0;
            if (dto.Payments != null)
                foreach (var p in dto.Payments) paidAmount += p.Amount;

            var status = paidAmount >= total ? "paid" : (paidAmount > 0 ? "partial" : "unpaid");

            // Insert invoice
            int newId;
            using (var cmd = new NpgsqlCommand(
                "INSERT INTO invoices (invoice_number, date, due_date, type, party_id, warehouse_id, cash_box_id, " +
                "subtotal, discount_amount, tax_rate, tax_amount, total, paid_amount, status, notes, created_by, created_at, updated_at) " +
                "VALUES (@num, @date, @due, @type, @pid, @wh, @cb, @sub, @disc, @taxrate, @tax, @total, @paid, @status, @notes, @cb_by, NOW(), NOW()) RETURNING id", conn, tx))
            {
                cmd.Parameters.AddWithValue("num", invNumber);
                cmd.Parameters.AddWithValue("date", dto.Date);
                cmd.Parameters.AddWithValue("due", (object?)dto.DueDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("type", dto.Type);
                cmd.Parameters.AddWithValue("pid", (object?)dto.PartyId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("wh", (object?)dto.WarehouseId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("cb", (object?)dto.CashBoxId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("sub", subtotal);
                cmd.Parameters.AddWithValue("disc", discountAmount);
                cmd.Parameters.AddWithValue("taxrate", dto.TaxRate);
                cmd.Parameters.AddWithValue("tax", taxAmount);
                cmd.Parameters.AddWithValue("total", total);
                cmd.Parameters.AddWithValue("paid", paidAmount);
                cmd.Parameters.AddWithValue("status", status);
                cmd.Parameters.AddWithValue("notes", (object?)dto.Notes ?? DBNull.Value);
                cmd.Parameters.AddWithValue("cb_by", userId);
                newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            // Insert lines + inventory movements
            foreach (var line in dto.Lines)
            {
                int lineId;
                using (var cmd = new NpgsqlCommand(
                    "INSERT INTO invoice_lines (invoice_id, item_id, quantity, price, total, cost_price, discount) " +
                    "VALUES (@iid, @item, @qty, @price, @total, @cost, @disc) RETURNING id", conn, tx))
                {
                    var lineTotal = line.Quantity * line.Price - line.Discount;
                    if (lineTotal < 0) lineTotal = 0;
                    cmd.Parameters.AddWithValue("iid", newId);
                    cmd.Parameters.AddWithValue("item", line.ItemId);
                    cmd.Parameters.AddWithValue("qty", line.Quantity);
                    cmd.Parameters.AddWithValue("price", line.Price);
                    cmd.Parameters.AddWithValue("total", lineTotal);
                    cmd.Parameters.AddWithValue("cost", 0);
                    cmd.Parameters.AddWithValue("disc", line.Discount);
                    lineId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                // Inventory movement (out for sales, in for purchase)
                var movType = dto.Type == "sales" ? "out" : "in";
                var qtySigned = dto.Type == "sales" ? -line.Quantity : line.Quantity;
                using (var cmd = new NpgsqlCommand(
                    "INSERT INTO inventory_movements (item_id, date, type, quantity, reference_id, reference_type, created_at) " +
                    "VALUES (@item, NOW(), @mtype, @qty, @refId, 'invoice', NOW())", conn, tx))
                {
                    cmd.Parameters.AddWithValue("item", line.ItemId);
                    cmd.Parameters.AddWithValue("mtype", movType);
                    cmd.Parameters.AddWithValue("qty", qtySigned);
                    cmd.Parameters.AddWithValue("refId", newId);
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            // Insert payments
            if (dto.Payments != null && dto.Payments.Count > 0)
            {
                foreach (var p in dto.Payments)
                {
                    using (var cmd = new NpgsqlCommand(
                        "INSERT INTO invoice_payments (invoice_id, method, amount, reference, paid_at, created_at) " +
                        "VALUES (@iid, @method, @amount, @ref, @paidAt, NOW())", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("iid", newId);
                        cmd.Parameters.AddWithValue("method", p.Method);
                        cmd.Parameters.AddWithValue("amount", p.Amount);
                        cmd.Parameters.AddWithValue("ref", (object?)p.Reference ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("paidAt", p.PaidAt ?? DateTime.Now);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }

            await LogAuditAsync(conn, tx, userId, "create_invoice", "invoice", newId, "Created " + dto.Type + " invoice " + invNumber + " total " + total);
            await tx.CommitAsync();

            return (await GetInvoiceAsync(newId))!;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> DeleteInvoiceAsync(int id, int userId)
    {
        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        try
        {
            // Delete inventory movements for this invoice
            using (var cmd = new NpgsqlCommand("DELETE FROM inventory_movements WHERE reference_id = @id AND reference_type = 'invoice'", conn, tx))
            {
                cmd.Parameters.AddWithValue("id", id);
                await cmd.ExecuteNonQueryAsync();
            }

            // Delete invoice (CASCADE will handle invoice_lines and invoice_payments)
            using (var cmd = new NpgsqlCommand("DELETE FROM invoices WHERE id = @id", conn, tx))
            {
                cmd.Parameters.AddWithValue("id", id);
                var affected = await cmd.ExecuteNonQueryAsync();
                if (affected == 0) { await tx.RollbackAsync(); return false; }
            }

            await LogAuditAsync(conn, tx, userId, "delete_invoice", "invoice", id, "Deleted invoice id " + id);
            await tx.CommitAsync();
            return true;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> CancelInvoiceAsync(int id, int userId)
    {
        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        using (var cmd = new NpgsqlCommand("UPDATE invoices SET status = 'cancelled', updated_at = NOW() WHERE id = @id AND status <> 'cancelled'", conn, tx))
        {
            cmd.Parameters.AddWithValue("id", id);
            var affected = await cmd.ExecuteNonQueryAsync();
            if (affected == 0) { await tx.RollbackAsync(); return false; }
        }

        // Reverse inventory movements
        using (var cmd = new NpgsqlCommand(
            "UPDATE inventory_movements SET quantity = -quantity WHERE reference_id = @id AND reference_type = 'invoice'", conn, tx))
        {
            cmd.Parameters.AddWithValue("id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        await LogAuditAsync(conn, tx, userId, "cancel_invoice", "invoice", id, "Cancelled invoice id " + id);
        await tx.CommitAsync();
        return true;
    }

    private async Task<string> GenerateInvoiceNumberAsync(NpgsqlConnection conn, NpgsqlTransaction tx, string type)
    {
        var prefix = type == "sales" ? "SAL" : "PUR";
        var year = DateTime.Now.Year;
        using var cmd = new NpgsqlCommand(
            "SELECT COUNT(*) FROM invoices WHERE type = @t AND EXTRACT(YEAR FROM date) = @y", conn, tx);
        cmd.Parameters.AddWithValue("t", type);
        cmd.Parameters.AddWithValue("y", year);
        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        return prefix + "-" + year + "-" + (count + 1).ToString("D6");
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
'@
WF "$apiDir\Services\InvoiceAppService.cs" $invoiceAppSvc

# 3. InvoicesController
Write-Host "[3/4] Creating InvoicesController..." -ForegroundColor Yellow
 $invoicesCtrl = @'
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Techtoon.Api.DTOs.Invoices;
using Techtoon.Api.Services;

namespace Techtoon.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceAppService _svc;
    private readonly ICurrentUserService _current;

    public InvoicesController(IInvoiceAppService svc, ICurrentUserService current)
    {
        _svc = svc;
        _current = current;
    }

    [HttpGet]
    public async Task<IActionResult> GetInvoices(
        [FromQuery] string? type,
        [FromQuery] string? status,
        [FromQuery] int? partyId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _svc.GetInvoicesAsync(type, status, partyId, from, to, search, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetInvoice(int id)
    {
        var inv = await _svc.GetInvoiceAsync(id);
        if (inv == null) return NotFound();
        return Ok(inv);
    }

    [HttpPost]
    public async Task<IActionResult> CreateInvoice([FromBody] InvoiceCreateDto dto)
    {
        if (_current.UserId == null) return Unauthorized();
        try
        {
            var created = await _svc.CreateInvoiceAsync(dto, _current.UserId.Value);
            return CreatedAtAction(nameof(GetInvoice), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteInvoice(int id)
    {
        if (_current.UserId == null) return Unauthorized();
        var deleted = await _svc.DeleteInvoiceAsync(id, _current.UserId.Value);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> CancelInvoice(int id)
    {
        if (_current.UserId == null) return Unauthorized();
        var cancelled = await _svc.CancelInvoiceAsync(id, _current.UserId.Value);
        if (!cancelled) return NotFound(new { message = "Invoice not found or already cancelled" });
        return Ok(new { message = "Invoice cancelled" });
    }
}
'@
WF "$apiDir\Controllers\InvoicesController.cs" $invoicesCtrl

# 4. Register InvoiceAppService in Program.cs
Write-Host "[4/4] Updating Program.cs..." -ForegroundColor Yellow
 $program = Get-Content "$apiDir\Program.cs" -Raw -Encoding UTF8
if ($program -notmatch 'IInvoiceAppService') {
    $program = $program -replace 'builder\.Services\.AddScoped<IPartyAppService, PartyAppService>\(\);',
        "builder.Services.AddScoped<IPartyAppService, PartyAppService>();`nbuilder.Services.AddScoped<IInvoiceAppService, InvoiceAppService>();"
    [System.IO.File]::WriteAllText("$apiDir\Program.cs", $program, $utf8)
    Write-Host "  + Techtoon.Api\Program.cs (IInvoiceAppService registered)" -ForegroundColor Green
} else {
    Write-Host "  (IInvoiceAppService already registered)" -ForegroundColor DarkGray
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
        Write-Host "=== B-3c BUILD SUCCEEDED ===" -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "=== B-3c BUILD FAILED ===" -ForegroundColor Red
    }
} finally {
    Pop-Location
}