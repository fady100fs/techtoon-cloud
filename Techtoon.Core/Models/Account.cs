using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("accounts")]
public class Account
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("code")]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("type")]
    [StringLength(50)]
    public string Type { get; set; } = "ASSET";

    [Column("parent_id")]
    public int? ParentId { get; set; }

    [ForeignKey("ParentId")]
    public Account? Parent { get; set; }
    public ICollection<Account> Children { get; set; } = new List<Account>();
    public ICollection<JournalLine> JournalLines { get; set; } = new List<JournalLine>();
    public ICollection<Party> Parties { get; set; } = new List<Party>();
    public ICollection<CashBox> CashBoxes { get; set; } = new List<CashBox>();
    public ICollection<Item> ItemsAsInventory { get; set; } = new List<Item>();
    public ICollection<Item> ItemsAsCogs { get; set; } = new List<Item>();
    public ICollection<Item> ItemsAsRevenue { get; set; } = new List<Item>();
    public ICollection<ExpenseCategory> ExpenseCategories { get; set; } = new List<ExpenseCategory>();
    public ICollection<Budget> Budgets { get; set; } = new List<Budget>();
    public ICollection<CostAllocation> CostAllocations { get; set; } = new List<CostAllocation>();
}
