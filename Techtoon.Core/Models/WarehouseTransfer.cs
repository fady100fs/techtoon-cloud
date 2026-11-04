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
    public double Quantity { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("status")]
    [StringLength(50)]
    public string Status { get; set; } = "completed";
}
