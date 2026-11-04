namespace Techtoon.Api.DTOs.Auth;

public class DeviceDto
{
    public int Id { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public DateTime LastSeen { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRevoked { get; set; }
}