using System;
using System.Collections.Generic;
using System.Linq;

namespace Techtoon.Core.Permissions;

/// <summary>
/// كتالوج الصلاحيات — المصدر الوحيد لأسماء الصلاحيات في النظام.
/// كل صلاحية بصيغة <c>module.action</c>، والمجموعات (modules) تُستخدم لعرض
/// شاشة الصلاحيات بشكل منظّم.
/// </summary>
public static class PermissionCatalog
{
    /// <summary>تعريف صلاحية واحدة.</summary>
    public sealed class PermissionDef
    {
        public string Key { get; set; } = "";
        public string Label { get; set; } = "";
        public string Module { get; set; } = "";
        public string ModuleLabel { get; set; } = "";
    }

    private static List<PermissionDef> Build() => new()
    {
        // لوحة التحكم
        P("dashboard.view", "عرض لوحة التحكم", "dashboard"),

        // العملاء والموردون
        P("parties.view", "عرض العملاء والموردين", "parties"),
        P("parties.edit", "إضافة وتعديل الأطراف", "parties"),
        P("parties.delete", "حذف الأطراف", "parties"),
        P("parties.discount", "منح خصومات على الأطراف", "parties"),

        // الأصناف
        P("items.view", "عرض الأصناف", "items"),
        P("items.edit", "إضافة وتعديل الأصناف", "items"),
        P("items.delete", "حذف الأصناف", "items"),
        P("items.cost", "عرض أسعار التكلفة", "items"),
        P("items.kit", "إدارة مكوّنات الكيت", "items"),

        // الفواتير
        P("invoices.view", "عرض الفواتير", "invoices"),
        P("invoices.edit", "إنشاء وتعديل الفواتير", "invoices"),
        P("invoices.delete", "حذف الفواتير", "invoices"),
        P("invoices.discount", "منح خصم على الفاتورة", "invoices"),
        P("invoices.price", "تعديل الأسعار", "invoices"),
        P("invoices.cancel", "إلغاء الفواتير", "invoices"),

        // المدفوعات
        P("payments.view", "عرض المدفوعات", "payments"),
        P("payments.edit", "إضافة وتعديل المدفوعات", "payments"),
        P("payments.delete", "حذف المدفوعات", "payments"),

        // المصروفات والمالية
        P("finance.view", "عرض البيانات المالية", "finance"),
        P("finance.edit", "تعديل البيانات المالية", "finance"),
        P("finance.expenses", "إضافة المصروفات", "finance"),

        // التقارير
        P("reports.view", "عرض التقارير", "reports"),
        P("reports.profit", "عرض تقارير الأرباح", "reports"),
        P("reports.inventory", "عرض تقارير المخزون", "reports"),
        P("reports.settings", "إعدادات التقارير", "reports"),

        // الاعتراف بالإيراد
        P("revenue.view", "عرض الاعتراف بالإيراد", "revenue"),
        P("revenue.manage", "إدارة الاعتراف بالإيراد", "revenue"),

        // المخزون
        P("inventory.view", "عرض أرصدة المخزون", "inventory"),
        P("inventory.count", "إنشاء جلسات الجرد", "inventory"),
        P("inventory.adjust", "تسويات المخزون", "inventory"),
        P("inventory.damaged", "تسجيل الأصناف التالفة", "inventory"),
        P("inventory.writeoff", "طلبات الشطب", "inventory"),
        P("inventory.transfer", "تحويل بين المخازن", "inventory"),
        P("inventory.audit", "سجل تدقيق المخزون", "inventory"),

        // الخزائن
        P("cashboxes.view", "عرض الخزائن", "cashboxes"),
        P("cashboxes.edit", "إضافة وتعديل الخزائن", "cashboxes"),
        P("cashboxes.transfer", "التحويل بين الخزائن", "cashboxes"),
        P("cashboxes.close", "إغلاق الوردية", "cashboxes"),

        // الموظفون
        P("employees.view", "عرض الموظفين", "employees"),
        P("employees.edit", "إضافة وتعديل الموظفين", "employees"),
        P("employees.salary", "كشفوف الرواتب والتسويات", "employees"),

        // المخازن
        P("warehouses.view", "عرض المخازن", "warehouses"),
        P("warehouses.edit", "إضافة وتعديل المخازن", "warehouses"),

        // دليل الحسابات
        P("accounts.view", "عرض دليل الحسابات", "accounts"),
        P("accounts.edit", "تعديل دليل الحسابات", "accounts"),

        // القيود
        P("journals.view", "عرض القيود المحاسبية", "journals"),
        P("journals.edit", "إضافة وتعديل القيود", "journals"),
        P("journals.post", "ترحيل القيود", "journals"),

        // الفترات المحاسبية
        P("periods.view", "عرض الفترات المحاسبية", "periods"),
        P("periods.edit", "إضافة وتعديل الفترات", "periods"),
        P("periods.close", "إغلاق الفترات المحاسبية", "periods"),

        // الاشتراكات
        P("subscriptions.view", "عرض الاشتراكات", "subscriptions"),
        P("subscriptions.edit", "إضافة وتعديل الاشتراكات", "subscriptions"),
        P("subscriptions.charge", "تحصيل periods الاشتراكات", "subscriptions"),

        // القروض والسلف
        P("loans.view", "عرض السلف والقروض", "loans"),
        P("loans.edit", "إضافة وتعديل السلف والقروض", "loans"),
        P("loans.repay", "تسديد الأقساط والسداد", "loans"),
        P("pettycash.view", "عرض العهدة", "pettycash"),
        P("pettycash.edit", "إدارة العهدة", "pettycash"),

        // مراكز التكلفة والأصول
        P("costs.view", "عرض مراكز التكلفة وتوزيع التكاليف", "costs"),
        P("costs.edit", "تعديل مراكز التكلفة وتوزيع التكاليف", "costs"),
        P("assets.view", "عرض الأصول الثابتة", "assets"),
        P("assets.edit", "إدارة الأصول الثابتة", "assets"),
        P("budgets.view", "عرض الميزانيات", "budgets"),
        P("budgets.edit", "تعديل الميزانيات", "budgets"),

        // التسوية البنكية وكشوف البنك
        P("bankrec.view", "عرض التسوية البنكية", "bankrec"),
        P("bankrec.edit", "إجراء التسوية البنكية", "bankrec"),
        P("bankstmt.view", "عرض كشوف البنك", "bankstmt"),
        P("bankstmt.import", "استيراد كشوف البنك", "bankstmt"),

        // الديون المعدومة والخصومات
        P("baddebt.view", "عرض الديون المعدومة", "baddebt"),
        P("baddebt.edit", "إضافة وتعديل شطب الديون", "baddebt"),
        P("baddebt.approve", "اعتماد شطب الديون", "baddebt"),
        P("discounts.view", "عرض سياسات الخصم", "discounts"),
        P("discounts.edit", "تعديل سياسات الخصم", "discounts"),
        P("fees.view", "عرض رسوم التأخير", "fees"),
        P("fees.edit", "تعديل رسوم التأخير", "fees"),

        // سجل التدقيق
        P("audit.view", "عرض سجل التدقيق", "audit"),

        // الإعدادات والبيانات
        P("settings.view", "عرض الإعدادات", "settings"),
        P("settings.backup", "إنشاء نسخة احتياطية", "settings"),
        P("settings.restore", "استعادة نسخة احتياطية", "settings"),
        P("settings.data", "حذف بيانات العمل", "settings"),
        P("settings.import", "استيراد البيانات", "settings"),
        P("settings.export", "تصدير البيانات", "settings"),

        // المستخدمون والصلاحيات
        P("users.view", "عرض المستخدمين", "users"),
        P("users.edit", "إضافة وتعديل المستخدمين", "users"),
        P("users.permissions", "إدارة صلاحيات المستخدمين", "users"),
        P("users.roles", "إدارة الأدوار والصلاحيات", "users"),
    };

