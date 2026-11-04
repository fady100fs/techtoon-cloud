namespace Techtoon.Api.DTOs.Payments;

public class PaymentDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public int? PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentType { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string? VoucherNo { get; set; }
    public string? Notes { get; set; }
    public int? CashBoxId { get; set; }
}

public class PaymentCreateDto
{
    public DateTime Date { get; set; } = DateTime.Now;
    public int? PartyId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentType { get; set; } = "receipt";
    public string PaymentMethod { get; set; } = "cash";
    public string? VoucherNo { get; set; }
    public string? Notes { get; set; }
    public int? CashBoxId { get; set; }
}