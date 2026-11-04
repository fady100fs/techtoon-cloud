# build_models.py
from pathlib import Path

ROOT = Path(r"D:\PROG\Accounting Prog\Techtoon Accounting C+")
MODELS_DIR = ROOT / "Techtoon.Core" / "Models"
DATA_DIR = ROOT / "Techtoon.Data" / "Context"

MODELS_DIR.mkdir(parents=True, exist_ok=True)
DATA_DIR.mkdir(parents=True, exist_ok=True)

# ========================================================
# MODELS
# ========================================================

(MODELS_DIR / "Account.cs").write_text("""using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("accounts")]
public class Account
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("code")]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("type")]
    [StringLength(50)]
    public string Type { get; set; } = "ASSET";

    [Column("parent_id")]
    public int? ParentId { get; set; }

    [ForeignKey("ParentId")]
    public Account? Parent { get; set; }
    public ICollection<Account> Children { get; set; } = new List<Account>();
    public ICollection<JournalLine> JournalLines { get; set; } = new List<JournalLine>();
    public ICollection<Party> Parties { get; set; } = new List<Party>();
    public ICollection<CashBox> CashBoxes { get; set; } = new List<CashBox>();
    public ICollection<Item> ItemsAsInventory { get; set; } = new List<Item>();
    public ICollection<Item> ItemsAsCogs { get; set; } = new List<Item>();
    public ICollection<Item> ItemsAsRevenue { get; set; } = new List<Item>();
    public ICollection<ExpenseCategory> ExpenseCategories { get; set; } = new List<ExpenseCategory>();
    public ICollection<Budget> Budgets { get; set; } = new List<Budget>();
    public ICollection<CostAllocation> CostAllocations { get; set; } = new List<CostAllocation>();
}
""", encoding="utf-8")

(MODELS_DIR / "User.cs").write_text("""using System.ComponentModel.DataAnnotations;
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
""", encoding="utf-8")

(MODELS_DIR / "Party.cs").write_text("""using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("parties")]
public class Party
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("type")]
    [StringLength(50)]
    public string Type { get; set; } = string.Empty;

    [Column("phone")]
    [StringLength(50)]
    public string? Phone { get; set; }

    [Column("address")]
    public string? Address { get; set; }

    [Column("credit_limit")]
    public double CreditLimit { get; set; }

    [Column("opening_balance")]
    public double OpeningBalance { get; set; }

    [Column("account_id")]
    public int AccountId { get; set; }

    [ForeignKey("AccountId")]
    public Account? Account { get; set; }

    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<CostHistory> CostHistories { get; set; } = new List<CostHistory>();
    public ICollection<RecurringInvoiceTemplate> RecurringTemplates { get; set; } = new List<RecurringInvoiceTemplate>();
}
""", encoding="utf-8")

(MODELS_DIR / "Item.cs").write_text("""using System.ComponentModel.DataAnnotations;
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
""", encoding="utf-8")

(MODELS_DIR / "Invoice.cs").write_text("""using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("invoices")]
public class Invoice
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("invoice_number")]
    [StringLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Column("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Column("due_date")]
    public DateTime? DueDate { get; set; }

    [Column("party_id")]
    public int PartyId { get; set; }
    [ForeignKey("PartyId")]
    public Party? Party { get; set; }

    [Column("type")]
    [StringLength(50)]
    public string Type { get; set; } = string.Empty;

    [Column("total_amount")]
    public double TotalAmount { get; set; }

    [Column("status")]
    [StringLength(50)]
    public string Status { get; set; } = "pending";

    [Column("currency_id")]
    public int CurrencyId { get; set; } = 1;
    [ForeignKey("CurrencyId")]
    public Currency? Currency { get; set; }

    [Column("discount_amount")]
    public double DiscountAmount { get; set; }

    [Column("discount_percentage")]
    public double DiscountPercentage { get; set; }

    [Column("tax_rate")]
    public double TaxRate { get; set; } = 14.0;

    [Column("tax_amount")]
    public double TaxAmount { get; set; }

    [Column("net_amount")]
    public double NetAmount { get; set; }

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    public ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();
    public ICollection<CostHistory> CostHistories { get; set; } = new List<CostHistory>();
}
""", encoding="utf-8")

