using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("invoice_payments")]
public class InvoicePayment
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("invoice_id")]
    public int InvoiceId { get; set; }
    [ForeignKey("InvoiceId")]
    public Invoice? Invoice { get; set; }

    [Column("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Column("amount")]
    public double Amount { get; set; }

    [Column("payment_method")]
    [StringLength(50)]
    public string PaymentMethod { get; set; } = "cash";

    [Column("cash_box_id")]
    public int? CashBoxId { get; set; }
    [ForeignKey("CashBoxId")]
    public CashBox? CashBox { get; set; }

    [Column("reference_number")]
    [StringLength(100)]
    public string? ReferenceNumber { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}