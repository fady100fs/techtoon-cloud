using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("cost_centers")]
public class CostCenter
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("code")]
    [StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("manager_name")]
    [StringLength(100)]
    public string? ManagerName { get; set; }

    [Column("budget_limit")]
    public double? BudgetLimit { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("notes")]
    public string? Notes { get; set; }
}
