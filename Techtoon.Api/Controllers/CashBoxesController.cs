using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Techtoon.Api.DTOs.CashBoxes;
using Techtoon.Api.Services;

namespace Techtoon.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CashBoxesController : ControllerBase
{
    private readonly ICashBoxAppService _svc;
    private readonly ICurrentUserService _current;

    public CashBoxesController(ICashBoxAppService svc, ICurrentUserService current)
    {
        _svc = svc;
        _current = current;
    }

    [HttpGet]
    public async Task<IActionResult> GetCashBoxes()
    {
        var list = await _svc.GetCashBoxesAsync();
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCashBox(int id)
    {
        var c = await _svc.GetCashBoxAsync(id);
        if (c == null) return NotFound();
        return Ok(c);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCashBox([FromBody] CashBoxCreateDto dto)
    {
        if (_current.UserId == null) return Unauthorized();
        try
        {
            var created = await _svc.CreateCashBoxAsync(dto, _current.UserId.Value);
            return CreatedAtAction(nameof(GetCashBox), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}