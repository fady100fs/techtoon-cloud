namespace Techtoon.Api.DTOs.Parties;

public class PartyUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
}