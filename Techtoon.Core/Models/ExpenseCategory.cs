using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("expense_categories")]
public class ExpenseCategory
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("account_id")]
    public int AccountId { get; set; }
    [ForeignKey("AccountId")]
    public Account? Account { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}
