using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("items")]
public class Item
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("barcode")]
    [StringLength(100)]
    public string? Barcode { get; set; }

    [Column("cost_price")]
    public double CostPrice { get; set; }

    [Column("sell_price")]
    public double SellPrice { get; set; }

    [Column("is_kit")]
    public bool IsKit { get; set; }

    [Column("min_stock")]
    public double MinStock { get; set; } = 10.0;

    [Column("opening_quantity")]
    public double OpeningQuantity { get; set; }

    [Column("category_id")]
    public int? CategoryId { get; set; }

    [ForeignKey("CategoryId")]
    public Category? Category { get; set; }

    [Column("avg_cost_price")]
    public double AvgCostPrice { get; set; }

    [Column("total_purchased_qty")]
    public double TotalPurchasedQty { get; set; }

    [Column("inventory_account_id")]
    public int? InventoryAccountId { get; set; }
    [ForeignKey("InventoryAccountId")]
    public Account? InventoryAccount { get; set; }

    [Column("cogs_account_id")]
    public int? CogsAccountId { get; set; }
    [ForeignKey("CogsAccountId")]
    public Account? CogsAccount { get; set; }

    [Column("revenue_account_id")]
    public int? RevenueAccountId { get; set; }
    [ForeignKey("RevenueAccountId")]
    public Account? RevenueAccount { get; set; }

    public ICollection<KitComponent> Components { get; set; } = new List<KitComponent>();
    public ICollection<KitComponent> UsedInKits { get; set; } = new List<KitComponent>();
    public ICollection<CostHistory> CostHistory { get; set; } = new List<CostHistory>();
    public ICollection<InventoryMovement> InventoryMovements { get; set; } = new List<InventoryMovement>();
    public ICollection<InvoiceLine> InvoiceLines { get; set; } = new List<InvoiceLine>();
    public ICollection<StockLevel> StockLevels { get; set; } = new List<StockLevel>();
    public ICollection<WarehouseTransfer> WarehouseTransfers { get; set; } = new List<WarehouseTransfer>();
    public ICollection<StockCount> StockCounts { get; set; } = new List<StockCount>();
    public ICollection<RecurringInvoiceLine> RecurringInvoiceLines { get; set; } = new List<RecurringInvoiceLine>();

    [NotMapped]
    public string DisplayCode => "ITM-" + Id.ToString("D5");

    [NotMapped]
    public string SearchDisplay => DisplayCode + " - " + Name;

    [NotMapped]
    public double CurrentStock { get; set; }

    [NotMapped]
    public bool IsSelected { get; set; }
}