(MODELS_DIR / "InvoiceLine.cs").write_text("""using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("invoice_lines")]
public class InvoiceLine
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("invoice_id")]
    public int InvoiceId { get; set; }
    [ForeignKey("InvoiceId")]
    public Invoice? Invoice { get; set; }

    [Column("item_id")]
    public int ItemId { get; set; }
    [ForeignKey("ItemId")]
    public Item? Item { get; set; }

    [Column("quantity")]
    public double Quantity { get; set; }

    [Column("price")]
    public double Price { get; set; }

    [Column("total")]
    public double Total { get; set; }

    [Column("cost_price")]
    public double CostPrice { get; set; }
}
""", encoding="utf-8")

(MODELS_DIR / "Payment.cs").write_text("""using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("payments")]
public class Payment
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Column("party_id")]
    public int PartyId { get; set; }
    [ForeignKey("PartyId")]
    public Party? Party { get; set; }

    [Column("amount")]
    public double Amount { get; set; }

    [Column("payment_type")]
    [StringLength(50)]
    public string PaymentType { get; set; } = string.Empty;

    [Column("payment_method")]
    [StringLength(100)]
    public string PaymentMethod { get; set; } = string.Empty;

    [Column("reference_number")]
    [StringLength(100)]
    public string? ReferenceNumber { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("currency_id")]
    public int CurrencyId { get; set; } = 1;
    [ForeignKey("CurrencyId")]
    public Currency? Currency { get; set; }

    [Column("cash_box_id")]
    public int CashBoxId { get; set; } = 1;
    [ForeignKey("CashBoxId")]
    public CashBox? CashBox { get; set; }
}
""", encoding="utf-8")

(MODELS_DIR / "Currency.cs").write_text("""using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("currencies")]
public class Currency
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("code")]
    [StringLength(10)]
    public string Code { get; set; } = string.Empty;

    [Column("name")]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [Column("symbol")]
    [StringLength(10)]
    public string Symbol { get; set; } = string.Empty;

    [Column("exchange_rate")]
    public double ExchangeRate { get; set; } = 1.0;

    [Column("is_default")]
    public bool IsDefault { get; set; }

    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
""", encoding="utf-8")

(MODELS_DIR / "CashBox.cs").write_text("""using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("cash_boxes")]
public class CashBox
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

    [Column("type")]
    [StringLength(50)]
    public string Type { get; set; } = "CASH";

    [Column("account_id")]
    public int AccountId { get; set; }
    [ForeignKey("AccountId")]
    public Account? Account { get; set; }

    [Column("responsible_person")]
    [StringLength(100)]
    public string? ResponsiblePerson { get; set; }

    [Column("max_limit")]
    public double? MaxLimit { get; set; }

    [Column("opening_balance")]
    public double OpeningBalance { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<CashTransfer> TransfersFrom { get; set; } = new List<CashTransfer>();
    public ICollection<CashTransfer> TransfersTo { get; set; } = new List<CashTransfer>();
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
}
""", encoding="utf-8")

(MODELS_DIR / "JournalEntry.cs").write_text("""using System.ComponentModel.DataAnnotations;
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
""", encoding="utf-8")

(MODELS_DIR / "JournalLine.cs").write_text("""using System.ComponentModel.DataAnnotations;
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
""", encoding="utf-8")

# --- باقي النماذج المختصرة ---
(MODELS_DIR / "AccountingPeriod.cs").write_text("""using System.ComponentModel.DataAnnotations;
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
""", encoding="utf-8")

