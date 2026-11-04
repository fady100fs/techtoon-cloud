namespace Techtoon.Api.DTOs.Parties;

public class PartyCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; } = "customer";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
    public decimal OpeningBalance { get; set; }
}