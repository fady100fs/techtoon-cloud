using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Techtoon.Api.DTOs.Auth;
using Techtoon.Api.Services;

namespace Techtoon.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthAppService _auth;
    private readonly ICurrentUserService _current;

    public AuthController(IAuthAppService auth, ICurrentUserService current)
    {
        _auth = auth;
        _current = current;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await _auth.LoginAsync(request, ip);
        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await _auth.RefreshAsync(request.RefreshToken, ip);
        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        if (_current.UserId == null || _current.DeviceId == null)
            return Unauthorized();
        await _auth.LogoutAsync(_current.UserId.Value, _current.DeviceId);
        return Ok(new { message = "Logged out successfully" });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        if (_current.UserId == null)
            return Unauthorized();
        var user = await _auth.GetCurrentUserAsync(_current.UserId.Value);
        return Ok(user);
    }

    [HttpGet("devices")]
    [Authorize]
    public async Task<IActionResult> GetMyDevices()
    {
        if (_current.UserId == null)
            return Unauthorized();
        var devices = await _auth.GetDevicesAsync(_current.UserId.Value);
        return Ok(devices);
    }

    [HttpDelete("devices/{id}")]
    [Authorize]
    public async Task<IActionResult> RevokeDevice(int id)
    {
        if (_current.UserId == null)
            return Unauthorized();
        var revoked = await _auth.RevokeDeviceAsync(_current.UserId.Value, id);
        if (!revoked) return NotFound(new { message = "Device not found" });
        return Ok(new { message = "Device revoked" });
    }
}