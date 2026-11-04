using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("currencies")]
public class Currency
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("code")]
    [StringLength(10)]
    public string Code { get; set; } = string.Empty;

    [Column("name")]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [Column("symbol")]
    [StringLength(10)]
    public string Symbol { get; set; } = string.Empty;

    [Column("exchange_rate")]
    public double ExchangeRate { get; set; } = 1.0;

    [Column("is_default")]
    public bool IsDefault { get; set; }

    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
