using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("stock_counts")]
public class StockCount
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Column("warehouse_id")]
    public int WarehouseId { get; set; }
    [ForeignKey("WarehouseId")]
    public Warehouse? Warehouse { get; set; }

    [Column("item_id")]
    public int ItemId { get; set; }
    [ForeignKey("ItemId")]
    public Item? Item { get; set; }

    [Column("system_quantity")]
    public double SystemQuantity { get; set; }

    [Column("counted_quantity")]
    public double CountedQuantity { get; set; }

    [Column("difference")]
    public double Difference { get; set; }
}
