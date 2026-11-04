namespace Techtoon.Api.DTOs.Invoices;

public class InvoiceCreateDto
{
    public string Type { get; set; } = "sales";
    public int? PartyId { get; set; }
    public int? WarehouseId { get; set; }
    public int? CashBoxId { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
    public DateTime? DueDate { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxRate { get; set; }
    public string? Notes { get; set; }
    public List<InvoiceLineInputDto> Lines { get; set; } = new();
    public List<InvoicePaymentInputDto> Payments { get; set; } = new();
}

public class InvoiceLineInputDto
{
    public int ItemId { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Discount { get; set; }
}

public class InvoicePaymentInputDto
{
    public string Method { get; set; } = "cash";
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public DateTime? PaidAt { get; set; }
}