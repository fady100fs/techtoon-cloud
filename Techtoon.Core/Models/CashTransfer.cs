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
    public decimal Amount { get; set; }
    [Column("transfer_number")] public string? TransferNumber { get; set; }
    [Column("transfer_fee")] public decimal TransferFee { get; set; }
    [Column("fee_account_id")] public int? FeeAccountId { get; set; }
    [Column("in_transit_account_id")] public int? InTransitAccountId { get; set; }
    [Column("source_value_date")] public DateTime? SourceValueDate { get; set; }
    [Column("destination_value_date")] public DateTime? DestinationValueDate { get; set; }
    [Column("status")] public string Status { get; set; } = "completed";
    [Column("approved_by")] public int? ApprovedBy { get; set; }
    [Column("approved_at")] public DateTime? ApprovedAt { get; set; }
    [Column("cancelled_at")] public DateTime? CancelledAt { get; set; }
    [Column("cancelled_by")] public int? CancelledBy { get; set; }
    [Column("cancellation_reason")] public string? CancellationReason { get; set; }

}

