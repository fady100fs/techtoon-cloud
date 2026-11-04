using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("accounting_periods")]
public class AccountingPeriod
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("period_name")]
    [StringLength(50)]
    public string PeriodName { get; set; } = string.Empty;

    [Column("start_date")]
    public DateTime StartDate { get; set; }

    [Column("end_date")]
    public DateTime EndDate { get; set; }

    [Column("status")]
    [StringLength(50)]
    public string Status { get; set; } = "OPEN";

    [Column("is_fiscal_year")]
    public bool IsFiscalYear { get; set; }

    [Column("closing_date")]
    public DateTime? ClosingDate { get; set; }

    [Column("closing_notes")]
    public string? ClosingNotes { get; set; }

    public ICollection<JournalEntry> JournalEntries { get; set; } = new List<JournalEntry>();
}
