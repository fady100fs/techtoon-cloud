using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace Techtoon.Api.Controllers;

/// <summary>P2.19: Salaries list (salary_records) for mobile generic screen.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SalariesController : ControllerBase
{
    private readonly Techtoon.Data.Context.TechtoonDbContext _db;
    public SalariesController(Techtoon.Data.Context.TechtoonDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var q = _db.SalaryRecords.OrderByDescending(s => s.Year).ThenByDescending(s => s.Month);
        var total = q.Count();
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new
        {
            items = items.Select(s => new { id = s.Id, employeeName = s.Employee?.FullName ?? string.Empty, netSalary = s.NetSalary, paymentDate = new DateTime(s.Year, s.Month, 1) }),
            totalCount = total,
            page,
            pageSize,
            totalPages = pageSize > 0 ? (int)Math.Ceiling(total / (double)pageSize) : 0
        });
    }
}

