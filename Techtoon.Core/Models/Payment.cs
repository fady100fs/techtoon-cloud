using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("payments")]
public class Payment
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Column("party_id")]
    public int PartyId { get; set; }
    [ForeignKey("PartyId")]
    public Party? Party { get; set; }

    [Column("amount")]
    public double Amount { get; set; }

    [Column("payment_type")]
    [StringLength(50)]
    public string PaymentType { get; set; } = string.Empty;

    [Column("payment_method")]
    [StringLength(100)]
    public string PaymentMethod { get; set; } = string.Empty;

    [Column("reference_number")]
    [StringLength(100)]
    public string? ReferenceNumber { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("currency_id")]
    public int CurrencyId { get; set; } = 1;
    [ForeignKey("CurrencyId")]
    public Currency? Currency { get; set; }

    [Column("cash_box_id")]
    public int CashBoxId { get; set; } = 1;
    [ForeignKey("CashBoxId")]
    public CashBox? CashBox { get; set; }
}
