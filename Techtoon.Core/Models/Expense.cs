using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("expenses")]
public class Expense
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Column("amount")]
    public double Amount { get; set; }

    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Column("payment_method")]
    [StringLength(50)]
    public string PaymentMethod { get; set; } = string.Empty;

    [Column("cash_box_id")]
    public int CashBoxId { get; set; }
    [ForeignKey("CashBoxId")]
    public CashBox? CashBox { get; set; }
}
