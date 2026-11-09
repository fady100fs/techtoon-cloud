using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("warehouse_transfers")]
public class WarehouseTransfer
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Column("from_warehouse_id")]
    public int FromWarehouseId { get; set; }
    [ForeignKey("FromWarehouseId")]
    public Warehouse? FromWarehouse { get; set; }

    [Column("to_warehouse_id")]
    public int ToWarehouseId { get; set; }
    [ForeignKey("ToWarehouseId")]
    public Warehouse? ToWarehouse { get; set; }

    [Column("item_id")]
    public int ItemId { get; set; }
    [ForeignKey("ItemId")]
    public Item? Item { get; set; }

    [Column("quantity")]
    public decimal Quantity { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("status")]
    [StringLength(50)]
    public string Status { get; set; } = "completed";
    [Column("transfer_number")] public string? TransferNumber { get; set; }
    [Column("shipped_quantity")] public decimal ShippedQuantity { get; set; }
    [Column("received_quantity")] public decimal ReceivedQuantity { get; set; }
    [Column("approved_by")] public int? ApprovedBy { get; set; }
    [Column("approved_at")] public DateTime? ApprovedAt { get; set; }
    [Column("shipped_at")] public DateTime? ShippedAt { get; set; }
    [Column("received_at")] public DateTime? ReceivedAt { get; set; }
    [Column("cancelled_at")] public DateTime? CancelledAt { get; set; }
    [Column("cancelled_by")] public int? CancelledBy { get; set; }
    [Column("cancellation_reason")] public string? CancellationReason { get; set; }
    public ICollection<WarehouseTransferLine> Lines { get; set; } = new List<WarehouseTransferLine>();

}

