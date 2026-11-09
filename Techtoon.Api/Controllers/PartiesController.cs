using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Techtoon.Api.DTOs.Parties;
using Techtoon.Api.Security;
using Techtoon.Api.Services;

namespace Techtoon.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PartiesController : ControllerBase
{
    private readonly IPartyAppService _svc;
    private readonly ICurrentUserService _current;

    public PartiesController(IPartyAppService svc, ICurrentUserService current)
    {
        _svc = svc;
        _current = current;
    }

    [HttpGet]
    public async Task<IActionResult> GetParties(
        [FromQuery] string? search,
        [FromQuery] string? type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _svc.GetPartiesAsync(search, type, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetParty(int id)
    {
        var party = await _svc.GetPartyAsync(id);
        if (party == null) return NotFound();
        return Ok(party);
    }

    [HttpPost]
    [HasPermission("sales")]
    public async Task<IActionResult> CreateParty([FromBody] PartyCreateDto dto)
    {
        if (_current.UserId == null) return Unauthorized();
        var created = await _svc.CreatePartyAsync(dto, _current.UserId.Value);
        return CreatedAtAction(nameof(GetParty), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [HasPermission("sales")]
    public async Task<IActionResult> UpdateParty(int id, [FromBody] PartyUpdateDto dto)
    {
        if (_current.UserId == null) return Unauthorized();
        var updated = await _svc.UpdatePartyAsync(id, dto, _current.UserId.Value);
        if (updated == null) return NotFound();
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [HasPermission("settings")]
    public async Task<IActionResult> DeleteParty(int id)
    {
        if (_current.UserId == null) return Unauthorized();
        try
        {
            var deleted = await _svc.DeletePartyAsync(id, _current.UserId.Value);
            if (!deleted) return NotFound();
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}/statement")]
    public async Task<IActionResult> GetStatement(int id, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        try
        {
            var stmt = await _svc.GetStatementAsync(id, from, to);
            return Ok(stmt);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}