namespace Techtoon.Api.DTOs.CashBoxes;

public class CashBoxDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public string Currency { get; set; } = string.Empty;
}

public class CashBoxCreateDto
{
    public string Name { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public string Currency { get; set; } = "EGP";
}