(MODELS_DIR / "Category.cs").write_text("""using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("categories")]
public class Category
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("parent_id")]
    public int? ParentId { get; set; }

    [ForeignKey("ParentId")]
    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = new List<Category>();

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    public ICollection<Item> Items { get; set; } = new List<Item>();
}
""", encoding="utf-8")

(MODELS_DIR / "KitComponent.cs").write_text("""using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("kit_components")]
public class KitComponent
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("kit_item_id")]
    public int KitItemId { get; set; }
    [ForeignKey("KitItemId")]
    public Item? KitItem { get; set; }

    [Column("component_item_id")]
    public int ComponentItemId { get; set; }
    [ForeignKey("ComponentItemId")]
    public Item? ComponentItem { get; set; }

    [Column("quantity")]
    public double Quantity { get; set; } = 1.0;
}
""", encoding="utf-8")

(MODELS_DIR / "InventoryMovement.cs").write_text("""using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("inventory_movements")]
public class InventoryMovement
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("item_id")]
    public int ItemId { get; set; }
    [ForeignKey("ItemId")]
    public Item? Item { get; set; }

    [Column("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Column("type")]
    [StringLength(50)]
    public string Type { get; set; } = string.Empty;

    [Column("quantity")]
    public double Quantity { get; set; }

    [Column("reference_id")]
    public int? ReferenceId { get; set; }
}
""", encoding="utf-8")

(MODELS_DIR / "Warehouse.cs").write_text("""using System.ComponentModel.DataAnnotations;
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
""", encoding="utf-8")

(MODELS_DIR / "StockLevel.cs").write_text("""using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("stock_levels")]
public class StockLevel
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("warehouse_id")]
    public int WarehouseId { get; set; }
    [ForeignKey("WarehouseId")]
    public Warehouse? Warehouse { get; set; }

    [Column("item_id")]
    public int ItemId { get; set; }
    [ForeignKey("ItemId")]
    public Item? Item { get; set; }

    [Column("quantity")]
    public double Quantity { get; set; }

    [Column("last_updated")]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
""", encoding="utf-8")

(MODELS_DIR / "WarehouseTransfer.cs").write_text("""using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("warehouse_transfers")]
public class WarehouseTransfer
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Column("from_warehouse_id")]
    public int FromWarehouseId { get; set; }
    [ForeignKey("FromWarehouseId")]
    public Warehouse? FromWarehouse { get; set; }

    [Column("to_warehouse_id")]
    public int ToWarehouseId { get; set; }
    [ForeignKey("ToWarehouseId")]
    public Warehouse? ToWarehouse { get; set; }

    [Column("item_id")]
    public int ItemId { get; set; }
    [ForeignKey("ItemId")]
    public Item? Item { get; set; }

    [Column("quantity")]
    public double Quantity { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("status")]
    [StringLength(50)]
    public string Status { get; set; } = "completed";
}
""", encoding="utf-8")

(MODELS_DIR / "StockCount.cs").write_text("""using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("stock_counts")]
public class StockCount
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Column("warehouse_id")]
    public int WarehouseId { get; set; }
    [ForeignKey("WarehouseId")]
    public Warehouse? Warehouse { get; set; }

    [Column("item_id")]
    public int ItemId { get; set; }
    [ForeignKey("ItemId")]
    public Item? Item { get; set; }

    [Column("system_quantity")]
    public double SystemQuantity { get; set; }

    [Column("counted_quantity")]
    public double CountedQuantity { get; set; }

    [Column("difference")]
    public double Difference { get; set; }
}
""", encoding="utf-8")

