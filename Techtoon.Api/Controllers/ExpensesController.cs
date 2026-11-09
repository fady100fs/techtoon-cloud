using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Techtoon.Api.DTOs.Expenses;
using Techtoon.Api.Security;
using Techtoon.Api.Services;

namespace Techtoon.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseAppService _svc;
    private readonly ICurrentUserService _current;

    public ExpensesController(IExpenseAppService svc, ICurrentUserService current)
    {
        _svc = svc;
        _current = current;
    }

    [HttpGet]
    public async Task<IActionResult> GetExpenses(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? categoryId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _svc.GetExpensesAsync(from, to, categoryId, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetExpense(int id)
    {
        var e = await _svc.GetExpenseAsync(id);
        if (e == null) return NotFound();
        return Ok(e);
    }

    [HttpPost]
    [HasPermission("expenses")]
    public async Task<IActionResult> CreateExpense([FromBody] ExpenseCreateDto dto)
    {
        if (_current.UserId == null) return Unauthorized();
        try
        {
            var created = await _svc.CreateExpenseAsync(dto, _current.UserId.Value);
            return CreatedAtAction(nameof(GetExpense), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [HasPermission("expenses")]
    public async Task<IActionResult> DeleteExpense(int id)
    {
        if (_current.UserId == null) return Unauthorized();
        var deleted = await _svc.DeleteExpenseAsync(id, _current.UserId.Value);
        if (!deleted) return NotFound();
        return NoContent();
    }
}