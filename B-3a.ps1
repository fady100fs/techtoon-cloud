 $ErrorActionPreference = "Stop"
 $root = "D:\PROG\Accounting Prog\Techtoon Accounting C+"
 $apiDir = Join-Path $root "Techtoon.Api"
 $utf8 = New-Object System.Text.UTF8Encoding($false)

function WF($path, $content) {
    [System.IO.File]::WriteAllText($path, $content, $utf8)
    $rel = $path.Replace($root + '\', '')
    Write-Host "  + $rel" -ForegroundColor Green
}

Write-Host "=== B-3a: Items Controller + DTOs + Service ===" -ForegroundColor Cyan
Write-Host ""

# 1. DTOs
Write-Host "[1/4] Creating Item DTOs..." -ForegroundColor Yellow

 $itemListDto = @'
namespace Techtoon.Api.DTOs.Items;

public class ItemListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal SellPrice { get; set; }
    public decimal CostPrice { get; set; }
    public decimal AvgCostPrice { get; set; }
    public decimal OpeningQuantity { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal MinStock { get; set; }
    public bool IsKit { get; set; }
    public int? CategoryId { get; set; }
}
'@
WF "$apiDir\DTOs\Items\ItemListDto.cs" $itemListDto

 $itemDetailDto = @'
namespace Techtoon.Api.DTOs.Items;

public class ItemDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal SellPrice { get; set; }
    public decimal CostPrice { get; set; }
    public decimal AvgCostPrice { get; set; }
    public decimal OpeningQuantity { get; set; }
    public decimal MinStock { get; set; }
    public bool IsKit { get; set; }
    public int? CategoryId { get; set; }
    public int? InventoryAccountId { get; set; }
    public int? CogsAccountId { get; set; }
    public int? RevenueAccountId { get; set; }
    public decimal TotalPurchasedQty { get; set; }
    public decimal CurrentStock { get; set; }
}
'@
WF "$apiDir\DTOs\Items\ItemDetailDto.cs" $itemDetailDto

 $itemCreateDto = @'
namespace Techtoon.Api.DTOs.Items;

public class ItemCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal SellPrice { get; set; }
    public decimal CostPrice { get; set; }
    public decimal OpeningQuantity { get; set; }
    public decimal MinStock { get; set; }
    public bool IsKit { get; set; }
    public int? CategoryId { get; set; }
}
'@
WF "$apiDir\DTOs\Items\ItemCreateDto.cs" $itemCreateDto

 $itemUpdateDto = @'
namespace Techtoon.Api.DTOs.Items;

public class ItemUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal SellPrice { get; set; }
    public decimal CostPrice { get; set; }
    public decimal MinStock { get; set; }
    public int? CategoryId { get; set; }
}
'@
WF "$apiDir\DTOs\Items\ItemUpdateDto.cs" $itemUpdateDto

# 2. ItemAppService
Write-Host "[2/4] Creating ItemAppService..." -ForegroundColor Yellow
 $itemAppSvc = @'
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Techtoon.Api.DTOs.Common;
using Techtoon.Api.DTOs.Items;

namespace Techtoon.Api.Services;

public interface IItemAppService
{
    Task<PagedResultDto<ItemListDto>> GetItemsAsync(string? search, string? barcode, int? categoryId, int page, int pageSize);
    Task<ItemDetailDto?> GetItemAsync(int id);
    Task<ItemDetailDto> CreateItemAsync(ItemCreateDto dto, int userId);
    Task<ItemDetailDto?> UpdateItemAsync(int id, ItemUpdateDto dto, int userId);
    Task<bool> DeleteItemAsync(int id, int userId);
    Task<decimal> GetCurrentStockAsync(int itemId);
    Task<List<ItemListDto>> GetLowStockItemsAsync();
}

public class ItemAppService : IItemAppService
{
    private readonly string _connStr;

    public ItemAppService(IConfiguration config)
    {
        _connStr = config.GetConnectionString("DefaultConnection") ?? "";
    }

