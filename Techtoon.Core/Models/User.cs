using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("username")]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [Column("password_hash")]
    [StringLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("full_name")]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Column("email")]
    [StringLength(100)]
    public string? Email { get; set; }

    [Column("role")]
    [StringLength(50)]
    public string Role { get; set; } = "VIEWER";

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("last_login")]
    public DateTime? LastLogin { get; set; }
}
