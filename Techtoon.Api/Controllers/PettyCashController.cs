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
[Route("api/petty-cash")]
[Authorize]
public class PettyCashController : ControllerBase
{
    private readonly TechtoonDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public PettyCashController(TechtoonDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet("custodies")]
    public async Task<ActionResult<object>> ListCustodies(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        [FromQuery] string? status = null, [FromQuery] int? employeeId = null)
    {
        var query = _db.PettyCashCustodies.Include(c => c.Employee).Include(c => c.CashBox).AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(c => c.Status == status);
        if (employeeId.HasValue)
            query = query.Where(c => c.EmployeeId == employeeId);

        var total = await query.CountAsync();
        var custodies = await query
            .OrderByDescending(c => c.IssuedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id,
                c.CustodyNumber,
                EmployeeName = c.Employee != null ? c.Employee.FullName : "",
                c.Amount,
                c.Currency,
                CashBoxName = c.CashBox != null ? c.CashBox.Name : "",
                c.IssuedDate,
                c.ExpectedSettlementDate,
                c.Status,
                c.Description,
                TotalExpenses = _db.PettyCashExpenses.Where(e => e.CustodyId == c.Id && e.Status != "rejected").Sum(e => (double?)e.Amount) ?? 0
            })
            .ToListAsync();

        return Ok(new { items = custodies, total, page, pageSize });
    }

    [HttpGet("custodies/{id}")]
    public async Task<ActionResult<object>> GetCustody(int id)
    {
        var custody = await _db.PettyCashCustodies
            .Include(c => c.Employee)
            .Include(c => c.CashBox)
            .Include(c => c.Expenses)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (custody == null) return NotFound();

        var totalExpenses = custody.Expenses.Where(e => e.Status != "rejected").Sum(e => e.Amount);

        return Ok(new
        {
            custody.Id,
            custody.CustodyNumber,
            EmployeeName = custody.Employee != null ? custody.Employee.FullName : "",
            custody.Amount,
            custody.Currency,
            CashBoxName = custody.CashBox != null ? custody.CashBox.Name : "",
            custody.CustodyAccountId,
            custody.IssuedDate,
            custody.ExpectedSettlementDate,
            custody.Status,
            custody.Description,
            custody.CreatedAt,
            custody.SettledAt,
            TotalExpenses = totalExpenses,
            RemainingAmount = custody.Amount - totalExpenses,
            Expenses = custody.Expenses.Select(e => new
            {
                e.Id,
                e.ExpenseNumber,
                e.ExpenseDate,
                e.Amount,
                e.Description,
                e.VendorName,
                e.ReceiptFilePath,
                e.Status,
                e.RecordedAt
            })
        });
    }

    [HttpPost("custodies")]
    [HasPermission("expenses")]
    public async Task<ActionResult> CreateCustody([FromBody] CreateCustodyDto dto)
    {
        if (dto.Amount <= 0)
            return BadRequest(new { message = "مبلغ العهدة يجب أن يكون موجباً" });

        var employee = await _db.Employees.FindAsync(dto.EmployeeId);
        if (employee == null) return BadRequest(new { message = "الموظف غير موجود" });

        var cashBox = await _db.CashBoxes.FindAsync(dto.CashBoxId);
        if (cashBox == null) return BadRequest(new { message = "الخزينة غير موجودة" });

        var custody = new PettyCashCustody
        {
            EmployeeId = dto.EmployeeId,
            Amount = dto.Amount,
            Currency = dto.Currency ?? "EGP",
            CashBoxId = dto.CashBoxId,
            CustodyAccountId = dto.CustodyAccountId,
            IssuedDate = dto.IssuedDate ?? DateTime.UtcNow,
            ExpectedSettlementDate = dto.ExpectedSettlementDate,
            Status = "open",
            Description = dto.Description,
            CreatedBy = _currentUser.UserId ?? 0
        };

        _db.PettyCashCustodies.Add(custody);
        await _db.SaveChangesAsync();

        var je = new JournalEntry
        {
            Date = DateTime.UtcNow,
            Description = $"صرف عهدة للموظف {employee.FullName}",
            ReferenceType = "custody",
            ReferenceId = custody.Id,
            CreatedBy = _currentUser.UserId ?? 0
        };
        _db.JournalEntries.Add(je);
        await _db.SaveChangesAsync();

        _db.JournalLines.AddRange(
            new JournalLine { EntryId = je.Id, AccountId = dto.CustodyAccountId, Debit = dto.Amount, Credit = 0 },
            new JournalLine { EntryId = je.Id, AccountId = cashBox.AccountId, Debit = 0, Credit = dto.Amount }
        );

        custody.JournalEntryId = je.Id;
        await _db.SaveChangesAsync();

        return Ok(custody);
    }

    [HttpPost("custodies/{custodyId}/expenses")]
    [HasPermission("expenses")]
    public async Task<ActionResult> RecordExpense(int custodyId, [FromBody] CreateExpenseDto dto)
    {
        var custody = await _db.PettyCashCustodies.FindAsync(custodyId);
        if (custody == null) return NotFound();

        if (custody.Status != "open" && custody.Status != "partially_settled")
            return BadRequest(new { message = "لا يمكن تسجيل مصروف على عهدة غير مفتوحة" });

        var totalExpenses = await _db.PettyCashExpenses
            .Where(e => e.CustodyId == custodyId && e.Status != "rejected")
            .SumAsync(e => (double?)e.Amount) ?? 0;
        var available = custody.Amount - totalExpenses;

        if (dto.Amount > available)
            return BadRequest(new { message = $"المبلغ يتجاوز المتاح: متاح {available}" });

        var expense = new PettyCashExpense
        {
            CustodyId = custodyId,
            ExpenseDate = dto.ExpenseDate ?? DateTime.UtcNow,
            Amount = dto.Amount,
            ExpenseCategoryId = dto.ExpenseCategoryId,
            ExpenseAccountId = dto.ExpenseAccountId,
            Description = dto.Description,
            VendorName = dto.VendorName,
            ReceiptFilePath = dto.ReceiptFilePath,
            Status = "pending",
            RecordedBy = _currentUser.UserId ?? 0
        };

        _db.PettyCashExpenses.Add(expense);
        await _db.SaveChangesAsync();

        if (dto.ExpenseAccountId.HasValue)
        {
            var je = new JournalEntry
            {
                Date = DateTime.UtcNow,
                Description = $"مصروف نثري من عهدة #{custodyId}: {dto.Description}",
                ReferenceType = "petty_cash_expense",
                ReferenceId = expense.Id,
                CreatedBy = _currentUser.UserId ?? 0
            };
            _db.JournalEntries.Add(je);
            await _db.SaveChangesAsync();

            _db.JournalLines.AddRange(
                new JournalLine { EntryId = je.Id, AccountId = dto.ExpenseAccountId.Value, Debit = dto.Amount, Credit = 0 },
                new JournalLine { EntryId = je.Id, AccountId = custody.CustodyAccountId, Debit = 0, Credit = dto.Amount }
            );

            expense.JournalEntryId = je.Id;
            expense.Status = "approved";
            expense.ApprovedBy = _currentUser.UserId;
            expense.ApprovedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
        }

        return Ok(expense);
    }

    [HttpPost("custodies/{id}/settle")]
    [HasPermission("expenses")]
    public async Task<ActionResult> SettleCustody(int id, [FromBody] SettleCustodyDto dto)
    {
        var custody = await _db.PettyCashCustodies.Include(c => c.Expenses).FirstOrDefaultAsync(c => c.Id == id);
        if (custody == null) return NotFound();
        if (custody.Status == "settled") return BadRequest(new { message = "العهدة مسواة بالفعل" });

        var cashBox = await _db.CashBoxes.FindAsync(dto.CashBoxId);
        if (cashBox == null) return BadRequest(new { message = "الخزينة غير موجودة" });

        var totalExpenses = custody.Expenses.Where(e => e.Status == "approved").Sum(e => e.Amount);
        var remaining = custody.Amount - totalExpenses;

        if (dto.ReturnedAmount > remaining)
            return BadRequest(new { message = $"المبلغ المُعاد يتجاوز المتبقي: متاح {remaining}" });

        var settlement = new PettyCashSettlement
        {
            CustodyId = id,
            SettlementDate = dto.SettlementDate ?? DateTime.UtcNow,
            TotalExpenses = totalExpenses,
            RemainingAmount = remaining,
            ReturnedAmount = dto.ReturnedAmount,
            ShortageAmount = dto.ShortageAmount,
            ShortageAccountId = dto.ShortageAccountId,
            CashBoxId = dto.CashBoxId,
            Status = "approved",
            ApprovedBy = _currentUser.UserId,
            ApprovedAt = DateTime.UtcNow,
            Notes = dto.Notes
        };

        _db.PettyCashSettlements.Add(settlement);
        await _db.SaveChangesAsync();

        var je = new JournalEntry
        {
            Date = DateTime.UtcNow,
            Description = $"تسوية عهدة #{id}",
            ReferenceType = "custody_settlement",
            ReferenceId = settlement.Id,
            CreatedBy = _currentUser.UserId ?? 0
        };
        _db.JournalEntries.Add(je);
        await _db.SaveChangesAsync();

        _db.JournalLines.Add(new JournalLine { EntryId = je.Id, AccountId = custody.CustodyAccountId, Debit = remaining, Credit = 0 });

        if (dto.ReturnedAmount > 0)
            _db.JournalLines.Add(new JournalLine { EntryId = je.Id, AccountId = cashBox.AccountId, Debit = 0, Credit = dto.ReturnedAmount });

        if (dto.ShortageAmount > 0 && dto.ShortageAccountId.HasValue)
            _db.JournalLines.Add(new JournalLine { EntryId = je.Id, AccountId = dto.ShortageAccountId.Value, Debit = 0, Credit = dto.ShortageAmount });

        settlement.JournalEntryId = je.Id;

        custody.Status = "settled";
        custody.SettledAt = DateTime.UtcNow;
        custody.SettledBy = _currentUser.UserId;

        await _db.SaveChangesAsync();
        return Ok(settlement);
    }

    [HttpGet("reports/open")]
    public async Task<ActionResult<object>> OpenCustodiesReport()
    {
        var today = DateTime.UtcNow;
        var openCustodies = await _db.PettyCashCustodies
            .Include(c => c.Employee)
            .Where(c => c.Status == "open" || c.Status == "partially_settled")
            .Select(c => new
            {
                c.Id,
                c.CustodyNumber,
                EmployeeName = c.Employee != null ? c.Employee.FullName : "",
                c.Amount,
                c.IssuedDate,
                c.ExpectedSettlementDate,
                DaysOpen = (today - c.IssuedDate).Days,
                IsOverdue = c.ExpectedSettlementDate.HasValue && c.ExpectedSettlementDate < today,
                TotalExpenses = _db.PettyCashExpenses.Where(e => e.CustodyId == c.Id && e.Status != "rejected").Sum(e => (double?)e.Amount) ?? 0
            })
            .OrderByDescending(c => c.IsOverdue)
            .ThenBy(c => c.ExpectedSettlementDate)
            .ToListAsync();

        return Ok(new
        {
            total_open = openCustodies.Count,
            total_amount = openCustodies.Sum(c => c.Amount),
            overdue_count = openCustodies.Count(c => c.IsOverdue),
            items = openCustodies
        });
    }
}

public class CreateCustodyDto
{
    [Required] public int EmployeeId { get; set; }
    [Required] public double Amount { get; set; }
    public string? Currency { get; set; }
    [Required] public int CashBoxId { get; set; }
    [Required] public int CustodyAccountId { get; set; }
    public DateTime? IssuedDate { get; set; }
    public DateTime? ExpectedSettlementDate { get; set; }
    public string? Description { get; set; }
}

public class CreateExpenseDto
{
    public DateTime? ExpenseDate { get; set; }
    [Required] public double Amount { get; set; }
    public int? ExpenseCategoryId { get; set; }
    public int? ExpenseAccountId { get; set; }
    [Required] public string Description { get; set; } = "";
    public string? VendorName { get; set; }
    public string? ReceiptFilePath { get; set; }
}

public class SettleCustodyDto
{
    public DateTime? SettlementDate { get; set; }
    [Required] public int CashBoxId { get; set; }
    [Required] public double ReturnedAmount { get; set; }
    public double ShortageAmount { get; set; }
    public int? ShortageAccountId { get; set; }
    public string? Notes { get; set; }
}
