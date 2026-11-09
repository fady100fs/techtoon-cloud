using System.ComponentModel.DataAnnotations;
namespace Techtoon.Api.DTOs.Auth;

public class LoginRequestDto
{
    [Required, MinLength(1)]
    public string Username { get; set; } = string.Empty;
    [Required, MinLength(1)]
    public string Password { get; set; } = string.Empty;
    public string? DeviceId { get; set; }
    public string? DeviceName { get; set; }
}