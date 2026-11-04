namespace Techtoon.Api.DTOs.Items;

public class ItemUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal SellPrice { get; set; }
    public decimal CostPrice { get; set; }
    public decimal MinStock { get; set; }
    public int? CategoryId { get; set; }
}