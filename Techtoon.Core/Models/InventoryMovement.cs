using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("inventory_movements")]
public class InventoryMovement
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("item_id")]
    public int ItemId { get; set; }
    [ForeignKey("ItemId")]
    public Item? Item { get; set; }

    [Column("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Column("type")]
    [StringLength(50)]
    public string Type { get; set; } = string.Empty;

    [Column("quantity")]
    public double Quantity { get; set; }

    [Column("reference_id")]
    public int? ReferenceId { get; set; }
}
