namespace Techtoon.Api.DTOs.Invoices;

public class InvoiceDetailDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public DateTime? DueDate { get; set; }
    public string Type { get; set; } = string.Empty;
    public int? PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public int? WarehouseId { get; set; }
    public int? CashBoxId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public decimal PaidAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<InvoiceLineDto> Lines { get; set; } = new();
    public List<InvoicePaymentDto> Payments { get; set; } = new();
}

public class InvoicePaymentDto
{
    public int Id { get; set; }
    public string Method { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public DateTime PaidAt { get; set; }
}