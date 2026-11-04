namespace Techtoon.Api.DTOs.Invoices;

public class InvoiceLineDto
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public decimal CostPrice { get; set; }
}