    public async Task<PagedResultDto<ItemListDto>> GetItemsAsync(string? search, string? barcode, int? categoryId, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 50;

        var result = new PagedResultDto<ItemListDto> { Page = page, PageSize = pageSize };
        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();

        var where = new List<string>();
        var parms = new List<NpgsqlParameter>();
        if (!string.IsNullOrEmpty(search))
        {
            where.Add("(i.name ILIKE @s OR i.barcode ILIKE @s)");
            parms.Add(new NpgsqlParameter("s", "%" + search + "%"));
        }
        if (!string.IsNullOrEmpty(barcode))
        {
            where.Add("i.barcode = @b");
            parms.Add(new NpgsqlParameter("b", barcode));
        }
        if (categoryId.HasValue)
        {
            where.Add("i.category_id = @c");
            parms.Add(new NpgsqlParameter("c", categoryId.Value));
        }
        var whereSql = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";

        // Count
        using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM items i " + whereSql, conn))
        {
            foreach (var p in parms) cmd.Parameters.Add(p);
            result.TotalCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        // Items
        var sql = @"
            SELECT i.id, i.name, i.barcode, i.sell_price, i.cost_price, i.avg_cost_price,
                   i.opening_quantity, i.min_stock, i.is_kit, i.category_id, i.total_purchased_qty,
                   COALESCE((SELECT SUM(quantity) FROM inventory_movements WHERE item_id = i.id), 0) AS movement_sum
            FROM items i " + whereSql + @"
            ORDER BY i.name
            LIMIT @limit OFFSET @offset";
        using (var cmd = new NpgsqlCommand(sql, conn))
        {
            foreach (var p in parms) cmd.Parameters.Add(p);
            cmd.Parameters.AddWithValue("limit", pageSize);
            cmd.Parameters.AddWithValue("offset", (page - 1) * pageSize);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                var opening = r.IsDBNull(6) ? 0m : r.GetDecimal(6);
                var movement = r.IsDBNull(11) ? 0m : r.GetDecimal(11);
                result.Items.Add(new ItemListDto
                {
                    Id = r.GetInt32(0),
                    Name = r.GetString(1),
                    Barcode = r.IsDBNull(2) ? null : r.GetString(2),
                    SellPrice = r.IsDBNull(3) ? 0m : r.GetDecimal(3),
                    CostPrice = r.IsDBNull(4) ? 0m : r.GetDecimal(4),
                    AvgCostPrice = r.IsDBNull(5) ? 0m : r.GetDecimal(5),
                    OpeningQuantity = opening,
                    CurrentStock = opening + movement,
                    MinStock = r.IsDBNull(7) ? 0m : r.GetDecimal(7),
                    IsKit = r.IsDBNull(8) ? false : r.GetBoolean(8),
                    CategoryId = r.IsDBNull(9) ? null : r.GetInt32(9)
                });
            }
        }
        return result;
    }

