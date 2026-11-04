using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("invoices")]
public class Invoice
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("invoice_number")]
    [StringLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Column("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Column("due_date")]
    public DateTime? DueDate { get; set; }

    [Column("party_id")]
    public int PartyId { get; set; }
    [ForeignKey("PartyId")]
    public Party? Party { get; set; }

    [Column("type")]
    [StringLength(50)]
    public string Type { get; set; } = string.Empty;

    [Column("total_amount")]
    public double TotalAmount { get; set; }

    [Column("status")]
    [StringLength(50)]
    public string Status { get; set; } = "pending";

    [Column("currency_id")]
    public int CurrencyId { get; set; } = 1;
    [ForeignKey("CurrencyId")]
    public Currency? Currency { get; set; }

    [Column("discount_amount")]
    public double DiscountAmount { get; set; }

    [Column("discount_percentage")]
    public double DiscountPercentage { get; set; }

    [Column("tax_rate")]
    public double TaxRate { get; set; } = 14.0;

    [Column("tax_amount")]
    public double TaxAmount { get; set; }

    [Column("net_amount")]
    public double NetAmount { get; set; }

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    public ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();
    public ICollection<CostHistory> CostHistories { get; set; } = new List<CostHistory>();
}
