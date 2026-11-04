using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("recurring_invoice_templates")]
public class RecurringInvoiceTemplate
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("party_id")]
    public int PartyId { get; set; }
    [ForeignKey("PartyId")]
    public Party? Party { get; set; }

    [Column("invoice_type")]
    [StringLength(50)]
    public string InvoiceType { get; set; } = string.Empty;

    [Column("frequency")]
    [StringLength(50)]
    public string Frequency { get; set; } = "MONTHLY";

    [Column("start_date")]
    public DateTime StartDate { get; set; }

    [Column("next_run_date")]
    public DateTime NextRunDate { get; set; }

    [Column("tax_rate")]
    public double TaxRate { get; set; } = 14.0;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    public ICollection<RecurringInvoiceLine> Lines { get; set; } = new List<RecurringInvoiceLine>();
}
