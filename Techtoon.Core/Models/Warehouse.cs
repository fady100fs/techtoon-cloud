using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("warehouses")]
public class Warehouse
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

    [Column("location")]
    [StringLength(200)]
    public string? Location { get; set; }

    [Column("responsible_person")]
    [StringLength(100)]
    public string? ResponsiblePerson { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<StockLevel> StockLevels { get; set; } = new List<StockLevel>();
    public ICollection<WarehouseTransfer> TransfersFrom { get; set; } = new List<WarehouseTransfer>();
    public ICollection<WarehouseTransfer> TransfersTo { get; set; } = new List<WarehouseTransfer>();
    public ICollection<StockCount> StockCounts { get; set; } = new List<StockCount>();
}
