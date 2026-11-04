using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("recurring_invoice_lines")]
public class RecurringInvoiceLine
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("template_id")]
    public int TemplateId { get; set; }
    [ForeignKey("TemplateId")]
    public RecurringInvoiceTemplate? Template { get; set; }

    [Column("item_id")]
    public int ItemId { get; set; }
    [ForeignKey("ItemId")]
    public Item? Item { get; set; }

    [Column("quantity")]
    public double Quantity { get; set; }

    [Column("price")]
    public double Price { get; set; }
}
