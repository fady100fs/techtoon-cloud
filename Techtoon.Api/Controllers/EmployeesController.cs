using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace Techtoon.Api.Controllers;

/// <summary>P2.19: Employees list for mobile generic screen.</summary>
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
        var q = _db.Employees.OrderBy(e => e.FullName);
        var total = q.Count();
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new
        {
            items = items.Select(e => new { id = e.Id, fullName = e.FullName, jobTitle = e.JobTitle, phone = e.Phone }),
            totalCount = total,
            page,
            pageSize,
            totalPages = pageSize > 0 ? (int)Math.Ceiling(total / (double)pageSize) : 0
        });
    }
}

