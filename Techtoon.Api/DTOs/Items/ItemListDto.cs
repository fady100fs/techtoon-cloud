namespace Techtoon.Api.DTOs.Items;

public class ItemListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal SellPrice { get; set; }
    public decimal CostPrice { get; set; }
    public decimal AvgCostPrice { get; set; }
    public decimal OpeningQuantity { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal MinStock { get; set; }
    public bool IsKit { get; set; }
    public int? CategoryId { get; set; }
}