(MODELS_DIR / "Loan.cs").write_text("""using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("loans")]
public class Loan
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("loan_number")]
    [StringLength(50)]
    public string LoanNumber { get; set; } = string.Empty;

    [Column("loan_type")]
    [StringLength(50)]
    public string LoanType { get; set; } = "RECEIVED";

    [Column("borrower_name")]
    [StringLength(100)]
    public string BorrowerName { get; set; } = string.Empty;

    [Column("principal_amount")]
    public double PrincipalAmount { get; set; }

    [Column("interest_rate")]
    public double InterestRate { get; set; }

    [Column("loan_date")]
    public DateTime LoanDate { get; set; }

    [Column("maturity_date")]
    public DateTime MaturityDate { get; set; }

    [Column("term_months")]
    public int TermMonths { get; set; }

    [Column("installment_amount")]
    public double InstallmentAmount { get; set; }

    [Column("total_interest")]
    public double TotalInterest { get; set; }

    [Column("total_amount")]
    public double TotalAmount { get; set; }

    [Column("paid_amount")]
    public double PaidAmount { get; set; }

    [Column("remaining_amount")]
    public double RemainingAmount { get; set; }

    [Column("status")]
    [StringLength(50)]
    public string Status { get; set; } = "ACTIVE";

    public ICollection<LoanInstallment> Installments { get; set; } = new List<LoanInstallment>();
}
""", encoding="utf-8")

(MODELS_DIR / "LoanInstallment.cs").write_text("""using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("loan_installments")]
public class LoanInstallment
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("loan_id")]
    public int LoanId { get; set; }
    [ForeignKey("LoanId")]
    public Loan? Loan { get; set; }

    [Column("installment_number")]
    public int InstallmentNumber { get; set; }

    [Column("due_date")]
    public DateTime DueDate { get; set; }

    [Column("principal_amount")]
    public double PrincipalAmount { get; set; }

    [Column("interest_amount")]
    public double InterestAmount { get; set; }

    [Column("total_amount")]
    public double TotalAmount { get; set; }

    [Column("paid_amount")]
    public double PaidAmount { get; set; }

    [Column("paid_date")]
    public DateTime? PaidDate { get; set; }

    [Column("status")]
    [StringLength(50)]
    public string Status { get; set; } = "pending";
}
""", encoding="utf-8")

(MODELS_DIR / "Employee.cs").write_text("""using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("employees")]
public class Employee
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("employee_code")]
    [StringLength(20)]
    public string EmployeeCode { get; set; } = string.Empty;

    [Column("full_name")]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Column("phone")]
    [StringLength(50)]
    public string? Phone { get; set; }

    [Column("job_title")]
    [StringLength(100)]
    public string JobTitle { get; set; } = string.Empty;

    [Column("hire_date")]
    public DateTime HireDate { get; set; }

    [Column("basic_salary")]
    public double BasicSalary { get; set; }

    [Column("allowances")]
    public double Allowances { get; set; }

    [Column("deductions")]
    public double Deductions { get; set; }

    [Column("social_insurance")]
    public double SocialInsurance { get; set; }

    [Column("tax_rate")]
    public double TaxRate { get; set; }

    [Column("status")]
    [StringLength(50)]
    public string Status { get; set; } = "ACTIVE";

    public ICollection<SalaryRecord> SalaryRecords { get; set; } = new List<SalaryRecord>();
}
""", encoding="utf-8")

(MODELS_DIR / "SalaryRecord.cs").write_text("""using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("salary_records")]
public class SalaryRecord
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("employee_id")]
    public int EmployeeId { get; set; }
    [ForeignKey("EmployeeId")]
    public Employee? Employee { get; set; }

    [Column("month")]
    public int Month { get; set; }

    [Column("year")]
    public int Year { get; set; }

    [Column("basic_salary")]
    public double BasicSalary { get; set; }

    [Column("allowances")]
    public double Allowances { get; set; }

    [Column("overtime")]
    public double Overtime { get; set; }

    [Column("bonus")]
    public double Bonus { get; set; }

    [Column("gross_salary")]
    public double GrossSalary { get; set; }

    [Column("social_insurance")]
    public double SocialInsurance { get; set; }

    [Column("tax")]
    public double Tax { get; set; }

    [Column("other_deductions")]
    public double OtherDeductions { get; set; }

    [Column("total_deductions")]
    public double TotalDeductions { get; set; }

    [Column("net_salary")]
    public double NetSalary { get; set; }

    [Column("payment_date")]
    public DateTime? PaymentDate { get; set; }

    [Column("payment_method")]
    [StringLength(50)]
    public string? PaymentMethod { get; set; }

    [Column("status")]
    [StringLength(50)]
    public string Status { get; set; } = "pending";
}
""", encoding="utf-8")

