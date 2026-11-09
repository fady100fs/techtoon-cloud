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
[Route("api/bad-debts")]
[Authorize]
public class BadDebtController : ControllerBase
{
    private readonly TechtoonDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public BadDebtController(TechtoonDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<object>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        [FromQuery] string? status = null, [FromQuery] int? partyId = null)
    {
        var query = _db.BadDebtWriteoffs.Include(b => b.Party).AsQueryable();
        if (!string.IsNullOrEmpty(status)) query = query.Where(b => b.Status == status);
        if (partyId.HasValue) query = query.Where(b => b.PartyId == partyId);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(b => b.RequestedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new
            {
                b.Id, b.WriteoffNumber,
                PartyName = b.Party != null ? b.Party.Name : "",
                b.PartyId, b.InvoiceId, b.OriginalAmount, b.RemainingAmount,
                b.WriteoffAmount, b.WriteoffType, b.Reason, b.WriteoffDate,
                b.Status, b.RequestedAt
            })
            .ToListAsync();

        return Ok(new { items, total, page, pageSize });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetById(int id)
    {
        var writeoff = await _db.BadDebtWriteoffs
            .Include(b => b.Party)
            .Include(b => b.Invoice)
            .Include(b => b.Attachments)
            .Include(b => b.Reversals)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (writeoff == null) return NotFound();

        return Ok(new
        {
            writeoff.Id, writeoff.WriteoffNumber,
            PartyName = writeoff.Party != null ? writeoff.Party.Name : "",
            InvoiceNumber = writeoff.Invoice != null ? writeoff.Invoice.InvoiceNumber : "",
            writeoff.OriginalAmount, writeoff.RemainingAmount, writeoff.WriteoffAmount,
            writeoff.WriteoffType, writeoff.Reason, writeoff.WriteoffDate,
            writeoff.BadDebtAccountId, writeoff.ArAccountId, writeoff.JournalEntryId,
            writeoff.Status, writeoff.RequestedBy, writeoff.RequestedAt,
            writeoff.ApprovedBy, writeoff.ApprovedAt, writeoff.PostedAt, writeoff.Notes,
            Attachments = writeoff.Attachments.Select(a => new
            {
                a.Id, a.FileName, a.FilePath, a.FileType, a.FileSize, a.Description, a.UploadedAt
            }),
            Reversals = writeoff.Reversals.Select(r => new
            {
                r.Id, r.ReversalNumber, r.ReversalDate, r.ReversalAmount, r.Reason, r.ReversedAt
            })
        });
    }

    [HttpPost]
    [HasPermission("settings")]
    public async Task<ActionResult> Create([FromBody] CreateBadDebtDto dto)
    {
        if (dto.WriteoffAmount <= 0)
            return BadRequest(new { message = "مبلغ الشطب يجب أن يكون موجباً" });

        var party = await _db.Parties.FindAsync(dto.PartyId);
        if (party == null) return BadRequest(new { message = "الطرف غير موجود" });

        decimal originalAmount = dto.OriginalAmount;
        decimal remainingAmount = dto.RemainingAmount;

        if (dto.InvoiceId.HasValue)
        {
            var invoice = await _db.Invoices.FindAsync(dto.InvoiceId);
            if (invoice == null) return BadRequest(new { message = "الفاتورة غير موجودة" });
            if (invoice.PartyId != dto.PartyId)
                return BadRequest(new { message = "الفاتورة لا تنتمي لهذا الطرف" });

            originalAmount = (decimal)invoice.NetAmount;
            remainingAmount = (decimal)invoice.NetAmount - (decimal)invoice.PaidAmount;
        }

        if (dto.WriteoffAmount > remainingAmount)
            return BadRequest(new { message = $"مبلغ الشطب يتجاوز المتبقي ({remainingAmount})" });

        var writeoff = new BadDebtWriteoff
        {
            PartyId = dto.PartyId, InvoiceId = dto.InvoiceId,
            OriginalAmount = originalAmount, RemainingAmount = remainingAmount,
            WriteoffAmount = dto.WriteoffAmount,
            WriteoffType = dto.WriteoffAmount >= remainingAmount ? "full" : "partial",
            Reason = dto.Reason, EvidenceFilePath = dto.EvidenceFilePath,
            WriteoffDate = dto.WriteoffDate ?? DateTime.UtcNow,
            BadDebtAccountId = dto.BadDebtAccountId, ArAccountId = dto.ArAccountId,
            Status = "pending", RequestedBy = _currentUser.UserId ?? 0,
            Notes = dto.Notes
        };

        _db.BadDebtWriteoffs.Add(writeoff);
        await _db.SaveChangesAsync();
        return Ok(writeoff);
    }

    [HttpPost("{id}/approve")]
    [HasPermission("settings")]
    public async Task<ActionResult> ApproveAndPost(int id)
    {
        var writeoff = await _db.BadDebtWriteoffs.FindAsync(id);
        if (writeoff == null) return NotFound();
        if (writeoff.Status != "pending")
            return BadRequest(new { message = "الطلب ليس بحالة pending" });

        var je = new JournalEntry
        {
            Date = DateTime.UtcNow,
            Description = $"شطب دين معدوم - {writeoff.Reason}",
            ReferenceType = "bad_debt_writeoff",
            ReferenceId = writeoff.Id,
            CreatedBy = _currentUser.UserId ?? 0
        };
        _db.JournalEntries.Add(je);
        await _db.SaveChangesAsync();

        _db.JournalLines.AddRange(
            new JournalLine { EntryId = je.Id, AccountId = writeoff.BadDebtAccountId, Debit = writeoff.WriteoffAmount, Credit = 0 },
            new JournalLine { EntryId = je.Id, AccountId = writeoff.ArAccountId, Debit = 0, Credit = writeoff.WriteoffAmount }
        );

        writeoff.JournalEntryId = je.Id;
        writeoff.Status = "posted";
        writeoff.ApprovedBy = _currentUser.UserId;
        writeoff.ApprovedAt = DateTime.UtcNow;
        writeoff.PostedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/reverse")]
    [HasPermission("settings")]
    public async Task<ActionResult> Reverse(int id, [FromBody] ReverseBadDebtDto dto)
    {
        var writeoff = await _db.BadDebtWriteoffs.FindAsync(id);
        if (writeoff == null) return NotFound();
        if (writeoff.Status != "posted")
            return BadRequest(new { message = "لا يمكن عكس شطب غير مرحّل" });
        if (dto.ReversalAmount > writeoff.WriteoffAmount)
            return BadRequest(new { message = "مبلغ العكس يتجاوز الشطب الأصلي" });

        var je = new JournalEntry
        {
            Date = DateTime.UtcNow,
            Description = $"عكس شطب دين - {dto.Reason}",
            ReferenceType = "bad_debt_reversal",
            ReferenceId = id,
            CreatedBy = _currentUser.UserId ?? 0
        };
        _db.JournalEntries.Add(je);
        await _db.SaveChangesAsync();

        _db.JournalLines.AddRange(
            new JournalLine { EntryId = je.Id, AccountId = writeoff.ArAccountId, Debit = dto.ReversalAmount, Credit = 0 },
            new JournalLine { EntryId = je.Id, AccountId = writeoff.BadDebtAccountId, Debit = 0, Credit = dto.ReversalAmount }
        );

        _db.BadDebtReversals.Add(new BadDebtReversal
        {
            WriteoffId = id,
            ReversalDate = dto.ReversalDate ?? DateTime.UtcNow,
            ReversalAmount = dto.ReversalAmount,
            Reason = dto.Reason,
            JournalEntryId = je.Id,
            ReversedBy = _currentUser.UserId ?? 0
        });

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("report")]
    public async Task<ActionResult<object>> Report([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var query = _db.BadDebtWriteoffs.Where(b => b.Status == "posted");
        if (startDate.HasValue) query = query.Where(b => b.WriteoffDate >= startDate);
        if (endDate.HasValue) query = query.Where(b => b.WriteoffDate <= endDate);

        var writeoffs = await query.Include(b => b.Party).OrderByDescending(b => b.WriteoffDate).ToListAsync();

        return Ok(new
        {
            total_count = writeoffs.Count,
            total_amount = writeoffs.Sum(b => b.WriteoffAmount),
            items = writeoffs.Select(b => new
            {
                b.WriteoffNumber,
                PartyName = b.Party != null ? b.Party.Name : "",
                b.WriteoffAmount, b.WriteoffType, b.Reason, b.WriteoffDate
            })
        });
    }
}

public class CreateBadDebtDto
{
    [Required] public int PartyId { get; set; }
    public int? InvoiceId { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    [Required] public decimal WriteoffAmount { get; set; }
    [Required] public string Reason { get; set; } = "";
    public string? EvidenceFilePath { get; set; }
    public DateTime? WriteoffDate { get; set; }
    [Required] public int BadDebtAccountId { get; set; }
    [Required] public int ArAccountId { get; set; }
    public string? Notes { get; set; }
}

public class ReverseBadDebtDto
{
    [Required] public decimal ReversalAmount { get; set; }
    [Required] public string Reason { get; set; } = "";
    public DateTime? ReversalDate { get; set; }
}
