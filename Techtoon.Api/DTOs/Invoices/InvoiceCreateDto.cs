using System.ComponentModel.DataAnnotations;
namespace Techtoon.Api.DTOs.Invoices;

public class InvoiceCreateDto
{
    [Required, StringLength(20)]
    public string Type { get; set; } = "sales";
    public int? PartyId { get; set; }
    public int? WarehouseId { get; set; }
    public int? CashBoxId { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
    public DateTime? DueDate { get; set; }
    [Range(0, double.MaxValue)]
    public decimal DiscountAmount { get; set; }
    [Range(0, 100)]
    public decimal TaxRate { get; set; }
    public string? Notes { get; set; }
    [Required, MinLength(1)]
    public List<InvoiceLineInputDto> Lines { get; set; } = new();
    public List<InvoicePaymentInputDto> Payments { get; set; } = new();
}

public class InvoiceLineInputDto
{
    [Range(1, int.MaxValue)]
    public int ItemId { get; set; }
    [Range(0.01, double.MaxValue)]
    public decimal Quantity { get; set; }
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }
    [Range(0, double.MaxValue)]
    public decimal Discount { get; set; }
}

public class InvoicePaymentInputDto
{
    [Required, StringLength(50)]
    public string Method { get; set; } = "cash";
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public DateTime? PaidAt { get; set; }
}