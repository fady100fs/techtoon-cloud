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
[Route("api/recurring-journals")]
[Authorize]
public class RecurringJournalController : ControllerBase
{
    private readonly TechtoonDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public RecurringJournalController(TechtoonDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<object>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        [FromQuery] bool? isActive = null, [FromQuery] string? frequency = null)
    {
        var query = _db.RecurringJournalTemplates.AsQueryable();
        if (isActive.HasValue) query = query.Where(t => t.IsActive == isActive);
        if (!string.IsNullOrEmpty(frequency)) query = query.Where(t => t.Frequency == frequency);

        var total = await query.CountAsync();
        var templates = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                t.Id, t.TemplateNumber, t.Name, t.Frequency, t.ExecutionDay,
                t.StartDate, t.EndDate, t.IsActive, t.AutoPost, t.NextRunDate,
                t.LastRunDate, t.LastRunStatus,
                LinesCount = _db.RecurringJournalLines.Count(l => l.TemplateId == t.Id)
            })
            .ToListAsync();

        return Ok(new { items = templates, total, page, pageSize });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetById(int id)
    {
        var template = await _db.RecurringJournalTemplates
            .Include(t => t.Lines)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (template == null) return NotFound();

        return Ok(new
        {
            template.Id, template.TemplateNumber, template.Name, template.Description,
            template.Frequency, template.CustomDays, template.ExecutionDay,
            template.StartDate, template.EndDate, template.IsActive, template.AutoPost,
            template.NextRunDate, template.LastRunDate, template.LastRunStatus,
            template.LastRunError, template.DescriptionTemplate,
            Lines = template.Lines.OrderBy(l => l.LineNumber).Select(l => new
            {
                l.Id, l.LineNumber, l.AccountId, l.Debit, l.Credit, l.Description,
                l.CostCenterId, l.AmountRule, l.RuleParameters
            })
        });
    }

    [HttpPost]
    [HasPermission("settings")]
    public async Task<ActionResult> Create([FromBody] CreateRecurringJournalDto dto)
    {
        if (dto.Lines == null || !dto.Lines.Any())
            return BadRequest(new { message = "يجب إضافة بند واحد على الأقل" });

        var totalDebit = dto.Lines.Sum(l => l.Debit);
        var totalCredit = dto.Lines.Sum(l => l.Credit);
        if (totalDebit != totalCredit)
            return BadRequest(new { message = $"القالب غير متوازن — مدين: {totalDebit}، دائن: {totalCredit}" });
        if (totalDebit == 0)
            return BadRequest(new { message = "القالب فارغ — يجب أن يحتوي على مبالغ" });

        var nextRun = CalculateNextRun(dto.Frequency, dto.StartDate, dto.ExecutionDay, dto.CustomDays);

        var template = new RecurringJournalTemplate
        {
            Name = dto.Name, Description = dto.Description, Frequency = dto.Frequency,
            CustomDays = dto.CustomDays, ExecutionDay = dto.ExecutionDay,
            StartDate = dto.StartDate, EndDate = dto.EndDate,
            IsActive = true, AutoPost = dto.AutoPost, NextRunDate = nextRun,
            CreatedBy = _currentUser.UserId ?? 0
        };

        _db.RecurringJournalTemplates.Add(template);
        await _db.SaveChangesAsync();

        for (int i = 0; i < dto.Lines.Count; i++)
        {
            var line = dto.Lines[i];
            _db.RecurringJournalLines.Add(new RecurringJournalLine
            {
                TemplateId = template.Id, LineNumber = i + 1,
                AccountId = line.AccountId, Debit = line.Debit, Credit = line.Credit,
                Description = line.Description, CostCenterId = line.CostCenterId,
                AmountRule = line.AmountRule ?? "fixed",
                RuleParameters = line.RuleParameters != null ? System.Text.Json.JsonSerializer.Serialize(line.RuleParameters) : null
            });
        }

        await _db.SaveChangesAsync();
        return Ok(template);
    }

    [HttpPost("{id}/run")]
    [HasPermission("settings")]
    public async Task<ActionResult> Run(int id, [FromQuery] DateTime? runDate = null)
    {
        var template = await _db.RecurringJournalTemplates.Include(t => t.Lines).FirstOrDefaultAsync(t => t.Id == id);
        if (template == null) return NotFound();
        if (!template.IsActive) return BadRequest(new { message = "القالب معطّل" });

        var date = runDate ?? DateTime.UtcNow;

        var existingExecution = await _db.RecurringJournalExecutions
            .FirstOrDefaultAsync(e => e.TemplateId == id && e.PlannedRunDate == date && e.Status == "success");
        if (existingExecution != null)
            return BadRequest(new { message = $"تم تنفيذ القالب بالفعل في {date:yyyy-MM-dd}" });

        var je = new JournalEntry
        {
            Date = date,
            Description = template.DescriptionTemplate ?? $"قيد دوري: {template.Name}",
            ReferenceType = "recurring_journal",
            ReferenceId = template.Id,
            CreatedBy = _currentUser.UserId ?? 0
        };
        _db.JournalEntries.Add(je);
        await _db.SaveChangesAsync();

        decimal totalDebit = 0, totalCredit = 0m;
        foreach (var line in template.Lines.OrderBy(l => l.LineNumber))
        {
            _db.JournalLines.Add(new JournalLine
            {
                EntryId = je.Id, AccountId = line.AccountId,
                Debit = line.Debit, Credit = line.Credit,
                Description = line.Description
            });
            totalDebit += line.Debit;
            totalCredit += line.Credit;
        }

        _db.RecurringJournalExecutions.Add(new RecurringJournalExecution
        {
            TemplateId = id, ExecutionDate = date, PlannedRunDate = date,
            JournalEntryId = je.Id, Status = "success",
            TotalDebit = totalDebit, TotalCredit = totalCredit,
            ExecutedBy = _currentUser.UserId
        });

        template.LastRunDate = DateTime.UtcNow;
        template.LastRunStatus = "success";
        template.NextRunDate = CalculateNextRun(template.Frequency, date, template.ExecutionDay, template.CustomDays);

        await _db.SaveChangesAsync();
        return Ok(new { message = "تم تنفيذ القالب بنجاح", journal_entry_id = je.Id });
    }

    [HttpPost("{id}/pause")]
    [HasPermission("settings")]
    public async Task<ActionResult> Pause(int id)
    {
        var template = await _db.RecurringJournalTemplates.FindAsync(id);
        if (template == null) return NotFound();
        template.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/resume")]
    [HasPermission("settings")]
    public async Task<ActionResult> Resume(int id)
    {
        var template = await _db.RecurringJournalTemplates.FindAsync(id);
        if (template == null) return NotFound();
        template.IsActive = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("due")]
    public async Task<ActionResult<object>> GetDue()
    {
        var today = DateTime.UtcNow;
        var due = await _db.RecurringJournalTemplates
            .Where(t => t.IsActive && t.NextRunDate <= today)
            .Select(t => new
            {
                t.Id, t.Name, t.Frequency, t.NextRunDate,
                DaysOverdue = (today - t.NextRunDate).Days
            })
            .ToListAsync();

        return Ok(new { count = due.Count, items = due });
    }

    private DateTime CalculateNextRun(string frequency, DateTime current, int? executionDay, int? customDays)
    {
        switch (frequency)
        {
            case "daily": return current.AddDays(1);
            case "weekly": return current.AddDays(7);
            case "monthly":
                {
                    var next = current.AddMonths(1);
                    var day = executionDay ?? current.Day;
                    var daysInMonth = DateTime.DaysInMonth(next.Year, next.Month);
                    return day > daysInMonth ? new DateTime(next.Year, next.Month, daysInMonth) : new DateTime(next.Year, next.Month, day);
                }
            case "quarterly": return current.AddMonths(3);
            case "semiannual": return current.AddMonths(6);
            case "annual": return current.AddYears(1);
            case "custom": return current.AddDays(customDays ?? 30);
            default: return current.AddDays(30);
        }
    }
}

public class CreateRecurringJournalDto
{
    [Required] public string Name { get; set; } = "";
    public string? Description { get; set; }
    [Required] public string Frequency { get; set; } = "monthly";
    public int? CustomDays { get; set; }
    public int? ExecutionDay { get; set; }
    [Required] public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool AutoPost { get; set; } = false;
    [Required] public List<RecurringLineDto> Lines { get; set; } = new();
}

public class RecurringLineDto
{
    [Required] public int AccountId { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Description { get; set; }
    public int? CostCenterId { get; set; }
    public string? AmountRule { get; set; }
    public object? RuleParameters { get; set; }
}
