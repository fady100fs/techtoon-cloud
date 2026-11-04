namespace Techtoon.Api.DTOs.Expenses;

public class ExpenseDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public int? CategoryId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public int? CashBoxId { get; set; }
    public string? Notes { get; set; }
}

public class ExpenseCreateDto
{
    public DateTime Date { get; set; } = DateTime.Now;
    public int? CategoryId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string PaymentMethod { get; set; } = "cash";
    public int? CashBoxId { get; set; }
    public string? Notes { get; set; }
}