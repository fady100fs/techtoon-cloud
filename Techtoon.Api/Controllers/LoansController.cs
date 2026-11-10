using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace Techtoon.Api.Controllers;

/// <summary>P2.19 + P4: Loans list — projected in SQL (no full-entity read), includes type/lender for mobile.</summary>
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
        var q = _db.Loans
            .Select(l => new
            {
                l.Id,
                LoanNumber = l.LoanNumber ?? "",
                BorrowerName = l.BorrowerName ?? "",
                LoanType = l.LoanType ?? "",
                PrincipalAmount = l.PrincipalAmount,
                PaidAmount = l.PaidAmount,
                RemainingAmount = l.RemainingAmount,
                Status = l.Status ?? ""
            })
            .OrderByDescending(l => l.Id);
        var total = await q.CountAsync();
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new
        {
            items,
            totalCount = total,
            page,
            pageSize,
            totalPages = pageSize > 0 ? (int)Math.Ceiling(total / (double)pageSize) : 0
        });
    }
}
