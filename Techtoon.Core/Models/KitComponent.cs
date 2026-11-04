using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("kit_components")]
public class KitComponent
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("kit_item_id")]
    public int KitItemId { get; set; }
    [ForeignKey("KitItemId")]
    public Item? KitItem { get; set; }

    [Column("component_item_id")]
    public int ComponentItemId { get; set; }
    [ForeignKey("ComponentItemId")]
    public Item? ComponentItem { get; set; }

    [Column("quantity")]
    public double Quantity { get; set; } = 1.0;
}
