using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("journal_lines")]
public class JournalLine
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("entry_id")]
    public int EntryId { get; set; }
    [ForeignKey("EntryId")]
    public JournalEntry? Entry { get; set; }

    [Column("account_id")]
    public int AccountId { get; set; }
    [ForeignKey("AccountId")]
    public Account? Account { get; set; }

    [Column("debit")]
    public double Debit { get; set; }

    [Column("credit")]
    public double Credit { get; set; }
}
