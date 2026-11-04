namespace Techtoon.Api.DTOs.Common;

public class ErrorResponseDto
{
    public int Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public string? TraceId { get; set; }
}