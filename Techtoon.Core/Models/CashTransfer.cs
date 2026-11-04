using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("cash_transfers")]
public class CashTransfer
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Column("from_cash_box_id")]
    public int FromCashBoxId { get; set; }
    [ForeignKey("FromCashBoxId")]
    public CashBox? FromCashBox { get; set; }

    [Column("to_cash_box_id")]
    public int ToCashBoxId { get; set; }
    [ForeignKey("ToCashBoxId")]
    public CashBox? ToCashBox { get; set; }

    [Column("amount")]
    public double Amount { get; set; }
}
