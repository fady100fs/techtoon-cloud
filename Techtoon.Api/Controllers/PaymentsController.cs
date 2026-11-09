using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Techtoon.Api.DTOs.Payments;
using Techtoon.Api.Security;
using Techtoon.Api.Services;

namespace Techtoon.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentAppService _svc;
    private readonly ICurrentUserService _current;

    public PaymentsController(IPaymentAppService svc, ICurrentUserService current)
    {
        _svc = svc;
        _current = current;
    }

    [HttpGet]
    public async Task<IActionResult> GetPayments(
        [FromQuery] string? type,
        [FromQuery] int? partyId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _svc.GetPaymentsAsync(type, partyId, from, to, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetPayment(int id)
    {
        var p = await _svc.GetPaymentAsync(id);
        if (p == null) return NotFound();
        return Ok(p);
    }

    [HttpPost]
    [HasPermission("sales")]
    public async Task<IActionResult> CreatePayment([FromBody] PaymentCreateDto dto)
    {
        if (_current.UserId == null) return Unauthorized();
        try
        {
            var created = await _svc.CreatePaymentAsync(dto, _current.UserId.Value);
            return CreatedAtAction(nameof(GetPayment), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [HasPermission("sales")]
    public async Task<IActionResult> UpdatePayment(int id, [FromBody] PaymentCreateDto dto)
    {
        if (_current.UserId == null) return Unauthorized();
        try
        {
            var updated = await _svc.UpdatePaymentAsync(id, dto, _current.UserId.Value);
            if (updated == null) return NotFound();
            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [HasPermission("cancel_invoice")]
    public async Task<IActionResult> DeletePayment(int id)
    {
        if (_current.UserId == null) return Unauthorized();
        var deleted = await _svc.DeletePaymentAsync(id, _current.UserId.Value);
        if (!deleted) return NotFound();
        return NoContent();
    }
}