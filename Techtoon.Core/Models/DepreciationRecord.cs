using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("depreciation_records")]
public class DepreciationRecord
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("date")]
    public DateTime Date { get; set; }

    [Column("depreciation_amount")]
    public double DepreciationAmount { get; set; }
}