    private static PermissionDef P(string key, string label, string module) => new()
    {
        Key = key,
        Label = label,
        Module = module,
        ModuleLabel = ModuleLabelOf(module)
    };

    /// <summary>كل الصلاحيات.</summary>
    public static IReadOnlyList<PermissionDef> All { get; } = Build();

    /// <summary>الصلاحيات مجمّعة حسب الوحدة (Module).</summary>
    public static IReadOnlyList<IGrouping<string, PermissionDef>> ByModule { get; } =
        All.GroupBy(p => p.Module).ToList();

    /// <summary>كل مفاتيح الصلاحيات.</summary>
    public static IReadOnlyList<string> AllKeys { get; } = All.Select(p => p.Key).ToList();

    /// <summary>الأسماء العربية للوحدات.</summary>
    public static string ModuleLabelOf(string module) => module switch
    {
        "dashboard" => "لوحة التحكم",
        "parties" => "العملاء والموردون",
        "items" => "الأصناف",
        "invoices" => "الفواتير",
        "payments" => "المدفوعات",
        "finance" => "المالية والمصروفات",
        "reports" => "التقارير",
        "revenue" => "الاعتراف بالإيراد",
        "inventory" => "المخزون",
        "cashboxes" => "الخزائن",
        "employees" => "الموظفون",
        "warehouses" => "المخازن",
        "accounts" => "دليل الحسابات",
        "journals" => "القيود المحاسبية",
        "periods" => "الفترات المحاسبية",
        "subscriptions" => "الاشتراكات",
        "loans" => "السلف والقروض",
        "pettycash" => "العهدة",
        "costs" => "مراكز التكلفة",
        "assets" => "الأصول الثابتة",
        "budgets" => "الميزانيات",
        "bankrec" => "التسوية البنكية",
        "bankstmt" => "كشوف البنك",
        "baddebt" => "الديون المعدومة",
        "discounts" => "سياسات الخصم",
        "fees" => "رسوم التأخير",
        "audit" => "سجل التدقيق",
        "settings" => "الإعدادات والبيانات",
        "users" => "المستخدمون والصلاحيات",
        _ => module
    };

