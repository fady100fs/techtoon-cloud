 $ErrorActionPreference = "Stop"
 $root = "D:\PROG\Accounting Prog\Techtoon Accounting C+"
 $apiDir = Join-Path $root "Techtoon.Api"
 $utf8 = New-Object System.Text.UTF8Encoding($false)

function WF($path, $content) {
    [System.IO.File]::WriteAllText($path, $content, $utf8)
    $rel = $path.Replace($root + '\', '')
    Write-Host "  + $rel" -ForegroundColor Green
}

Write-Host "=== B-3b: Parties Controller + DTOs + Service ===" -ForegroundColor Cyan
Write-Host ""

# 1. DTOs
Write-Host "[1/3] Creating Party DTOs..." -ForegroundColor Yellow

 $partyListDto = @'
namespace Techtoon.Api.DTOs.Parties;

public class PartyListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Phone { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal CurrentBalance { get; set; }
}
'@
WF "$apiDir\DTOs\Parties\PartyListDto.cs" $partyListDto

 $partyDetailDto = @'
namespace Techtoon.Api.DTOs.Parties;

public class PartyDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal CurrentBalance { get; set; }
}
'@
WF "$apiDir\DTOs\Parties\PartyDetailDto.cs" $partyDetailDto

 $partyCreateDto = @'
namespace Techtoon.Api.DTOs.Parties;

public class PartyCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; } = "customer";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
    public decimal OpeningBalance { get; set; }
}
'@
WF "$apiDir\DTOs\Parties\PartyCreateDto.cs" $partyCreateDto

 $partyUpdateDto = @'
namespace Techtoon.Api.DTOs.Parties;

public class PartyUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
}
'@
WF "$apiDir\DTOs\Parties\PartyUpdateDto.cs" $partyUpdateDto

 $partyStatementDto = @'
namespace Techtoon.Api.DTOs.Parties;

public class PartyStatementDto
{
    public int PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public List<StatementEntryDto> Entries { get; set; } = new();
}

public class StatementEntryDto
{
    public DateTime Date { get; set; }
    public string EntryType { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Notes { get; set; }
}
'@
WF "$apiDir\DTOs\Parties\PartyStatementDto.cs" $partyStatementDto

# 2. PartyAppService
Write-Host "[2/3] Creating PartyAppService..." -ForegroundColor Yellow
 $partyAppSvc = @'
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
'@
WF "$apiDir\Services\PartyAppService.cs" $partyAppSvc

# 3. PartiesController
Write-Host "[3/3] Creating PartiesController..." -ForegroundColor Yellow
 $partiesCtrl = @'
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Techtoon.Api.DTOs.Parties;
using Techtoon.Api.Services;

namespace Techtoon.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PartiesController : ControllerBase
{
    private readonly IPartyAppService _svc;
    private readonly ICurrentUserService _current;

    public PartiesController(IPartyAppService svc, ICurrentUserService current)
    {
        _svc = svc;
        _current = current;
    }

    [HttpGet]
    public async Task<IActionResult> GetParties(
        [FromQuery] string? search,
        [FromQuery] string? type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _svc.GetPartiesAsync(search, type, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetParty(int id)
    {
        var party = await _svc.GetPartyAsync(id);
        if (party == null) return NotFound();
        return Ok(party);
    }

    [HttpPost]
    public async Task<IActionResult> CreateParty([FromBody] PartyCreateDto dto)
    {
        if (_current.UserId == null) return Unauthorized();
        var created = await _svc.CreatePartyAsync(dto, _current.UserId.Value);
        return CreatedAtAction(nameof(GetParty), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateParty(int id, [FromBody] PartyUpdateDto dto)
    {
        if (_current.UserId == null) return Unauthorized();
        var updated = await _svc.UpdatePartyAsync(id, dto, _current.UserId.Value);
        if (updated == null) return NotFound();
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteParty(int id)
    {
        if (_current.UserId == null) return Unauthorized();
        try
        {
            var deleted = await _svc.DeletePartyAsync(id, _current.UserId.Value);
            if (!deleted) return NotFound();
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}/statement")]
    public async Task<IActionResult> GetStatement(int id, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        try
        {
            var stmt = await _svc.GetStatementAsync(id, from, to);
            return Ok(stmt);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
'@
WF "$apiDir\Controllers\PartiesController.cs" $partiesCtrl

# 4. Register PartyAppService in Program.cs
Write-Host ""
Write-Host "Updating Program.cs..." -ForegroundColor Yellow
 $program = Get-Content "$apiDir\Program.cs" -Raw -Encoding UTF8
if ($program -notmatch 'IPartyAppService') {
    $program = $program -replace 'builder\.Services\.AddScoped<IItemAppService, ItemAppService>\(\);',
        "builder.Services.AddScoped<IItemAppService, ItemAppService>();`nbuilder.Services.AddScoped<IPartyAppService, PartyAppService>();"
    [System.IO.File]::WriteAllText("$apiDir\Program.cs", $program, $utf8)
    Write-Host "  + Techtoon.Api\Program.cs (IPartyAppService registered)" -ForegroundColor Green
} else {
    Write-Host "  (IPartyAppService already registered)" -ForegroundColor DarkGray
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
        Write-Host "=== B-3b BUILD SUCCEEDED ===" -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "=== B-3b BUILD FAILED ===" -ForegroundColor Red
    }
} finally {
    Pop-Location
}