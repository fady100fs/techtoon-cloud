using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Techtoon.Core.Models;
using Techtoon.Data.Context;
using Techtoon.Api.Services;
using Techtoon.Api.Security;

namespace Techtoon.Api.Controllers;

[ApiController]
[Route("api/kits")]
[Authorize]
public class KitsController : ControllerBase
{
    private readonly TechtoonDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public KitsController(TechtoonDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<object>> GetKits([FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] string? search = null)
    {
        var query = _db.Items.Where(i => i.IsKit);
        if (!string.IsNullOrEmpty(search))
            query = query.Where(i => i.Name.Contains(search) || i.Barcode.Contains(search));

        var total = await query.CountAsync();
        var kits = await query
            .OrderByDescending(i => i.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new
            {
                i.Id,
                i.Name,
                i.Barcode,
                SellPrice = i.SellPrice,
                CostPrice = i.CostPrice,
                i.LastCostRecalculation,
                ComponentsCount = _db.KitComponents.Count(kc => kc.KitItemId == i.Id)
            })
            .ToListAsync();

        return Ok(new { items = kits, total, page, pageSize });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetKit(int id)
    {
        var kit = await _db.Items.FirstOrDefaultAsync(i => i.Id == id && i.IsKit);
        if (kit == null) return NotFound();

        var components = await (
            from kc in _db.KitComponents
            join item in _db.Items on kc.ComponentItemId equals item.Id
            where kc.KitItemId == id
            select new
            {
                kc.Id,
                kc.ComponentItemId,
                ComponentName = item.Name,
                ComponentBarcode = item.Barcode,
                kc.Quantity,
                kc.WarehouseId,
                UnitCost = item.CostPrice,
                TotalCost = kc.Quantity * item.CostPrice
            }
        ).ToListAsync();

        var totalCost = components.Sum(c => c.TotalCost);

        return Ok(new
        {
            kit.Id,
            kit.Name,
            kit.Barcode,
            SellPrice = kit.SellPrice,
            CostPrice = kit.CostPrice,
            AvgCostPrice = kit.AvgCostPrice,
            kit.LastCostRecalculation,
            TotalCostCalculated = totalCost,
            Components = components
        });
    }

    [HttpPost]
    [HasPermission("stock_adjustment")]
    public async Task<ActionResult> CreateKit([FromBody] CreateKitDto dto)
    {
        if (string.IsNullOrEmpty(dto.Name))
            return BadRequest(new { message = "اسم الكيت مطلوب" });
        if (dto.Components == null || !dto.Components.Any())
            return BadRequest(new { message = "يجب إضافة مكون واحد على الأقل" });

        decimal totalCost = 0m;
        foreach (var comp in dto.Components)
        {
            var item = await _db.Items.FindAsync(comp.ComponentItemId);
            if (item == null)
                return BadRequest(new { message = $"المكون #{comp.ComponentItemId} غير موجود" });
            if (comp.Quantity <= 0)
                return BadRequest(new { message = "كمية المكون يجب أن تكون موجبة" });
            totalCost += comp.Quantity * item.CostPrice;
        }

        var kit = new Item
        {
            Name = dto.Name,
            Barcode = dto.Barcode ?? $"KIT{DateTime.Now:yyyyMMddHHmmss}",
            SellPrice = dto.SellPrice,
            CostPrice = Math.Round(totalCost, 4),
            AvgCostPrice = Math.Round(totalCost, 4),
            IsKit = true,
            LastCostRecalculation = DateTime.UtcNow
        };

        _db.Items.Add(kit);
        await _db.SaveChangesAsync();

        foreach (var comp in dto.Components)
        {
            _db.KitComponents.Add(new KitComponent
            {
                KitItemId = kit.Id,
                ComponentItemId = comp.ComponentItemId,
                Quantity = comp.Quantity,
                WarehouseId = comp.WarehouseId
            });
        }

        await _db.SaveChangesAsync();
        return Ok(kit);
    }

    [HttpPut("{id}")]
    [HasPermission("stock_adjustment")]
    public async Task<ActionResult> UpdateKit(int id, [FromBody] UpdateKitDto dto)
    {
        var kit = await _db.Items.FirstOrDefaultAsync(i => i.Id == id && i.IsKit);
        if (kit == null) return NotFound();

        var hasInvoices = await _db.InvoiceLines.AnyAsync(il => il.ItemId == id);
        if (hasInvoices)
            return BadRequest(new { message = "لا يمكن تعديل كيت مستخدم في فواتير" });

        kit.Name = dto.Name ?? kit.Name;
        if (dto.SellPrice.HasValue) kit.SellPrice = dto.SellPrice.Value;
        kit.Barcode = dto.Barcode ?? kit.Barcode;

        if (dto.Components != null)
        {
            var existingComps = _db.KitComponents.Where(kc => kc.KitItemId == id);
            _db.KitComponents.RemoveRange(existingComps);

            decimal totalCost = 0m;
            foreach (var comp in dto.Components)
            {
                var item = await _db.Items.FindAsync(comp.ComponentItemId);
                if (item == null)
                    return BadRequest(new { message = $"المكون #{comp.ComponentItemId} غير موجود" });
                totalCost += comp.Quantity * item.CostPrice;

                _db.KitComponents.Add(new KitComponent
                {
                    KitItemId = id,
                    ComponentItemId = comp.ComponentItemId,
                    Quantity = comp.Quantity,
                    WarehouseId = comp.WarehouseId
                });
            }
            kit.CostPrice = Math.Round(totalCost, 4);
            kit.AvgCostPrice = Math.Round(totalCost, 4);
            kit.LastCostRecalculation = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [HasPermission("stock_adjustment")]
    public async Task<ActionResult> DeleteKit(int id)
    {
        var kit = await _db.Items.FirstOrDefaultAsync(i => i.Id == id && i.IsKit);
        if (kit == null) return NotFound();

        var hasInvoices = await _db.InvoiceLines.AnyAsync(il => il.ItemId == id);
        if (hasInvoices)
            return BadRequest(new { message = "لا يمكن حذف كيت مستخدم في فواتير" });

        var components = _db.KitComponents.Where(kc => kc.KitItemId == id);
        _db.KitComponents.RemoveRange(components);
        _db.Items.Remove(kit);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}/cost")]
    public async Task<ActionResult<object>> GetKitCost(int id)
    {
        var kit = await _db.Items.FirstOrDefaultAsync(i => i.Id == id && i.IsKit);
        if (kit == null) return NotFound();

        var components = await (
            from kc in _db.KitComponents
            join item in _db.Items on kc.ComponentItemId equals item.Id
            where kc.KitItemId == id
            select new
            {
                item.Name,
                kc.Quantity,
                CostPrice = item.CostPrice,
                LineTotal = kc.Quantity * item.CostPrice
            }
        ).ToListAsync();

        var totalCost = components.Sum(c => c.LineTotal);

        return Ok(new
        {
            KitId = id,
            KitName = kit.Name,
            StoredCost = kit.CostPrice,
            CalculatedCost = Math.Round(totalCost, 4),
            CostIsStale = kit.CostPrice != Math.Round(totalCost, 4),
            Components = components
        });
    }

    [HttpPost("{id}/recalculate-cost")]
    [HasPermission("stock_adjustment")]
    public async Task<ActionResult> RecalculateKitCost(int id)
    {
        var kit = await _db.Items.FirstOrDefaultAsync(i => i.Id == id && i.IsKit);
        if (kit == null) return NotFound();

        var components = await (
            from kc in _db.KitComponents
            join item in _db.Items on kc.ComponentItemId equals item.Id
            where kc.KitItemId == id
            select new { kc.Quantity, CostPrice = item.CostPrice }
        ).ToListAsync();

        var totalCost = components.Sum(c => (decimal)c.Quantity * (decimal)c.CostPrice);
        kit.CostPrice = Math.Round(totalCost, 4);
        kit.AvgCostPrice = Math.Round(totalCost, 4);
        kit.LastCostRecalculation = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { kit.Id, NewCost = kit.CostPrice, UpdatedAt = kit.LastCostRecalculation });
    }

    [HttpGet("{id}/availability")]
    public async Task<ActionResult<object>> CheckAvailability(
        int id, [FromQuery, Required] decimal quantity, [FromQuery] int? warehouseId = null)
    {
        var kit = await _db.Items.FirstOrDefaultAsync(i => i.Id == id && i.IsKit);
        if (kit == null) return NotFound();

        var components = await (
            from kc in _db.KitComponents
            join item in _db.Items on kc.ComponentItemId equals item.Id
            where kc.KitItemId == id
            select new { kc.ComponentItemId, item.Name, kc.Quantity, kc.WarehouseId }
        ).ToListAsync();

        var shortages = new List<object>();
        foreach (var comp in components)
        {
            var needed = comp.Quantity * quantity;
            decimal available;

            if (comp.WarehouseId.HasValue)
            {
                var stockVal = await _db.StockLevels
                    .Where(sl => sl.WarehouseId == comp.WarehouseId && sl.ItemId == comp.ComponentItemId)
                    .Select(sl => (decimal?)sl.Quantity)
                    .FirstOrDefaultAsync();
                available = stockVal ?? 0;
            }
            else if (warehouseId.HasValue)
            {
                var stockVal = await _db.StockLevels
                    .Where(sl => sl.WarehouseId == warehouseId && sl.ItemId == comp.ComponentItemId)
                    .Select(sl => (decimal?)sl.Quantity)
                    .FirstOrDefaultAsync();
                available = stockVal ?? 0;
            }
            else
            {
                available = (decimal)(await _db.StockLevels
                    .Where(sl => sl.ItemId == comp.ComponentItemId)
                    .SumAsync(sl => (decimal?)sl.Quantity) ?? 0);
            }

            if (available < needed)
            {
                shortages.Add(new
                {
                    ComponentId = comp.ComponentItemId,
                    ComponentName = comp.Name,
                    Needed = needed,
                    Available = available,
                    Shortfall = needed - available
                });
            }
        }

        return Ok(new
        {
            KitId = id,
            RequestedQuantity = quantity,
            WarehouseId = warehouseId,
            Available = !shortages.Any(),
            Shortages = shortages
        });
    }
}

public class CreateKitDto
{
    [Required] public string Name { get; set; } = "";
    public string? Barcode { get; set; }
    [Required] public decimal SellPrice { get; set; }
    [Required] public List<KitComponentDto> Components { get; set; } = new();
}

public class UpdateKitDto
{
    public string? Name { get; set; }
    public string? Barcode { get; set; }
    public decimal? SellPrice { get; set; }
    public List<KitComponentDto>? Components { get; set; }
}

public class KitComponentDto
{
    [Required] public int ComponentItemId { get; set; }
    [Required] public decimal Quantity { get; set; }
    public int? WarehouseId { get; set; }
}


