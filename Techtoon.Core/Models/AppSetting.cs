using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("app_settings")]
public class AppSetting
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("key")]
    [StringLength(100)]
    public string Key { get; set; } = string.Empty;

    [Column("value")]
    public string? Value { get; set; }

    [Column("category")]
    [StringLength(50)]
    public string Category { get; set; } = "general";
}
