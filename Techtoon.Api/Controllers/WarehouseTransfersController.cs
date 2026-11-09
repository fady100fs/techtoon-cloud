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
[Route("api/warehouse-transfers")]
[Authorize]
public class WarehouseTransfersController : ControllerBase
{
    private readonly TechtoonDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public WarehouseTransfersController(TechtoonDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<object>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        [FromQuery] string? status = null,
        [FromQuery] int? fromWarehouseId = null,
        [FromQuery] int? toWarehouseId = null)
    {
        var query = _db.WarehouseTransfers.Include(wt => wt.Lines).AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(wt => wt.Status == status);
        if (fromWarehouseId.HasValue)
            query = query.Where(wt => wt.FromWarehouseId == fromWarehouseId);
        if (toWarehouseId.HasValue)
            query = query.Where(wt => wt.ToWarehouseId == toWarehouseId);

        var total = await query.CountAsync();
        var transfers = await query
            .OrderByDescending(wt => wt.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(wt => new
            {
                wt.Id,
                wt.TransferNumber,
                wt.Date,
                wt.FromWarehouseId,
                wt.ToWarehouseId,
                wt.Status,
                LinesCount = wt.Lines.Count,
                TotalQuantity = wt.Lines.Sum(l => l.RequestedQuantity)
            })
            .ToListAsync();

        return Ok(new { items = transfers, total, page, pageSize });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetById(int id)
    {
        var transfer = await _db.WarehouseTransfers
            .Include(wt => wt.Lines)
            .FirstOrDefaultAsync(wt => wt.Id == id);

        if (transfer == null) return NotFound();

        return Ok(new
        {
            transfer.Id,
            transfer.TransferNumber,
            transfer.Date,
            transfer.FromWarehouseId,
            transfer.ToWarehouseId,
            transfer.Status,
            transfer.Notes,
            transfer.ApprovedAt,
            transfer.ShippedAt,
            transfer.ReceivedAt,
            Lines = transfer.Lines.Select(l => new
            {
                l.Id,
                l.ItemId,
                l.RequestedQuantity,
                l.ShippedQuantity,
                l.ReceivedQuantity,
                l.DamagedQuantity,
                l.MissingQuantity,
                l.DifferenceReason
            })
        });
    }

    [HttpPost]
    [HasPermission("stock_adjustment")]
    public async Task<ActionResult> Create([FromBody] CreateTransferDto dto)
    {
        if (dto.FromWarehouseId == dto.ToWarehouseId)
            return BadRequest(new { message = "لا يمكن التحويل بين نفس المخزن!" });
        if (dto.Lines == null || !dto.Lines.Any())
            return BadRequest(new { message = "يجب إضافة صنف واحد على الأقل" });

        var transfer = new WarehouseTransfer
        {
            Date = DateTime.UtcNow,
            FromWarehouseId = dto.FromWarehouseId,
            ToWarehouseId = dto.ToWarehouseId,
            ItemId = dto.Lines.First().ItemId,
            Quantity = dto.Lines.Sum(l => l.RequestedQuantity),
            Notes = dto.Notes,
            Status = "draft"
        };

        _db.WarehouseTransfers.Add(transfer);
        await _db.SaveChangesAsync();

        foreach (var line in dto.Lines)
        {
            _db.WarehouseTransferLines.Add(new WarehouseTransferLine
            {
                TransferId = transfer.Id,
                ItemId = line.ItemId,
                RequestedQuantity = line.RequestedQuantity,
                ShippedQuantity = 0,
                ReceivedQuantity = 0
            });
        }

        _db.WarehouseTransferLogs.Add(new WarehouseTransferLog
        {
            TransferId = transfer.Id,
            Action = "created",
            PerformedBy = _currentUser.UserId ?? 0,
            NewStatus = "draft"
        });

        await _db.SaveChangesAsync();
        return Ok(transfer);
    }

    [HttpPost("{id}/approve")]
    [HasPermission("stock_adjustment")]
    public async Task<ActionResult> Approve(int id)
    {
        var transfer = await _db.WarehouseTransfers.FindAsync(id);
        if (transfer == null) return NotFound();
        if (transfer.Status != "pending_approval" && transfer.Status != "draft")
            return BadRequest(new { message = "يجب أن تكون الحالة draft أو pending_approval" });

        var oldStatus = transfer.Status;
        transfer.Status = "approved";
        transfer.ApprovedBy = _currentUser.UserId ?? 0;
        transfer.ApprovedAt = DateTime.UtcNow;

        _db.WarehouseTransferLogs.Add(new WarehouseTransferLog
        {
            TransferId = id, Action = "approved",
            PerformedBy = _currentUser.UserId ?? 0,
            OldStatus = oldStatus, NewStatus = "approved"
        });

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/ship")]
    [HasPermission("stock_adjustment")]
    public async Task<ActionResult> Ship(int id, [FromBody] ShipTransferDto dto)
    {
        var transfer = await _db.WarehouseTransfers
            .Include(wt => wt.Lines)
            .FirstOrDefaultAsync(wt => wt.Id == id);

        if (transfer == null) return NotFound();
        if (transfer.Status != "approved" && transfer.Status != "shipped")
            return BadRequest(new { message = "يجب اعتماد التحويل أولاً" });

        foreach (var line in transfer.Lines)
        {
            var shippedQty = dto.ShippedQuantities.ContainsKey(line.ItemId) ? dto.ShippedQuantities[line.ItemId] : 0;
            if (shippedQty <= 0) continue;

            var stock = await _db.StockLevels
                .FirstOrDefaultAsync(sl => sl.WarehouseId == transfer.FromWarehouseId && sl.ItemId == line.ItemId);
            var currentQty = stock?.Quantity ?? 0;

            if (currentQty < shippedQty)
            {
                var item = await _db.Items.FindAsync(line.ItemId);
                return BadRequest(new { message = $"الرصيد غير كافٍ للصنف '{item?.Name}': مطلوب {shippedQty}، متاح {currentQty}" });
            }

            if (stock == null)
            {
                _db.StockLevels.Add(new StockLevel { WarehouseId = transfer.FromWarehouseId, ItemId = line.ItemId, Quantity = -shippedQty });
            }
            else
            {
                stock.Quantity -= shippedQty;
            }

            line.ShippedQuantity = shippedQty;

            _db.InventoryMovements.Add(new InventoryMovement
            {
                ItemId = line.ItemId, Date = DateTime.UtcNow, Type = "transfer_out",
                Quantity = shippedQty, ReferenceId = transfer.Id
            });
        }

        transfer.Status = "shipped";
        transfer.ShippedAt = DateTime.UtcNow;

        _db.WarehouseTransferLogs.Add(new WarehouseTransferLog
        {
            TransferId = id, Action = "shipped",
            PerformedBy = _currentUser.UserId ?? 0,
            OldStatus = "approved", NewStatus = "shipped"
        });

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/receive")]
    [HasPermission("stock_adjustment")]
    public async Task<ActionResult> Receive(int id, [FromBody] ReceiveTransferDto dto)
    {
        var transfer = await _db.WarehouseTransfers
            .Include(wt => wt.Lines)
            .FirstOrDefaultAsync(wt => wt.Id == id);

        if (transfer == null) return NotFound();
        if (transfer.Status != "shipped" && transfer.Status != "partially_received")
            return BadRequest(new { message = "يجب شحن التحويل أولاً" });

        bool allFullyReceived = true;
        foreach (var line in transfer.Lines)
        {
            var receivedQty = dto.ReceivedQuantities.ContainsKey(line.ItemId) ? dto.ReceivedQuantities[line.ItemId] : 0;
            if (receivedQty <= 0) { allFullyReceived = false; continue; }

            var stock = await _db.StockLevels
                .FirstOrDefaultAsync(sl => sl.WarehouseId == transfer.ToWarehouseId && sl.ItemId == line.ItemId);
            if (stock == null)
            {
                _db.StockLevels.Add(new StockLevel { WarehouseId = transfer.ToWarehouseId, ItemId = line.ItemId, Quantity = receivedQty });
            }
            else
            {
                stock.Quantity += receivedQty;
            }

            line.ReceivedQuantity = receivedQty;
            line.DamagedQuantity = dto.DamagedQuantities != null && dto.DamagedQuantities.ContainsKey(line.ItemId) ? dto.DamagedQuantities[line.ItemId] : 0;

            if (receivedQty < line.ShippedQuantity)
                allFullyReceived = false;

            _db.InventoryMovements.Add(new InventoryMovement
            {
                ItemId = line.ItemId, Date = DateTime.UtcNow, Type = "transfer_in",
                Quantity = receivedQty, ReferenceId = transfer.Id
            });
        }

        var oldStatus = transfer.Status;
        transfer.Status = allFullyReceived ? "fully_received" : "partially_received";
        transfer.ReceivedAt = DateTime.UtcNow;

        _db.WarehouseTransferLogs.Add(new WarehouseTransferLog
        {
            TransferId = id, Action = transfer.Status,
            PerformedBy = _currentUser.UserId ?? 0,
            OldStatus = oldStatus, NewStatus = transfer.Status
        });

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/cancel")]
    [HasPermission("stock_adjustment")]
    public async Task<ActionResult> Cancel(int id, [FromBody] CancelDto dto)
    {
        var transfer = await _db.WarehouseTransfers.FindAsync(id);
        if (transfer == null) return NotFound();

        if (transfer.Status == "shipped" || transfer.Status == "partially_received" || transfer.Status == "fully_received")
            return BadRequest(new { message = "لا يمكن إلغاء تحويل تم شحنه" });

        var oldStatus = transfer.Status;
        transfer.Status = "cancelled";
        transfer.CancelledAt = DateTime.UtcNow;
        transfer.CancelledBy = _currentUser.UserId ?? 0;
        transfer.CancellationReason = dto.Reason;

        _db.WarehouseTransferLogs.Add(new WarehouseTransferLog
        {
            TransferId = id, Action = "cancelled",
            PerformedBy = _currentUser.UserId ?? 0,
            OldStatus = oldStatus, NewStatus = "cancelled",
            Notes = dto.Reason
        });

        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public class CreateTransferDto
{
    [Required] public int FromWarehouseId { get; set; }
    [Required] public int ToWarehouseId { get; set; }
    [Required] public List<TransferLineDto> Lines { get; set; } = new();
    public string? Notes { get; set; }
}

public class TransferLineDto
{
    [Required] public int ItemId { get; set; }
    [Required] public double RequestedQuantity { get; set; }
}

public class ShipTransferDto
{
    public Dictionary<int, double> ShippedQuantities { get; set; } = new();
}

public class ReceiveTransferDto
{
    public Dictionary<int, double> ReceivedQuantities { get; set; } = new();
    public Dictionary<int, double>? DamagedQuantities { get; set; }
}

public class CancelDto
{
    [Required] public string Reason { get; set; } = "";
}
