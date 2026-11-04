using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Techtoon.Api.Services;

public interface ICurrentUserService
{
    int? UserId { get; }
    string? Username { get; }
    string? Role { get; }
    string? DeviceId { get; }
    bool IsAuthenticated { get; }
    bool HasPermission(string key);
}

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public int? UserId => int.TryParse(User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;
    public string? Username => User?.FindFirst(ClaimTypes.Name)?.Value;
    public string? Role => User?.FindFirst(ClaimTypes.Role)?.Value;
    public string? DeviceId => User?.FindFirst("device_id")?.Value;
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public bool HasPermission(string key)
    {
        if (Role == "ADMIN") return true;
        return User?.HasClaim("permission", key) == true;
    }
}