(MODELS_DIR / "Expense.cs").write_text("""using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("expenses")]
public class Expense
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Column("amount")]
    public double Amount { get; set; }

    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Column("payment_method")]
    [StringLength(50)]
    public string PaymentMethod { get; set; } = string.Empty;

    [Column("cash_box_id")]
    public int CashBoxId { get; set; }
    [ForeignKey("CashBoxId")]
    public CashBox? CashBox { get; set; }
}
""", encoding="utf-8")

(MODELS_DIR / "CostHistory.cs").write_text("""using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("cost_history")]
public class CostHistory
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("item_id")]
    public int ItemId { get; set; }
    [ForeignKey("ItemId")]
    public Item? Item { get; set; }

    [Column("invoice_id")]
    public int InvoiceId { get; set; }
    [ForeignKey("InvoiceId")]
    public Invoice? Invoice { get; set; }

    [Column("purchase_price")]
    public double PurchasePrice { get; set; }

    [Column("quantity")]
    public double Quantity { get; set; }

    [Column("total_cost")]
    public double TotalCost { get; set; }

    [Column("purchase_date")]
    public DateTime PurchaseDate { get; set; }

    [Column("supplier_id")]
    public int? SupplierId { get; set; }
    [ForeignKey("SupplierId")]
    public Party? Supplier { get; set; }
}
""", encoding="utf-8")

(MODELS_DIR / "CashTransfer.cs").write_text("""using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("cash_transfers")]
public class CashTransfer
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Column("from_cash_box_id")]
    public int FromCashBoxId { get; set; }
    [ForeignKey("FromCashBoxId")]
    public CashBox? FromCashBox { get; set; }

    [Column("to_cash_box_id")]
    public int ToCashBoxId { get; set; }
    [ForeignKey("ToCashBoxId")]
    public CashBox? ToCashBox { get; set; }

    [Column("amount")]
    public double Amount { get; set; }
}
""", encoding="utf-8")

(MODELS_DIR / "ExpenseCategory.cs").write_text("""using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("expense_categories")]
public class ExpenseCategory
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("account_id")]
    public int AccountId { get; set; }
    [ForeignKey("AccountId")]
    public Account? Account { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}
""", encoding="utf-8")

(MODELS_DIR / "CostCenter.cs").write_text("""using System.ComponentModel.DataAnnotations;
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
""", encoding="utf-8")

(MODELS_DIR / "CostAllocation.cs").write_text("""using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("cost_allocations")]
public class CostAllocation
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("account_id")]
    public int AccountId { get; set; }
    [ForeignKey("AccountId")]
    public Account? Account { get; set; }

    [Column("allocation_percentage")]
    public double AllocationPercentage { get; set; }

    [Column("month")]
    public int Month { get; set; }

    [Column("year")]
    public int Year { get; set; }
}
""", encoding="utf-8")

(MODELS_DIR / "Budget.cs").write_text("""using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("budgets")]
public class Budget
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("account_id")]
    public int AccountId { get; set; }
    [ForeignKey("AccountId")]
    public Account? Account { get; set; }

    [Column("month")]
    public int Month { get; set; }

    [Column("year")]
    public int Year { get; set; }

    [Column("budgeted_amount")]
    public double BudgetedAmount { get; set; }
}
""", encoding="utf-8")

(MODELS_DIR / "FixedAsset.cs").write_text("""using System.ComponentModel.DataAnnotations;
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
""", encoding="utf-8")

