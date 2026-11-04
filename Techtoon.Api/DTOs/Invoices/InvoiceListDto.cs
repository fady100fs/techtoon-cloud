namespace Techtoon.Api.DTOs.Invoices;

public class InvoiceListDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public decimal PaidAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public int? WarehouseId { get; set; }
}