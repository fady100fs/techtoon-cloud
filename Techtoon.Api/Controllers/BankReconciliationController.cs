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
[Route("api/bank-reconciliation")]
[Authorize]
public class BankReconciliationController : ControllerBase
{
    private readonly TechtoonDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public BankReconciliationController(TechtoonDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet("statements")]
    public async Task<ActionResult<object>> ListStatements(
        [FromQuery] int? cashBoxId, [FromQuery] string? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var query = _db.BankStatements.Include(bs => bs.CashBox).AsQueryable();
        if (cashBoxId.HasValue) query = query.Where(bs => bs.CashBoxId == cashBoxId);
        if (!string.IsNullOrEmpty(status)) query = query.Where(bs => bs.Status == status);

        var total = await query.CountAsync();
        var statements = await query
            .OrderByDescending(bs => bs.ImportedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(bs => new
            {
                bs.Id,
                CashBoxName = bs.CashBox != null ? bs.CashBox.Name : "",
                bs.PeriodStart, bs.PeriodEnd,
                bs.OpeningBalance, bs.ClosingBalance,
                bs.Currency, bs.Status, bs.ImportedAt,
                LinesCount = _db.BankStatementLines.Count(l => l.StatementId == bs.Id),
                MatchedCount = _db.BankStatementLines.Count(l => l.StatementId == bs.Id && l.MatchStatus != "unmatched")
            })
            .ToListAsync();

        return Ok(new { items = statements, total, page, pageSize });
    }

    [HttpGet("statements/{id}/lines")]
    public async Task<ActionResult<object>> GetStatementLines(int id, [FromQuery] string? matchStatus)
    {
        var query = _db.BankStatementLines.Where(l => l.StatementId == id);
        if (!string.IsNullOrEmpty(matchStatus))
            query = query.Where(l => l.MatchStatus == matchStatus);

        var lines = await query.OrderBy(l => l.LineNumber).ToListAsync();
        return Ok(lines);
    }

    [HttpPost("reconciliations")]
    [HasPermission("settings")]
    public async Task<ActionResult> CreateReconciliation([FromBody] CreateReconciliationDto dto)
    {
        var existing = await _db.BankReconciliations.FirstOrDefaultAsync(r =>
            r.CashBoxId == dto.CashBoxId && r.PeriodStart == dto.PeriodStart && r.PeriodEnd == dto.PeriodEnd);
        if (existing != null)
            return BadRequest(new { message = "توجد تسوية سابقة لهذه الفترة" });

        var difference = dto.ClosingBookBalance - dto.ClosingBankBalance;

        var reconciliation = new BankReconciliation
        {
            CashBoxId = dto.CashBoxId,
            PeriodStart = dto.PeriodStart,
            PeriodEnd = dto.PeriodEnd,
            BankStatementId = dto.BankStatementId,
            OpeningBookBalance = dto.OpeningBookBalance,
            OpeningBankBalance = dto.OpeningBankBalance,
            ClosingBookBalance = dto.ClosingBookBalance,
            ClosingBankBalance = dto.ClosingBankBalance,
            Difference = difference,
            Status = "draft",
            CreatedBy = _currentUser.UserId ?? 0
        };

        _db.BankReconciliations.Add(reconciliation);
        await _db.SaveChangesAsync();
        return Ok(reconciliation);
    }

    [HttpPost("reconciliations/{id}/auto-match")]
    [HasPermission("settings")]
    public async Task<ActionResult> AutoMatch(int id, [FromQuery] int dateTolerance = 3, [FromQuery] decimal amountTolerance =  0.01m)
    {
        var reconciliation = await _db.BankReconciliations.FindAsync(id);
        if (reconciliation == null) return NotFound();
        if (reconciliation.Status == "approved" || reconciliation.Status == "locked")
            return BadRequest(new { message = "لا يمكن مطابقة تسوية معتمدة" });

        var bankLines = await _db.BankStatementLines
            .Include(l => l.Statement)
            .Where(l => l.Statement.CashBoxId == reconciliation.CashBoxId
                     && l.TransactionDate >= reconciliation.PeriodStart
                     && l.TransactionDate <= reconciliation.PeriodEnd
                     && l.MatchStatus == "unmatched")
            .ToListAsync();

        var matchedPaymentIds = await _db.ReconciliationMatches
            .Where(m => m.ReconciliationId == id && m.SystemPaymentId.HasValue)
            .Select(m => m.SystemPaymentId.Value).ToListAsync();

        var payments = await _db.Payments
            .Where(p => p.Date >= reconciliation.PeriodStart
                     && p.Date <= reconciliation.PeriodEnd
                     && !matchedPaymentIds.Contains(p.Id))
            .ToListAsync();

        int matchesMade = 0;
        foreach (var bankLine in bankLines)
        {
            decimal bankAmount = (bankLine.Debit) - (bankLine.Credit);
            decimal absBankAmount = Math.Abs(bankAmount);

            Payment? bestMatch = null;
            decimal bestScore = 0m;

            foreach (var payment in payments)
            {
                if (Math.Abs(absBankAmount - payment.Amount) > amountTolerance)
                    continue;

                decimal score = 40;

                var dateDiff = Math.Abs((bankLine.TransactionDate - payment.Date).Days);
                if (dateDiff == 0) score += 30;
                else if (dateDiff <= dateTolerance) score += 30 - (dateDiff * 5);

                if (!string.IsNullOrEmpty(bankLine.Reference) && !string.IsNullOrEmpty(payment.ReferenceNumber))
                {
                    if (bankLine.Reference.Equals(payment.ReferenceNumber, StringComparison.OrdinalIgnoreCase))
                        score += 20;
                    else if (bankLine.Reference.Contains(payment.ReferenceNumber) || payment.ReferenceNumber.Contains(bankLine.Reference))
                        score += 15;
                }

                if (score > bestScore) { bestScore = score; bestMatch = payment; }
            }

            if (bestMatch != null && bestScore >= 60)
            {
                _db.ReconciliationMatches.Add(new ReconciliationMatch
                {
                    ReconciliationId = id,
                    BankStatementLineId = bankLine.Id,
                    SystemPaymentId = bestMatch.Id,
                    MatchType = "auto",
                    ConfidenceScore = bestScore,
                    MatchedBy = _currentUser.UserId ?? 0
                });

                bankLine.MatchStatus = "auto_matched";
                bankLine.MatchedPaymentId = bestMatch.Id;
                bankLine.MatchedAt = DateTime.UtcNow;
                bankLine.MatchedBy = _currentUser.UserId;
                bankLine.MatchConfidence = bestScore;

                matchesMade++;
                payments.Remove(bestMatch);
            }
        }

        await _db.SaveChangesAsync();
        return Ok(new
        {
            reconciliation_id = id,
            matches_made = matchesMade,
            remaining_unmatched = bankLines.Count - matchesMade,
            available_payments = payments.Count
        });
    }

    [HttpPost("reconciliations/{id}/approve")]
    [HasPermission("settings")]
    public async Task<ActionResult> Approve(int id, [FromQuery] bool postAdjustments = true)
    {
        var reconciliation = await _db.BankReconciliations.Include(r => r.CashBox).FirstOrDefaultAsync(r => r.Id == id);
        if (reconciliation == null) return NotFound();
        if (reconciliation.Status != "draft" && reconciliation.Status != "under_review")
            return BadRequest(new { message = "يجب أن تكون الحالة draft أو under_review" });

        if (postAdjustments)
        {
            var pendingAdjustments = await _db.ReconciliationAdjustments
                .Where(a => a.ReconciliationId == id && a.Status == "pending").ToListAsync();

            // P0-4 Batch 5: جمع كل القيود أولًا، ثم حفظ دفعة واحدة (كان SaveChanges داخل الحلقة)
            var approvedEntries = new List<(ReconciliationAdjustment adj, JournalEntry je)>();
            foreach (var adj in pendingAdjustments)
            {
                if (!adj.TargetAccountId.HasValue) continue;

                var je = new JournalEntry
                {
                    Date = DateTime.UtcNow,
                    Description = $"تسوية بنكية: {adj.Description}",
                    ReferenceType = "bank_reconciliation",
                    ReferenceId = id,
                    CreatedBy = _currentUser.UserId ?? 0
                };
                _db.JournalEntries.Add(je);
                approvedEntries.Add((adj, je));
            }
            await _db.SaveChangesAsync(); // دفعة واحدة لجلب IDs لكل القيود

            foreach (var (adj, je) in approvedEntries)
            {
                bool isCreditToBank = adj.AdjustmentType == "bank_fee" || adj.AdjustmentType == "unknown_withdrawal" || adj.AdjustmentType == "duplicate";
                bool isDebitToBank = adj.AdjustmentType == "bank_interest" || adj.AdjustmentType == "unknown_deposit";

                if (isCreditToBank)
                {
                    _db.JournalLines.AddRange(
                        new JournalLine { EntryId = je.Id, AccountId = adj.TargetAccountId.Value, Debit = adj.Amount, Credit = 0 },
                        new JournalLine { EntryId = je.Id, AccountId = reconciliation.CashBox.AccountId, Debit = 0, Credit = adj.Amount }
                    );
                }
                else if (isDebitToBank)
                {
                    _db.JournalLines.AddRange(
                        new JournalLine { EntryId = je.Id, AccountId = reconciliation.CashBox.AccountId, Debit = adj.Amount, Credit = 0 },
                        new JournalLine { EntryId = je.Id, AccountId = adj.TargetAccountId.Value, Debit = 0, Credit = adj.Amount }
                    );
                }

                adj.JournalEntryId = je.Id;
                adj.Status = "posted";
                adj.PostedAt = DateTime.UtcNow;
                adj.PostedBy = _currentUser.UserId;
            }
            await _db.SaveChangesAsync(); // دفعة واحدة للسطور + تحديث التسويات
        }

        reconciliation.Status = "approved";
        reconciliation.ApprovedBy = _currentUser.UserId;
        reconciliation.ApprovedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("reconciliations/{id}/report")]
    public async Task<ActionResult<object>> GetReport(int id)
    {
        var reconciliation = await _db.BankReconciliations.Include(r => r.CashBox).FirstOrDefaultAsync(r => r.Id == id);
        if (reconciliation == null) return NotFound();

        var matches = await _db.ReconciliationMatches.Where(m => m.ReconciliationId == id).ToListAsync();
        var adjustments = await _db.ReconciliationAdjustments.Where(a => a.ReconciliationId == id).ToListAsync();

        var unmatchedLines = await _db.BankStatementLines
            .Include(l => l.Statement)
            .Where(l => l.Statement.CashBoxId == reconciliation.CashBoxId
                     && l.TransactionDate >= reconciliation.PeriodStart
                     && l.TransactionDate <= reconciliation.PeriodEnd
                     && l.MatchStatus == "unmatched")
            .ToListAsync();

        return Ok(new
        {
            reconciliation = new
            {
                reconciliation.Id,
                reconciliation.ReconciliationNumber,
                period = $"{reconciliation.PeriodStart:yyyy-MM-dd} → {reconciliation.PeriodEnd:yyyy-MM-dd}",
                cash_box = reconciliation.CashBox != null ? reconciliation.CashBox.Name : "",
                reconciliation.Status,
                balances = new
                {
                    opening_book = reconciliation.OpeningBookBalance,
                    opening_bank = reconciliation.OpeningBankBalance,
                    closing_book = reconciliation.ClosingBookBalance,
                    closing_bank = reconciliation.ClosingBankBalance,
                    difference = reconciliation.Difference
                }
            },
            matches = new
            {
                total = matches.Count,
                auto = matches.Count(m => m.MatchType == "auto"),
                manual = matches.Count(m => m.MatchType == "manual"),
                avg_confidence = matches.Where(m => m.MatchType == "auto").Select(m => m.ConfidenceScore ?? 0).DefaultIfEmpty(0).Average()
            },
            adjustments = adjustments.Select(a => new { a.AdjustmentType, a.Amount, a.Description, a.Status }),
            unmatched_lines = unmatchedLines.Select(l => new { l.TransactionDate, l.Description, l.Debit, l.Credit, l.Reference })
        });
    }
}

public class CreateReconciliationDto
{
    [Required] public int CashBoxId { get; set; }
    [Required] public DateTime PeriodStart { get; set; }
    [Required] public DateTime PeriodEnd { get; set; }
    public int? BankStatementId { get; set; }
    [Required] public decimal OpeningBookBalance { get; set; }
    [Required] public decimal OpeningBankBalance { get; set; }
    [Required] public decimal ClosingBookBalance { get; set; }
    [Required] public decimal ClosingBankBalance { get; set; }
}