(MODELS_DIR / "DepreciationRecord.cs").write_text("""using System.ComponentModel.DataAnnotations;
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
""", encoding="utf-8")

(MODELS_DIR / "AppSetting.cs").write_text("""using System.ComponentModel.DataAnnotations;
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
""", encoding="utf-8")

(MODELS_DIR / "RecurringInvoiceTemplate.cs").write_text("""using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("recurring_invoice_templates")]
public class RecurringInvoiceTemplate
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("party_id")]
    public int PartyId { get; set; }
    [ForeignKey("PartyId")]
    public Party? Party { get; set; }

    [Column("invoice_type")]
    [StringLength(50)]
    public string InvoiceType { get; set; } = string.Empty;

    [Column("frequency")]
    [StringLength(50)]
    public string Frequency { get; set; } = "MONTHLY";

    [Column("start_date")]
    public DateTime StartDate { get; set; }

    [Column("next_run_date")]
    public DateTime NextRunDate { get; set; }

    [Column("tax_rate")]
    public double TaxRate { get; set; } = 14.0;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    public ICollection<RecurringInvoiceLine> Lines { get; set; } = new List<RecurringInvoiceLine>();
}
""", encoding="utf-8")

(MODELS_DIR / "RecurringInvoiceLine.cs").write_text("""using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("recurring_invoice_lines")]
public class RecurringInvoiceLine
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("template_id")]
    public int TemplateId { get; set; }
    [ForeignKey("TemplateId")]
    public RecurringInvoiceTemplate? Template { get; set; }

    [Column("item_id")]
    public int ItemId { get; set; }
    [ForeignKey("ItemId")]
    public Item? Item { get; set; }

    [Column("quantity")]
    public double Quantity { get; set; }

    [Column("price")]
    public double Price { get; set; }
}
""", encoding="utf-8")

# ========================================================
# DbContext
# ========================================================

