using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("journal_entries")]
public class JournalEntry
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Column("description")]
    [StringLength(255)]
    public string? Description { get; set; }

    [Column("reference_type")]
    [StringLength(50)]
    public string? ReferenceType { get; set; }

    [Column("reference_id")]
    public int? ReferenceId { get; set; }

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    [Column("period_id")]
    public int? PeriodId { get; set; }
    [ForeignKey("PeriodId")]
    public AccountingPeriod? Period { get; set; }

    public ICollection<JournalLine> Lines { get; set; } = new List<JournalLine>();
}
