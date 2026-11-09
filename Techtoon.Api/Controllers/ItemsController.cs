using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Techtoon.Api.DTOs.Items;
using Techtoon.Api.Security;
using Techtoon.Api.Services;

namespace Techtoon.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ItemsController : ControllerBase
{
    private readonly IItemAppService _svc;
    private readonly ICurrentUserService _current;

    public ItemsController(IItemAppService svc, ICurrentUserService current)
    {
        _svc = svc;
        _current = current;
    }

    [HttpGet]
    public async Task<IActionResult> GetItems(
        [FromQuery] string? search,
        [FromQuery] string? barcode,
        [FromQuery] int? categoryId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _svc.GetItemsAsync(search, barcode, categoryId, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetItem(int id)
    {
        var item = await _svc.GetItemAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    [HasPermission("settings")]
    public async Task<IActionResult> CreateItem([FromBody] ItemCreateDto dto)
    {
        if (_current.UserId == null) return Unauthorized();
        var created = await _svc.CreateItemAsync(dto, _current.UserId.Value);
        return CreatedAtAction(nameof(GetItem), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [HasPermission("settings")]
    public async Task<IActionResult> UpdateItem(int id, [FromBody] ItemUpdateDto dto)
    {
        if (_current.UserId == null) return Unauthorized();
        var updated = await _svc.UpdateItemAsync(id, dto, _current.UserId.Value);
        if (updated == null) return NotFound();
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [HasPermission("settings")]
    public async Task<IActionResult> DeleteItem(int id)
    {
        if (_current.UserId == null) return Unauthorized();
        try
        {
            var deleted = await _svc.DeleteItemAsync(id, _current.UserId.Value);
            if (!deleted) return NotFound();
            return NoContent();
        }
        catch (System.InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}/stock")]
    public async Task<IActionResult> GetStock(int id)
    {
        var stock = await _svc.GetCurrentStockAsync(id);
        return Ok(new { itemId = id, currentStock = stock });
    }

    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStock()
    {
        var items = await _svc.GetLowStockItemsAsync();
        return Ok(items);
    }
}