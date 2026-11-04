using Microsoft.EntityFrameworkCore;
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
    public DbSet<InvoicePayment> InvoicePayments { get; set; }

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

        // تكوين علاقات CashTransfer (لها مفتاحان أجنبيان指向 CashBox)
        modelBuilder.Entity<CashTransfer>(e =>
        {
            e.HasOne(t => t.FromCashBox)
             .WithMany(c => c.TransfersFrom)
             .HasForeignKey(t => t.FromCashBoxId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(t => t.ToCashBox)
             .WithMany(c => c.TransfersTo)
             .HasForeignKey(t => t.ToCashBoxId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // تكوين علاقات WarehouseTransfer (لها مفتاحان أجنبيان指向 Warehouse)
        modelBuilder.Entity<WarehouseTransfer>(e =>
        {
            e.HasOne(t => t.FromWarehouse)
             .WithMany(w => w.TransfersFrom)
             .HasForeignKey(t => t.FromWarehouseId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(t => t.ToWarehouse)
             .WithMany(w => w.TransfersTo)
             .HasForeignKey(t => t.ToWarehouseId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InvoicePayment>(e => {
            e.HasOne(p => p.Invoice).WithMany().HasForeignKey(p => p.InvoiceId);
            e.HasOne(p => p.CashBox).WithMany().HasForeignKey(p => p.CashBoxId);
        });
    }
}
