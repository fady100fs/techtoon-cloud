using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace Techtoon.Api.Controllers;

/// <summary>P2.19 + P4 fix: Employees list — projected in SQL (no full-entity read, immune to NULL money columns).</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly Techtoon.Data.Context.TechtoonDbContext _db;
    public EmployeesController(Techtoon.Data.Context.TechtoonDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var q = _db.Employees
            .Select(e => new { e.Id, FullName = e.FullName ?? "", JobTitle = e.JobTitle ?? "", Phone = e.Phone })
            .OrderBy(e => e.FullName);
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
