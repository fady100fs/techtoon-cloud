using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Techtoon.Api.DTOs.Invoices;
using Techtoon.Api.Services;

namespace Techtoon.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceAppService _svc;
    private readonly ICurrentUserService _current;

    public InvoicesController(IInvoiceAppService svc, ICurrentUserService current)
    {
        _svc = svc;
        _current = current;
    }

    [HttpGet]
    public async Task<IActionResult> GetInvoices(
        [FromQuery] string? type,
        [FromQuery] string? status,
        [FromQuery] int? partyId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _svc.GetInvoicesAsync(type, status, partyId, from, to, search, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetInvoice(int id)
    {
        var inv = await _svc.GetInvoiceAsync(id);
        if (inv == null) return NotFound();
        return Ok(inv);
    }

    [HttpPost]
    public async Task<IActionResult> CreateInvoice([FromBody] InvoiceCreateDto dto)
    {
        if (_current.UserId == null) return Unauthorized();
        try
        {
            var created = await _svc.CreateInvoiceAsync(dto, _current.UserId.Value);
            return CreatedAtAction(nameof(GetInvoice), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteInvoice(int id)
    {
        if (_current.UserId == null) return Unauthorized();
        var deleted = await _svc.DeleteInvoiceAsync(id, _current.UserId.Value);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> CancelInvoice(int id)
    {
        if (_current.UserId == null) return Unauthorized();
        var cancelled = await _svc.CancelInvoiceAsync(id, _current.UserId.Value);
        if (!cancelled) return NotFound(new { message = "Invoice not found or already cancelled" });
        return Ok(new { message = "Invoice cancelled" });
    }
}