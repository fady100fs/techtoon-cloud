using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("cost_history")]
public class CostHistory
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("item_id")]
    public int ItemId { get; set; }
    [ForeignKey("ItemId")]
    public Item? Item { get; set; }

    [Column("invoice_id")]
    public int InvoiceId { get; set; }
    [ForeignKey("InvoiceId")]
    public Invoice? Invoice { get; set; }

    [Column("purchase_price")]
    public double PurchasePrice { get; set; }

    [Column("quantity")]
    public double Quantity { get; set; }

    [Column("total_cost")]
    public double TotalCost { get; set; }

    [Column("purchase_date")]
    public DateTime PurchaseDate { get; set; }

    [Column("supplier_id")]
    public int? SupplierId { get; set; }
    [ForeignKey("SupplierId")]
    public Party? Supplier { get; set; }
}
