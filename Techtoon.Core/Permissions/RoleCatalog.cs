using System;
using System.Collections.Generic;
using System.Linq;

namespace Techtoon.Core.Permissions;

/// <summary>تعريف دور (Role) مع صلاحياته الافتراضية.</summary>
public class RoleDefinition
{
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsSystemRole { get; set; } = true;
    public List<string> Permissions { get; set; } = new();
}

/// <summary>
/// الأدوار الجاهزة في النظام مع صلاحياتها الافتراضية.
/// </summary>
public static class RoleCatalog
{
    /// <summary>الأدوار النظامية.</summary>
    public static List<RoleDefinition> All() => new()
    {
        new RoleDefinition
        {
            Name = "ADMIN",
            DisplayName = "مدير النظام",
            Description = "صلاحية كاملة على كل الشاشات والعمليات، بما فيها المستخدمون والصلاحيات.",
            Permissions = PermissionCatalog.AllKeys.ToList()
        },
        new RoleDefinition
        {
            Name = "MANAGER",
            DisplayName = "مدير",
            Description = "كل العمليات التشغيلية والتقارير، دون إدارة المستخدمين والأدوار ودون حذف بيانات العمل.",
            Permissions = PermissionCatalog.AllKeys
                .Where(k => !k.StartsWith("users.") && k != "settings.data")
                .ToList()
        },
        new RoleDefinition
        {
            Name = "ACCOUNTANT",
            DisplayName = "محاسب",
            Description = "الفواتير والمدفوعات والمالية والقيود والتقارير والمخزون.",
            Permissions = Keys(
                "dashboard.view",
                "parties.view", "parties.edit", "parties.discount",
                "items.view", "items.cost",
                "invoices.view", "invoices.edit", "invoices.discount", "invoices.price",
                "payments.view", "payments.edit",
                "finance.view", "finance.edit", "finance.expenses",
                "reports.view", "reports.profit", "reports.inventory",
                "inventory.view", "inventory.adjust", "inventory.audit",
                "cashboxes.view", "cashboxes.transfer",
                "accounts.view",
                "journals.view", "journals.edit", "journals.post",
                "periods.view",
                "subscriptions.view", "subscriptions.edit", "subscriptions.charge",
                "loans.view", "loans.edit", "loans.repay",
                "pettycash.view",
                "costs.view", "costs.edit",
                "assets.view", "budgets.view",
                "bankrec.view", "bankrec.edit",
                "bankstmt.view", "bankstmt.import",
                "baddebt.view", "baddebt.edit",
                "discounts.view", "fees.view")
        },
        new RoleDefinition
        {
            Name = "SALESPERSON",
            DisplayName = "مندوب مبيعات",
            Description = "فواتير البيع والأطراف والأصناف والعروض — بلا مالية أو مخزون.",
            Permissions = Keys(
                "dashboard.view",
                "parties.view", "parties.edit", "parties.discount",
                "items.view",
                "invoices.view", "invoices.edit", "invoices.discount", "invoices.price",
                "payments.view")
        },
        new RoleDefinition
        {
            Name = "CASHIER",
            DisplayName = "كاشير",
            Description = "تحصيل المدفوعات والفواتير وعرض الخزائن.",
            Permissions = Keys(
                "dashboard.view",
                "parties.view",
                "items.view",
                "invoices.view",
                "payments.view", "payments.edit",
                "cashboxes.view",
                "inventory.view")
        },
        new RoleDefinition
        {
            Name = "STOREKEEPER",
            DisplayName = "أمين مخزن",
            Description = "الأصناف والمخازن والجرد والتسويات والأرصدة.",
            Permissions = Keys(
                "dashboard.view",
                "parties.view",
                "items.view", "items.edit",
                "warehouses.view", "warehouses.edit",
                "inventory.view", "inventory.count", "inventory.adjust",
                "inventory.damaged", "inventory.writeoff", "inventory.transfer", "inventory.audit",
                "invoices.view",
                "reports.inventory")
        },
        new RoleDefinition
        {
            Name = "VIEWER",
            DisplayName = "مطّلع (عرض فقط)",
            Description = "عرض كل الصفحات دون أي تعديل (بلا الإعدادات ولا إدارة المستخدمين).",
            Permissions = PermissionCatalog.AllKeys
                .Where(k => k.EndsWith(".view") && k != "settings.view" && k != "users.view")
                .ToList()
        }
    };

    /// <summary>يبحث عن دور بالاسم.</summary>
    public static RoleDefinition? Find(string name) =>
        All().FirstOrDefault(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase));

    private static List<string> Keys(params string[] keys) => keys.ToList();
}