    /// <summary>
    /// الصلاحية المطلوبة لعرض صفحة — تُستخدم لإخفاء عناصر القائمة الجانبية
    /// ومنع الانتقال المباشر لصفحة غير مصرّح بها.
    /// </summary>
    public static string PagePermission(string pageKey) => pageKey switch
    {
        "Dashboard" => "dashboard.view",
        "Invoices" => "invoices.view",
        "Parties" => "parties.view",
        "Payments" => "payments.view",
        "Items" => "items.view",
        "Warehouses" => "warehouses.view",
        "StockLevels" => "inventory.view",
        "WarehouseTransfers" => "inventory.transfer",
        "StockCounts" => "inventory.count",
        "StockAdjustments" => "inventory.adjust",
        "DamagedItems" => "inventory.damaged",
        "WriteoffRequests" => "inventory.writeoff",
        "Employees" => "employees.view",
        "Salaries" => "employees.salary",
        "Loans" => "loans.view",
        "PettyCash" => "pettycash.view",
        "CashBoxes" => "cashboxes.view",
        "BankReconciliation" => "bankrec.view",
        "ExpenseCategories" => "finance.view",
        "BadDebts" => "baddebt.view",
        "EarlyPaymentDiscount" => "discounts.view",
        "LatePaymentFees" => "fees.view",
        "Accounts" => "accounts.view",
        "RecurringJournals" => "journals.view",
        "RecurringInvoices" => "subscriptions.view",
        "ReportingSettings" => "reports.settings",
        "RevenueRecognition" => "revenue.view",
        "BankStatementImport" => "bankstmt.view",
        "CostCenters" => "costs.view",
        "CostAllocations" => "costs.view",
        "AccountingPeriods" => "periods.view",
        "JournalEntries" => "journals.view",
        "Budgets" => "budgets.view",
        "FixedAssets" => "assets.view",
        "Reports" => "reports.view",
        "AuditLog" => "audit.view",
        "Settings" => "settings.view",
        "Users" => "users.view",
        "DataManagement" => "settings.data",
        "DataExchange" => "settings.import",
        "RolesPermissions" => "users.roles",
        _ => ""
    };

    /// <summary>كل الصفحات التي لها صلاحية.</summary>
    public static IReadOnlyList<string> AllPages { get; } = new[]
    {
        "Dashboard", "Invoices", "Parties", "Payments", "Items", "Warehouses", "StockLevels",
        "WarehouseTransfers", "StockCounts", "StockAdjustments", "DamagedItems", "WriteoffRequests",
        "Employees", "Salaries", "Loans", "PettyCash", "CashBoxes", "BankReconciliation",
        "ExpenseCategories", "BadDebts", "EarlyPaymentDiscount", "LatePaymentFees", "Accounts",
        "RecurringJournals", "RecurringInvoices", "ReportingSettings", "RevenueRecognition",
        "BankStatementImport", "CostCenters", "CostAllocations", "AccountingPeriods", "JournalEntries",
        "Budgets", "FixedAssets", "Reports", "AuditLog", "Settings", "Users", "DataManagement",
        "DataExchange", "RolesPermissions"
    };