(DATA_DIR / "TechtoonDbContext.cs").write_text("""using Microsoft.EntityFrameworkCore;
using Techtoon.Core.Models;

namespace Techtoon.Data.Context;

public class TechtoonDbContext : DbContext
{
    public TechtoonDbContext() { }
    public TechtoonDbContext(DbContextOptions<TechtoonDbContext> options) : base(options) { }

    public DbSet<Currency> Currencies { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<AccountingPeriod> AccountingPeriods { get; set; }
    public DbSet<Party> Parties { get; set; }
    public DbSet<CashBox> CashBoxes { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Item> Items { get; set; }
    public DbSet<CostHistory> CostHistory { get; set; }
    public DbSet<KitComponent> KitComponents { get; set; }
    public DbSet<InventoryMovement> InventoryMovements { get; set; }
    public DbSet<CostCenter> CostCenters { get; set; }
    public DbSet<CostAllocation> CostAllocations { get; set; }
    public DbSet<JournalEntry> JournalEntries { get; set; }
    public DbSet<JournalLine> JournalLines { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoiceLine> InvoiceLines { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<CashTransfer> CashTransfers { get; set; }
    public DbSet<ExpenseCategory> ExpenseCategories { get; set; }
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<Warehouse> Warehouses { get; set; }
    public DbSet<StockLevel> StockLevels { get; set; }
    public DbSet<WarehouseTransfer> WarehouseTransfers { get; set; }
    public DbSet<StockCount> StockCounts { get; set; }
    public DbSet<FixedAsset> FixedAssets { get; set; }
    public DbSet<DepreciationRecord> DepreciationRecords { get; set; }
    public DbSet<Loan> Loans { get; set; }
    public DbSet<LoanInstallment> LoanInstallments { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<SalaryRecord> SalaryRecords { get; set; }
    public DbSet<Budget> Budgets { get; set; }
    public DbSet<RecurringInvoiceTemplate> RecurringInvoiceTemplates { get; set; }
    public DbSet<RecurringInvoiceLine> RecurringInvoiceLines { get; set; }
    public DbSet<AppSetting> AppSettings { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql("Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=npg_K9nHVkY5OjRC;Database=neondb;SSL Mode=Require;Trust Server Certificate=true;Pooling=true;Timeout=15;CommandTimeout=60");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Account>(e => {
            e.HasIndex(a => a.Code).IsUnique();
            e.HasOne(a => a.Parent).WithMany(a => a.Children).HasForeignKey(a => a.ParentId);
        });

        modelBuilder.Entity<User>(e => e.HasIndex(u => u.Username).IsUnique());
        modelBuilder.Entity<Party>(e => e.HasOne(p => p.Account).WithMany(a => a.Parties).HasForeignKey(p => p.AccountId));
        modelBuilder.Entity<CashBox>(e => { e.HasIndex(c => c.Code).IsUnique(); e.HasOne(c => c.Account).WithMany(a => a.CashBoxes).HasForeignKey(c => c.AccountId); });
        modelBuilder.Entity<Category>(e => { e.HasIndex(c => c.Name).IsUnique(); e.HasOne(c => c.Parent).WithMany(c => c.Children).HasForeignKey(c => c.ParentId); });

        modelBuilder.Entity<Item>(e => {
            e.HasOne(i => i.Category).WithMany(c => c.Items).HasForeignKey(i => i.CategoryId);
            e.HasOne(i => i.InventoryAccount).WithMany(a => a.ItemsAsInventory).HasForeignKey(i => i.InventoryAccountId);
            e.HasOne(i => i.CogsAccount).WithMany(a => a.ItemsAsCogs).HasForeignKey(i => i.CogsAccountId);
            e.HasOne(i => i.RevenueAccount).WithMany(a => a.ItemsAsRevenue).HasForeignKey(i => i.RevenueAccountId);
        });

        modelBuilder.Entity<KitComponent>(e => {
            e.HasOne(k => k.KitItem).WithMany(i => i.Components).HasForeignKey(k => k.KitItemId);
            e.HasOne(k => k.ComponentItem).WithMany(i => i.UsedInKits).HasForeignKey(k => k.ComponentItemId);
        });

        modelBuilder.Entity<JournalLine>(e => {
            e.HasOne(jl => jl.Entry).WithMany(je => je.Lines).HasForeignKey(jl => jl.EntryId);
            e.HasOne(jl => jl.Account).WithMany(a => a.JournalLines).HasForeignKey(jl => jl.AccountId);
        });

        modelBuilder.Entity<Invoice>(e => {
            e.HasIndex(i => i.InvoiceNumber).IsUnique();
            e.HasOne(i => i.Party).WithMany(p => p.Invoices).HasForeignKey(i => i.PartyId);
        });

        modelBuilder.Entity<InvoiceLine>(e => {
            e.HasOne(il => il.Invoice).WithMany(i => i.Lines).HasForeignKey(il => il.InvoiceId);
            e.HasOne(il => il.Item).WithMany(i => i.InvoiceLines).HasForeignKey(il => il.ItemId);
        });

        modelBuilder.Entity<Payment>(e => {
            e.HasOne(p => p.Party).WithMany(p => p.Payments).HasForeignKey(p => p.PartyId);
            e.HasOne(p => p.CashBox).WithMany(c => c.Payments).HasForeignKey(p => p.CashBoxId);
        });

        modelBuilder.Entity<Warehouse>(e => { e.HasIndex(w => w.Name).IsUnique(); e.HasIndex(w => w.Code).IsUnique(); });
        modelBuilder.Entity<Loan>(e => e.HasIndex(l => l.LoanNumber).IsUnique());
        modelBuilder.Entity<Employee>(e => e.HasIndex(emp => emp.EmployeeCode).IsUnique());
        modelBuilder.Entity<AppSetting>(e => { e.HasIndex(s => s.Key).IsUnique(); e.HasIndex(s => s.Category); });
    }
}
""", encoding="utf-8")

print("=== Models and DbContext created successfully ===")