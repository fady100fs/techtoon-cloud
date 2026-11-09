using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace Techtoon.Api.Controllers;

/// <summary>P2.19: Loans list for mobile loans screen.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LoansController : ControllerBase
{
    private readonly Techtoon.Data.Context.TechtoonDbContext _db;
    public LoansController(Techtoon.Data.Context.TechtoonDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var q = _db.Loans.OrderByDescending(l => l.LoanDate);
        var total = q.Count();
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new
        {
            items = items.Select(l => new { id = l.Id, loanNumber = l.LoanNumber, principalAmount = l.PrincipalAmount, paidAmount = l.PaidAmount, remainingAmount = l.RemainingAmount, status = l.Status }),
            totalCount = total,
            page,
            pageSize,
            totalPages = pageSize > 0 ? (int)Math.Ceiling(total / (double)pageSize) : 0
        });
    }
}

