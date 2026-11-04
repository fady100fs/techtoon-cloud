using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("parties")]
public class Party
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("type")]
    [StringLength(50)]
    public string Type { get; set; } = string.Empty;

    [Column("phone")]
    [StringLength(50)]
    public string? Phone { get; set; }

    [Column("address")]
    public string? Address { get; set; }

    [Column("credit_limit")]
    public double CreditLimit { get; set; }

    [Column("opening_balance")]
    public double OpeningBalance { get; set; }

    [Column("account_id")]
    public int AccountId { get; set; }

    [ForeignKey("AccountId")]
    public Account? Account { get; set; }

    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<CostHistory> CostHistories { get; set; } = new List<CostHistory>();
    public ICollection<RecurringInvoiceTemplate> RecurringTemplates { get; set; } = new List<RecurringInvoiceTemplate>();
}
