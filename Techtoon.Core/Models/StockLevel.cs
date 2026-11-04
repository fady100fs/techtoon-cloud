using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("stock_levels")]
public class StockLevel
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("warehouse_id")]
    public int WarehouseId { get; set; }
    [ForeignKey("WarehouseId")]
    public Warehouse? Warehouse { get; set; }

    [Column("item_id")]
    public int ItemId { get; set; }
    [ForeignKey("ItemId")]
    public Item? Item { get; set; }

    [Column("quantity")]
    public double Quantity { get; set; }

    [Column("last_updated")]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
