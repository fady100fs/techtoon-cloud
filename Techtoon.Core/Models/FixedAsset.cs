using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("fixed_assets")]
public class FixedAsset
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

    [Column("purchase_date")]
    public DateTime PurchaseDate { get; set; }

    [Column("purchase_cost")]
    public double PurchaseCost { get; set; }

    [Column("useful_life_years")]
    public int UsefulLifeYears { get; set; }

    [Column("depreciation_method")]
    [StringLength(50)]
    public string DepreciationMethod { get; set; } = "STRAIGHT_LINE";

    [Column("net_book_value")]
    public double NetBookValue { get; set; }

    [Column("status")]
    [StringLength(50)]
    public string Status { get; set; } = "active";
}