    public async Task<ItemDetailDto?> GetItemAsync(int id)
    {
        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        var sql = @"
            SELECT i.id, i.name, i.barcode, i.sell_price, i.cost_price, i.avg_cost_price,
                   i.opening_quantity, i.min_stock, i.is_kit, i.category_id,
                   i.inventory_account_id, i.cogs_account_id, i.revenue_account_id, i.total_purchased_qty,
                   COALESCE((SELECT SUM(quantity) FROM inventory_movements WHERE item_id = i.id), 0)
            FROM items i WHERE i.id = @id";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", id);
        using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        var opening = r.IsDBNull(6) ? 0m : r.GetDecimal(6);
        var movement = r.IsDBNull(14) ? 0m : r.GetDecimal(14);
        return new ItemDetailDto
        {
            Id = r.GetInt32(0),
            Name = r.GetString(1),
            Barcode = r.IsDBNull(2) ? null : r.GetString(2),
            SellPrice = r.IsDBNull(3) ? 0m : r.GetDecimal(3),
            CostPrice = r.IsDBNull(4) ? 0m : r.GetDecimal(4),
            AvgCostPrice = r.IsDBNull(5) ? 0m : r.GetDecimal(5),
            OpeningQuantity = opening,
            MinStock = r.IsDBNull(7) ? 0m : r.GetDecimal(7),
            IsKit = r.IsDBNull(8) ? false : r.GetBoolean(8),
            CategoryId = r.IsDBNull(9) ? null : r.GetInt32(9),
            InventoryAccountId = r.IsDBNull(10) ? null : r.GetInt32(10),
            CogsAccountId = r.IsDBNull(11) ? null : r.GetInt32(11),
            RevenueAccountId = r.IsDBNull(12) ? null : r.GetInt32(12),
            TotalPurchasedQty = r.IsDBNull(13) ? 0m : r.GetDecimal(13),
            CurrentStock = opening + movement
        };
    }

    public async Task<ItemDetailDto> CreateItemAsync(ItemCreateDto dto, int userId)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Item name is required");

        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        int newId;
        using (var cmd = new NpgsqlCommand(
            @"INSERT INTO items (name, barcode, sell_price, cost_price, avg_cost_price, opening_quantity, min_stock, is_kit, category_id, total_purchased_qty)
              VALUES (@name, @barcode, @sell, @cost, @cost, @opening, @min, @kit, @cat, 0) RETURNING id", conn, tx))
        {
            cmd.Parameters.AddWithValue("name", dto.Name);
            cmd.Parameters.AddWithValue("barcode", (object?)dto.Barcode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("sell", dto.SellPrice);
            cmd.Parameters.AddWithValue("cost", dto.CostPrice);
            cmd.Parameters.AddWithValue("opening", dto.OpeningQuantity);
            cmd.Parameters.AddWithValue("min", dto.MinStock);
            cmd.Parameters.AddWithValue("kit", dto.IsKit);
            cmd.Parameters.AddWithValue("cat", (object?)dto.CategoryId ?? DBNull.Value);
            newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        // Record opening balance as inventory movement
        if (dto.OpeningQuantity != 0)
        {
            using var cmd = new NpgsqlCommand(
                @"INSERT INTO inventory_movements (item_id, date, type, quantity, reference_type, created_at)
                  VALUES (@itemId, NOW(), 'opening', @qty, 'manual', NOW())", conn, tx);
            cmd.Parameters.AddWithValue("itemId", newId);
            cmd.Parameters.AddWithValue("qty", dto.OpeningQuantity);
            await cmd.ExecuteNonQueryAsync();
        }

        await LogAuditAsync(conn, tx, userId, "create_item", "item", newId, "Created item: " + dto.Name);
        await tx.CommitAsync();

        return (await GetItemAsync(newId))!;
    }

    public async Task<ItemDetailDto?> UpdateItemAsync(int id, ItemUpdateDto dto, int userId)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Item name is required");

        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        using (var cmd = new NpgsqlCommand(
            @"UPDATE items SET name=@name, barcode=@barcode, sell_price=@sell, cost_price=@cost,
                min_stock=@min, category_id=@cat WHERE id=@id", conn, tx))
        {
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("name", dto.Name);
            cmd.Parameters.AddWithValue("barcode", (object?)dto.Barcode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("sell", dto.SellPrice);
            cmd.Parameters.AddWithValue("cost", dto.CostPrice);
            cmd.Parameters.AddWithValue("min", dto.MinStock);
            cmd.Parameters.AddWithValue("cat", (object?)dto.CategoryId ?? DBNull.Value);
            var affected = await cmd.ExecuteNonQueryAsync();
            if (affected == 0)
            {
                await tx.RollbackAsync();
                return null;
            }
        }

        await LogAuditAsync(conn, tx, userId, "update_item", "item", id, "Updated item: " + dto.Name);
        await tx.CommitAsync();

        return await GetItemAsync(id);
    }

    public async Task<bool> DeleteItemAsync(int id, int userId)
    {
        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        // Check if item has any movements (sold/purchased)
        using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM inventory_movements WHERE item_id=@id AND type NOT IN ('opening')", conn, tx))
        {
            cmd.Parameters.AddWithValue("id", id);
            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            if (count > 0)
                throw new InvalidOperationException("Cannot delete item with existing transactions. Consider disabling it instead.");
        }

        using (var cmd = new NpgsqlCommand("DELETE FROM inventory_movements WHERE item_id=@id", conn, tx))
        {
            cmd.Parameters.AddWithValue("id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        using (var cmd = new NpgsqlCommand("DELETE FROM items WHERE id=@id", conn, tx))
        {
            cmd.Parameters.AddWithValue("id", id);
            var affected = await cmd.ExecuteNonQueryAsync();
            if (affected == 0)
            {
                await tx.RollbackAsync();
                return false;
            }
        }

        await LogAuditAsync(conn, tx, userId, "delete_item", "item", id, "Deleted item id " + id);
        await tx.CommitAsync();
        return true;
    }

    public async Task<decimal> GetCurrentStockAsync(int itemId)
    {
        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        using var cmd = new NpgsqlCommand(
            @"SELECT COALESCE(opening_quantity, 0) + COALESCE((SELECT SUM(quantity) FROM inventory_movements WHERE item_id = @id), 0)
              FROM items WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("id", itemId);
        var result = await cmd.ExecuteScalarAsync();
        return result == null ? 0m : Convert.ToDecimal(result);
    }

    public async Task<List<ItemListDto>> GetLowStockItemsAsync()
    {
        var list = new List<ItemListDto>();
        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        using var cmd = new NpgsqlCommand(
            @"SELECT i.id, i.name, i.barcode, i.sell_price, i.cost_price, i.avg_cost_price,
                     i.opening_quantity, i.min_stock, i.is_kit, i.category_id,
                     COALESCE((SELECT SUM(quantity) FROM inventory_movements WHERE item_id = i.id), 0) AS movement_sum
              FROM items i
              WHERE i.min_stock > 0
                AND (COALESCE(i.opening_quantity, 0) + COALESCE((SELECT SUM(quantity) FROM inventory_movements WHERE item_id = i.id), 0)) <= i.min_stock
              ORDER BY i.name", conn);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            var opening = r.IsDBNull(6) ? 0m : r.GetDecimal(6);
            var movement = r.IsDBNull(10) ? 0m : r.GetDecimal(10);
            list.Add(new ItemListDto
            {
                Id = r.GetInt32(0),
                Name = r.GetString(1),
                Barcode = r.IsDBNull(2) ? null : r.GetString(2),
                SellPrice = r.IsDBNull(3) ? 0m : r.GetDecimal(3),
                CostPrice = r.IsDBNull(4) ? 0m : r.GetDecimal(4),
                AvgCostPrice = r.IsDBNull(5) ? 0m : r.GetDecimal(5),
                OpeningQuantity = opening,
                CurrentStock = opening + movement,
                MinStock = r.IsDBNull(7) ? 0m : r.GetDecimal(7),
                IsKit = r.IsDBNull(8) ? false : r.GetBoolean(8),
                CategoryId = r.IsDBNull(9) ? null : r.GetInt32(9)
            });
        }
        return list;
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
WF "$apiDir\Services\ItemAppService.cs" $itemAppSvc

# 3. ItemsController
Write-Host "[3/4] Creating ItemsController..." -ForegroundColor Yellow
 $itemsCtrl = @'
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Techtoon.Api.DTOs.Items;
using Techtoon.Api.Services;

namespace Techtoon.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ItemsController : ControllerBase
{
    private readonly IItemAppService _svc;
    private readonly ICurrentUserService _current;

    public ItemsController(IItemAppService svc, ICurrentUserService current)
    {
        _svc = svc;
        _current = current;
    }

    [HttpGet]
    public async Task<IActionResult> GetItems(
        [FromQuery] string? search,
        [FromQuery] string? barcode,
        [FromQuery] int? categoryId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _svc.GetItemsAsync(search, barcode, categoryId, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetItem(int id)
    {
        var item = await _svc.GetItemAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> CreateItem([FromBody] ItemCreateDto dto)
    {
        if (_current.UserId == null) return Unauthorized();
        var created = await _svc.CreateItemAsync(dto, _current.UserId.Value);
        return CreatedAtAction(nameof(GetItem), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateItem(int id, [FromBody] ItemUpdateDto dto)
    {
        if (_current.UserId == null) return Unauthorized();
        var updated = await _svc.UpdateItemAsync(id, dto, _current.UserId.Value);
        if (updated == null) return NotFound();
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteItem(int id)
    {
        if (_current.UserId == null) return Unauthorized();
        try
        {
            var deleted = await _svc.DeleteItemAsync(id, _current.UserId.Value);
            if (!deleted) return NotFound();
            return NoContent();
        }
        catch (System.InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}/stock")]
    public async Task<IActionResult> GetStock(int id)
    {
        var stock = await _svc.GetCurrentStockAsync(id);
        return Ok(new { itemId = id, currentStock = stock });
    }

    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStock()
    {
        var items = await _svc.GetLowStockItemsAsync();
        return Ok(items);
    }
}
'@
WF "$apiDir\Controllers\ItemsController.cs" $itemsCtrl

# 4. Register ItemAppService in Program.cs
Write-Host "[4/4] Updating Program.cs..." -ForegroundColor Yellow
 $program = Get-Content "$apiDir\Program.cs" -Raw -Encoding UTF8
if ($program -notmatch 'IItemAppService') {
    $program = $program -replace 'builder\.Services\.AddSingleton<IJwtTokenService, JwtTokenService>\(\);',
        "builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();`nbuilder.Services.AddScoped<IItemAppService, ItemAppService>();"
    [System.IO.File]::WriteAllText("$apiDir\Program.cs", $program, $utf8)
    Write-Host "  + Techtoon.Api\Program.cs (IItemAppService registered)" -ForegroundColor Green
} else {
    Write-Host "  (IItemAppService already registered)" -ForegroundColor DarkGray
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
        Write-Host "=== B-3a BUILD SUCCEEDED ===" -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "=== B-3a BUILD FAILED ===" -ForegroundColor Red
    }
} finally {
    Pop-Location
}