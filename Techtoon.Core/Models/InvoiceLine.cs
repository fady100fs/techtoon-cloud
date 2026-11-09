using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("invoice_lines")]
public class InvoiceLine
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("invoice_id")]
    public int InvoiceId { get; set; }
    [ForeignKey("InvoiceId")]
    public Invoice? Invoice { get; set; }

    [Column("item_id")]
    public int ItemId { get; set; }
    [ForeignKey("ItemId")]
    public Item? Item { get; set; }

    [Column("quantity")]
    public decimal Quantity { get; set; }

    [Column("price")]
    public decimal Price { get; set; }

    [Column("total")]
    public decimal Total { get; set; }

    [Column("cost_price")]
    public decimal CostPrice { get; set; }
}
