using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("cash_boxes")]
public class CashBox
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("code")]
    [StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Column("type")]
    [StringLength(50)]
    public string Type { get; set; } = "CASH";

    [Column("account_id")]
    public int AccountId { get; set; }
    [ForeignKey("AccountId")]
    public Account? Account { get; set; }

    [Column("responsible_person")]
    [StringLength(100)]
    public string? ResponsiblePerson { get; set; }

    [Column("max_limit")]
    public double? MaxLimit { get; set; }

    [Column("opening_balance")]
    public double OpeningBalance { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<CashTransfer> TransfersFrom { get; set; } = new List<CashTransfer>();
    public ICollection<CashTransfer> TransfersTo { get; set; } = new List<CashTransfer>();
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
}