    /// <summary>
    /// مفاتيح الصلاحيات القديمة (ما قبل نظام الأدوار) وبدائلها الحديثة.
    /// نُبقيها تعمل كي لا تنكسر واجهة الـ API و Checks القديمة:
    /// طلب الصلاحية القديمة يُقبل إذا مُنحت الصلاحية الحديثة المقابلة.
    /// </summary>
    public static IReadOnlyDictionary<string, string[]> LegacyAliases { get; } = new Dictionary<string, string[]>
    {
        ["sales"] = new[] { "invoices.view", "invoices.edit", "payments.view" },
        ["returns"] = new[] { "invoices.edit", "invoices.cancel" },
        ["discounts"] = new[] { "discounts.view", "discounts.edit", "invoices.discount", "parties.discount" },
        ["edit_price"] = new[] { "invoices.price" },
        ["cancel_invoice"] = new[] { "invoices.cancel" },
        ["expenses"] = new[] { "finance.view", "finance.edit", "finance.expenses", "pettycash.edit" },
        ["profit_reports"] = new[] { "reports.view", "reports.profit", "accounts.view", "journals.view" },
        ["backup"] = new[] { "settings.backup", "settings.restore" },
        ["settings"] = new[] { "settings.view" },
        ["manage_users"] = new[] { "users.view", "users.edit", "users.permissions", "users.roles" },
        ["stock_adjustment"] = new[] { "inventory.adjust" },
        ["inventory.count.create"] = new[] { "inventory.count" },
        ["inventory.count.count"] = new[] { "inventory.count" },
        ["inventory.count.review"] = new[] { "inventory.count" },
        ["inventory.count.approve"] = new[] { "inventory.count" },
        ["inventory.adjustment.create"] = new[] { "inventory.adjust" },
        ["inventory.adjustment.approve"] = new[] { "inventory.adjust" },
        ["inventory.adjustment.execute"] = new[] { "inventory.adjust" },
        ["inventory.damaged.create"] = new[] { "inventory.damaged" },
        ["inventory.damaged.inspect"] = new[] { "inventory.damaged" },
        ["inventory.writeoff.approve"] = new[] { "inventory.writeoff" },
        ["inventory.writeoff.execute"] = new[] { "inventory.writeoff" },
        ["inventory.reports.view"] = new[] { "reports.inventory" },
        ["inventory.audit.view"] = new[] { "inventory.audit" }
    };

    /// <summary>كل المفاتيح القديمة — للتوافق مع الشاشات القديمة.</summary>
    public static IReadOnlyList<string> LegacyKeys { get; } = LegacyAliases.Keys.ToList();

    /// <summary>
    /// الخريطة العكسية: من صلاحية حديثة → المفاتيح القديمة التي تُقبل معها.
    /// تُحسب تلقائيًا من <see cref="LegacyAliases"/>.
    /// </summary>
    public static IReadOnlyDictionary<string, string[]> ReverseAliases { get; } = BuildReverse();

    private static Dictionary<string, string[]> BuildReverse()
    {
        var map = new Dictionary<string, List<string>>();
        foreach (var (legacy, modern) in LegacyAliases)
        {
            foreach (var m in modern)
            {
                if (!map.TryGetValue(m, out var list)) { list = new List<string>(); map[m] = list; }
                list.Add(legacy);
            }
        }
        return map.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray());
    }

    /// <summary>
    /// هل هذا المفتاح القديم؟ (مفتاح لا يحتوي على نقطة).
    /// </summary>
    public static bool IsLegacyKey(string key) => LegacyAliases.ContainsKey(key);

    /// <summary>كل المفاتيح: الحديثة + القديمة (لشاشة إدارة الصلاحيات).</summary>
    public static IReadOnlyList<string> AllKeysIncludingLegacy { get; } =
        AllKeys.Concat(LegacyKeys).Distinct().ToList();
}
