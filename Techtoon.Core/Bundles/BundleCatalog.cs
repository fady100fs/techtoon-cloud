using System.Collections.Generic;

namespace Techtoon.Core.Bundles;

/// <summary>وصف مجمّع بيانات قابل للاستيراد/التصدير.</summary>
public class BundleDefinition
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "📦";
    /// <summary>الجداول داخل المجمّع بترتيب الاستيراد (المراجع يجب أن تأتي قبل صاحبها).</summary>
    public List<BundleTableSpec> Tables { get; set; } = new();
}

/// <summary>
/// تعريفات المجمّعات الجاهزة.
///
/// <para><b>المبدأ:</b> كل مُجمّع مجموعة جداول مترابطة تُنقل معًا في ملف واحد،
/// وتُستورد ذرّيًا (كله أو لا شيء) مع <b>مفاتيح طبيعية</b> (كود الحساب، باركود الصنف، …)
/// بدل الأرقام — لذلك تعمل الحزمة بين أي قاعدتين.</para>
///
/// <para><b>ملاحظة:</b> الأعمدة مُتحقَّق منها مقابل المخطط الحقيقي، وأي عمود غير موجود
/// في إصدار سابق من المخطط يُتجاهل تلقائيًا (حماية من انحراف المخطط).</para>
/// </summary>
public static class BundleCatalog
{
    // ---------- أدوات بناء الأعمدة ----------

    private static BundleColumn C(string db, string type = "text", bool required = false, string? def = null) =>
        new BundleColumn { Db = db, Export = db, Type = type, Required = required, DefaultValue = def };

    /// <summary>
    /// مرجع بسيط: عمود FK ← عمود المفتاح في الهدف + اسم العمود في الملف.
    /// </summary>
    /// <param name="fk">عمود FK في الجدول المصدر.</param>
    /// <param name="targetTable">الجدول الهدف.</param>
    /// <param name="exportKey">اسم عمود المفتاح داخل الملف.</param>
    /// <param name="targetKey">عمود المفتاح الطبيعي في الجدول الهدف (إن اختلف عن اسم الملف).</param>
    /// <param name="required">هل المرجع إلزامي؟</param>
    private static BundleRef R(string fk, string targetTable, string exportKey,
        string targetKey = "", bool required = true)
    {
        var tk = string.IsNullOrEmpty(targetKey) ? exportKey : targetKey;
        return new BundleRef
        {
            ForeignKey = fk,
            TargetTable = targetTable,
            TargetKeys = new List<string> { tk },
            ExportKeys = new List<string> { exportKey },
            Required = required
        };
    }

    /// <summary>مرجع بمفتاح مركّب (أعمدة مختلفة في الملف والهدف).</summary>
    private static BundleRef R2(string fk, string targetTable, List<string> targetKeys, List<string> exportKeys, bool required = true)
        => new BundleRef
        {
            ForeignKey = fk,
            TargetTable = targetTable,
            TargetKeys = targetKeys,
            ExportKeys = exportKeys,
            Required = required
        };

    // ---------- الجداول المشتركة ----------

    /// <summary>الحسابات — المفتاح الطبيعي = code.</summary>
    public static BundleTableSpec Accounts() => new()
    {
        Table = "accounts",
        KeyColumns = new List<string> { "code" },
        OrderBy = "code",
        Columns = new List<BundleColumn>
        {
            C("code", required: true),
            C("name", required: true),
            C("type", required: true, def: "ASSET"),
            C("parent_id", "int"),
        },
        References = new List<BundleRef>
        {
            R("parent_id", "accounts", "parent_code", "code", required: false),
        }
    };

    /// <summary>الأطراف (عملاء/موردون) — المفتاح = name + type.</summary>
    public static BundleTableSpec Parties() => new()
    {
        Table = "parties",
        KeyColumns = new List<string> { "name", "type" },
        OrderBy = "name",
        Columns = new List<BundleColumn>
        {
            C("name", required: true),
            C("type", required: true, def: "customer"),
            C("account_id", "int"),
            C("phone", "text"),
            C("address", "text"),
            C("credit_limit", "decimal"),
            C("opening_balance", "decimal"),
            C("is_active", "bool", def: "true"),
        },
        References = new List<BundleRef>
        {
            // حساب الطرف إجباري في قاعدة البيانات (NOT NULL) — إن لم يذكره الملف
            // يُستعمل حساب الأطراف الافتراضي بدل فشل الاستيراد برسالة PostgreSQL خام.
            new BundleRef
            {
                ForeignKey = "account_id",
                TargetTable = "accounts",
                TargetKeys = new List<string> { "code" },
                ExportKeys = new List<string> { "account_code" },
                Required = false,
                DefaultKey = "DEFAULT-PARTY",
                DefaultName = "حساب الأطراف (افتراضي)"
            },
        }
    };

    /// <summary>الأصناف — المفتاح = barcode.</summary>
    public static BundleTableSpec Items() => new()
    {
        Table = "items",
        KeyColumns = new List<string> { "barcode" },
        // barcode قد يكون فارغًا — نستخدم name كبديل حتى لا تُنسخ الصفوف في كل استيراد
        KeyFallbacks = new List<string?> { "name" },
        OrderBy = "name",
        Columns = new List<BundleColumn>
        {
            C("name", required: true),
            C("barcode", "text"),
            C("sell_price", "decimal", def: "0"),
            C("cost_price", "decimal", def: "0"),
            C("avg_cost_price", "decimal", def: "0"),
            C("min_stock", "decimal", def: "0"),
            C("opening_quantity", "decimal", def: "0"),
            C("total_purchased_qty", "decimal", def: "0"),
            C("category_id", "int"),
            C("is_kit", "bool", def: "false"),
            C("is_active", "bool", def: "true"),
        },
        References = new List<BundleRef>
        {
            R("category_id", "categories", "category_name", "name", required: false),
        }
    };

    /// <summary>الأصناف الخاصة (كيت) — المفتاح = (kit, component).</summary>
    public static BundleTableSpec KitComponents() => new()
    {
        Table = "kit_components",
        KeyColumns = new List<string> { "kit_item_id", "component_item_id" },
        OrderBy = "kit_item_id",
        Columns = new List<BundleColumn>
        {
            C("kit_item_id", "int"),
            C("component_item_id", "int"),
            C("quantity", "decimal", required: true),
            C("warehouse_id", "int"),
        },
        References = new List<BundleRef>
        {
            R("kit_item_id", "items", "kit_barcode", "barcode"),
            R("component_item_id", "items", "component_barcode", "barcode"),
            R("warehouse_id", "warehouses", "warehouse_code", "code", required: false),
        }
    };

    public static BundleTableSpec Categories() => new()
    {
        Table = "categories",
        KeyColumns = new List<string> { "name" },
        OrderBy = "name",
        Columns = new List<BundleColumn>
        {
            C("name", required: true),
            C("description", "text"),
            C("is_active", "bool", def: "true"),
        }
    };

    public static BundleTableSpec Warehouses() => new()
    {
        Table = "warehouses",
        KeyColumns = new List<string> { "code" },
        OrderBy = "code",
        Columns = new List<BundleColumn>
        {
            C("name", required: true),
            C("code", required: true),
            C("location", "text"),
            C("responsible_person", "text"),
            C("is_active", "bool", def: "true"),
        }
    };

    public static BundleTableSpec CashBoxes() => new()
    {
        Table = "cash_boxes",
        KeyColumns = new List<string> { "code" },
        OrderBy = "code",
        Columns = new List<BundleColumn>
        {
            C("name", required: true),
            C("code", required: true),
            C("account_id", "int"),
            C("type", "text"),
            C("opening_balance", "decimal", def: "0"),
            C("max_limit", "decimal"),
            C("responsible_person", "text"),
            C("is_active", "bool", def: "true"),
        },
        References = new List<BundleRef>
        {
            R("account_id", "accounts", "account_code", "code"),
        }
    };

    public static BundleTableSpec StockLevels() => new()
    {
        Table = "stock_levels",
        KeyColumns = new List<string> { "warehouse_id", "item_id" },
        Columns = new List<BundleColumn>
        {
            C("warehouse_id", "int"),
            C("item_id", "int"),
            C("quantity", "decimal", def: "0"),
            C("reserved_quantity", "decimal", def: "0"),
            C("damaged_quantity", "decimal", def: "0"),
            C("under_inspection_quantity", "decimal", def: "0"),
        },
        References = new List<BundleRef>
        {
            R("warehouse_id", "warehouses", "warehouse_code", "code"),
            R("item_id", "items", "item_barcode", "barcode"),
        }
    };

    /// <summary>العملات — المفتاح الطبيعي = code (EGP / USD / SAR).</summary>
    public static BundleTableSpec Currencies() => new()
    {
        Table = "currencies",
        KeyColumns = new List<string> { "code" },
        OrderBy = "id",
        Columns = new List<BundleColumn>
        {
            C("code", required: true),
            C("name", required: true),
            C("symbol", required: true),
            C("exchange_rate", "decimal", def: "1"),
            C("is_default", "bool", def: "false"),
        }
    };

    /// <summary>الفواتير — المفتاح = invoice_number.</summary>
    public static BundleTableSpec Invoices(string? where = null) => new()
    {
        Table = "invoices",
        KeyColumns = new List<string> { "invoice_number" },
        OrderBy = "invoice_number",
        Where = where,
        Columns = new List<BundleColumn>
        {
            C("invoice_number", required: true),
            C("date", "datetime"),
            C("due_date", "datetime"),
            C("party_id", "int"),
            C("type", required: true),
            C("warehouse_id", "int"),
            C("subtotal", "decimal", def: "0"),
            C("discount_amount", "decimal", def: "0"),
            C("total", "decimal", def: "0"),
            C("paid_amount", "decimal", def: "0"),
            C("status", "text"),
            C("notes", "text"),
            // currency_id إلزامي في قاعدة البيانات — بلا عمود في الحزمة يفشل الإدراج
            C("currency_id", "int", def: "1"),
        },
        References = new List<BundleRef>
        {
            R("party_id", "parties", "party_name", "name", required: false),
            R("warehouse_id", "warehouses", "warehouse_code", "code", required: false),
            R("currency_id", "currencies", "currency_code", "code", required: false),
        }
    };

    public static BundleTableSpec InvoiceLines() => new()
    {
        Table = "invoice_lines",
        KeyColumns = new List<string> { "invoice_id", "item_id", "quantity" },
        Columns = new List<BundleColumn>
        {
            C("invoice_id", "int"),
            C("item_id", "int"),
            C("quantity", "decimal", required: true),
            C("price", "decimal", def: "0"),
            C("discount", "decimal", def: "0"),
            C("total", "decimal", def: "0"),
        },
        References = new List<BundleRef>
        {
            R("invoice_id", "invoices", "invoice_number", "invoice_number"),
            R("item_id", "items", "item_barcode", "barcode"),
        }
    };

    public static BundleTableSpec InvoicePayments() => new()
    {
        Table = "invoice_payments",
        KeyColumns = new List<string> { "invoice_id", "amount", "method" },
        Columns = new List<BundleColumn>
        {
            C("invoice_id", "int"),
            C("method", "text", required: true),
            C("amount", "decimal", required: true),
            C("reference", "text"),
            C("paid_at", "datetime"),
        },
        References = new List<BundleRef>
        {
            R("invoice_id", "invoices", "invoice_number", "invoice_number"),
        }
    };

    public static BundleTableSpec Payments() => new()
    {
        Table = "payments",
        // date قد يكون NULL — المطابقة تستخدم IS NOT DISTINCT FROM فلا تتكرر الصفوف
        KeyColumns = new List<string> { "date", "amount", "payment_type" },
        OrderBy = "date",
        Columns = new List<BundleColumn>
        {
            C("date", "datetime"),
            C("party_id", "int"),
            C("amount", "decimal", required: true),
            C("payment_type", required: true),
            C("payment_method", required: true),
            C("reference_number", "text"),
            C("notes", "text"),
            C("cash_box_id", "int"),
            C("account_id", "int"),
            // currency_id إلزامي في قاعدة البيانات — بلا عمود في الحزمة يفشل الإدراج
            C("currency_id", "int", def: "1"),
        },
        References = new List<BundleRef>
        {
            R("party_id", "parties", "party_name", "name", required: false),
            R("cash_box_id", "cash_boxes", "cash_box_code", "code"),
            R("account_id", "accounts", "account_code", "code", required: false),
            R("currency_id", "currencies", "currency_code", "code", required: false),
        }
    };

    public static BundleTableSpec Expenses() => new()
    {
        Table = "expenses",
        KeyColumns = new List<string> { "date", "description", "amount" },
        OrderBy = "date",
        Columns = new List<BundleColumn>
        {
            C("date", "datetime", required: true),
            C("category_id", "int"),
            C("amount", "decimal", required: true),
            C("description", "text", required: true),
            C("payment_method", "text"),
            C("cash_box_id", "int"),
            C("notes", "text"),
        },
        References = new List<BundleRef>
        {
            R("category_id", "expense_categories", "category_name", "name", required: false),
            R("cash_box_id", "cash_boxes", "cash_box_code", "code", required: false),
        }
    };

    public static BundleTableSpec ExpenseCategories() => new()
    {
        Table = "expense_categories",
        KeyColumns = new List<string> { "name" },
        OrderBy = "name",
        Columns = new List<BundleColumn>
        {
            C("name", required: true),
            C("description", "text"),
            C("is_active", "bool", def: "true"),
        }
    };

    public static BundleTableSpec RecurringInvoiceTemplates() => new()    {
        Table = "recurring_invoice_templates",
        KeyColumns = new List<string> { "name" },
        OrderBy = "name",
        Columns = new List<BundleColumn>
        {
            C("name", required: true),
            C("party_id", "int"),
            C("invoice_type", required: true, def: "sales"),
            C("frequency", required: true),
            C("start_date", "date", required: true),
            C("end_date", "date"),
            C("next_run_date", "date"),
            C("discount_percentage", "decimal", def: "0"),
            C("tax_rate", "decimal", def: "0"),
            C("notes", "text"),
            C("is_active", "bool", def: "true"),
        },
        References = new List<BundleRef>
        {
            R("party_id", "parties", "party_name", "name", required: false),
        }
    };

    public static BundleTableSpec RecurringInvoiceLines() => new()
    {
        Table = "recurring_invoice_lines",
        KeyColumns = new List<string> { "template_id", "item_id", "quantity" },
        Columns = new List<BundleColumn>
        {
            C("template_id", "int"),
            C("item_id", "int"),
            C("quantity", "decimal", required: true),
            C("price", "decimal", def: "0"),
        },
        References = new List<BundleRef>
        {
            R("template_id", "recurring_invoice_templates", "template_name", "name"),
            R("item_id", "items", "item_barcode", "barcode"),
        }
    };

    /// <summary>
    /// الاشتراكات — المفتاح الطبيعي = (الطرف + اسم الخدمة + تاريخ البدء).
    /// <para>المفتاح في الملف يعتمد على <b>اسم الطرف</b> لا رقمه — لذلك ينتقل بين أي قاعدتين.</para>
    /// </summary>
    public static BundleTableSpec Subscriptions() => new()
    {
        Table = "subscriptions",
        KeyColumns = new List<string> { "party_id", "service_name", "start_date" },
        OrderBy = "id",
        Columns = new List<BundleColumn>
        {
            C("party_id", "int"),
            C("service_name", required: true),
            C("description"),
            C("amount", "decimal", def: "0"),
            C("frequency", required: true, def: "monthly"),
            C("start_date", "date", required: true),
            C("end_date", "date"),
            C("auto_invoice", "bool", def: "true"),
            C("status", required: true, def: "active"),
            C("notes"),
        },
        References = new List<BundleRef>
        {
            R("party_id", "parties", "party_name", "name", required: true),
        }
    };

    /// <summary>
    /// فترات الاشتراك — المفتاح الطبيعي = (الاشتراك + رقم الفترة).
    /// <para>الاشتراك يُحدَّد في الملف باسمه وتاريخ بدئه واسم طرفه — لا بالأرقام.</para>
    /// </summary>
    public static BundleTableSpec SubscriptionPeriods() => new()
    {
        Table = "subscription_periods",
        KeyColumns = new List<string> { "subscription_id", "period_number" },
        OrderBy = "id",
        Columns = new List<BundleColumn>
        {
            C("subscription_id", "int"),
            C("period_number", "int", required: true),
            C("period_start", "date", required: true),
            C("period_end", "date", required: true),
            C("due_date", "date", required: true),
            C("amount", "decimal", def: "0"),
            C("paid_amount", "decimal", def: "0"),
            C("status", required: true, def: "due"),
            C("notes"),
        },
        References = new List<BundleRef>
        {
            new BundleRef
            {
                ForeignKey = "subscription_id",
                TargetTable = "subscriptions",
                // المفتاح الطبيعي للاشتراك: اسم الطرف + اسم الخدمة + تاريخ البدء
                TargetKeys = new List<string> { "", "service_name", "start_date" },
                ExportKeys = new List<string> { "subscription_party_name", "subscription_service_name", "subscription_start_date" },
                MatchKeys = new List<string> { "pj.name", "t.service_name", "t.start_date" },
                Required = true,
                CarryJoins = new List<BundleJoin>
                {
                    new BundleJoin
                    {
                        Alias = "pj",
                        Table = "parties",
                        On = "pj.id = ref0.party_id",
                        Column = "name",
                        ExportName = "subscription_party_name"
                    }
                },
                TargetJoins = new List<BundleJoin>
                {
                    new BundleJoin
                    {
                        Alias = "pj",
                        Table = "parties",
                        On = "pj.id = t.party_id"
                    }
                }
            },
        }
    };

    public static BundleTableSpec InventoryMovements() => new()
    {
        Table = "inventory_movements",
        KeyColumns = new List<string> { "item_id", "date", "type", "reference_type" },
        OrderBy = "date",
        Columns = new List<BundleColumn>
        {
            C("item_id", "int"),
            C("date", "datetime", required: true),
            C("type", required: true),
            C("quantity", "decimal", required: true),
            C("warehouse_id", "int"),
            C("reference_type", "text"),
            C("reference_id", "int"),
        },
        References = new List<BundleRef>
        {
            R("item_id", "items", "item_barcode", "barcode"),
            R("warehouse_id", "warehouses", "warehouse_code", "code", required: false),
        }
    };

    public static BundleTableSpec JournalEntries() => new()
    {
        Table = "journal_entries",
        KeyColumns = new List<string> { "date", "description" },
        OrderBy = "date",
        Columns = new List<BundleColumn>
        {
            C("date", "datetime", required: true),
            C("description", "text", required: true),
            C("reference_type", "text"),
            C("reference_id", "int"),
        }
    };

    public static BundleTableSpec JournalLines() => new()
    {
        Table = "journal_lines",
        KeyColumns = new List<string> { "entry_id", "account_id" },
        Columns = new List<BundleColumn>
        {
            C("entry_id", "int"),
            C("account_id", "int"),
            C("debit", "decimal", def: "0"),
            C("credit", "decimal", def: "0"),
        },
        References = new List<BundleRef>
        {
            R2("entry_id", "journal_entries",
                new List<string> { "date", "description" },
                new List<string> { "entry_date", "entry_description" }),
            R("account_id", "accounts", "account_code", "code"),
        }
    };

    // ---------- المجمّعات ----------

    /// <summary>كل المجمّعات المتاحة.</summary>
    public static List<BundleDefinition> All() => new()
    {
        new BundleDefinition
        {
            Id = "parties",
            Title = "العملاء والموردون",
            Icon = "👥",
            Description = "كل الأطراف (عملاء + موردون) مع حساباتهم المرتبطة — استيراد مُجمَّع ذرّي.",
            Tables = new List<BundleTableSpec> { Accounts(), Parties() }
        },
        new BundleDefinition
        {
            Id = "items",
            Title = "الأصناف والمخزون",
            Icon = "📦",
            Description = "الأصناف + التصنيفات + مكوّنات الكيت + المخازن + أرصدة المخزون.",
            Tables = new List<BundleTableSpec> { Accounts(), Categories(), Warehouses(), Items(), KitComponents(), StockLevels() }
        },
        new BundleDefinition
        {
            Id = "invoices",
            Title = "فواتير البيع",
            Icon = "🧾",
            Description = "فواتير البيع + سطورها + دفعاتها + الأطراف والأصناف والخزائن المرتبطة.",
            Tables = new List<BundleTableSpec> { Accounts(), Currencies(), Parties(), Categories(), Warehouses(), Items(), CashBoxes(), Invoices("s.type = 'sales'"), InvoiceLines(), InvoicePayments(), Payments() }
        },
        new BundleDefinition
        {
            Id = "purchases",
            Title = "فواتير الشراء",
            Icon = "🛒",
            Description = "فواتير الشراء + سطورها + مدفوعاتها + الأطراف والأصناف والخزائن المرتبطة.",
            Tables = new List<BundleTableSpec> { Accounts(), Currencies(), Parties(), Categories(), Warehouses(), Items(), CashBoxes(), Invoices("s.type = 'purchase'"), InvoiceLines(), InvoicePayments(), Payments() }
        },
        new BundleDefinition
        {
            Id = "subscriptions",
            Title = "الاشتراكات والفواتير المتكررة",
            Icon = "🔄",
            Description = "الاشتراكات وفتراتها (الاسم + تاريخ البدء + الطرف) + قوالب الفواتير المتكررة وخطوطها + الأطراف والأصناف. المفتاح بالاسم لا بالأرقام — ينتقل بين أي قاعدتين.",
            Tables = new List<BundleTableSpec>
            {
                Accounts(), Parties(), Categories(), Warehouses(), Items(),
                Subscriptions(), SubscriptionPeriods(),
                RecurringInvoiceTemplates(), RecurringInvoiceLines()
            }
        },
        new BundleDefinition
        {
            Id = "inventory",
            Title = "حركات المخزون",
            Icon = "📊",
            Description = "المخازن + الأرصدة + كل حركات المخزون (وارد/منصرف/تسوية) مع الأصناف.",
            Tables = new List<BundleTableSpec> { Accounts(), Categories(), Warehouses(), Items(), StockLevels(), InventoryMovements() }
        },
        new BundleDefinition
        {
            Id = "finance",
            Title = "البيانات المالية",
            Icon = "💰",
            Description = "الخزائن + المصروفات + القيود المحاسبية وسطورها + الحسابات والأطراف.",
            Tables = new List<BundleTableSpec> { Accounts(), Currencies(), Parties(), CashBoxes(), ExpenseCategories(), Expenses(), JournalEntries(), JournalLines(), Payments() }
        },
        new BundleDefinition
        {
            Id = "all",
            Title = "كل البيانات (نسخة كاملة)",
            Icon = "🗄️",
            Description = "نسخة كاملة: الحسابات + الأطراف + الأصناف + المخازن + الفواتير + الاشتراكات + المالية + المخزون.",
            Tables = new List<BundleTableSpec>
            {
                Accounts(), Currencies(), Categories(), Warehouses(), Items(), KitComponents(),
                Parties(), CashBoxes(), ExpenseCategories(),
                Invoices(), InvoiceLines(), InvoicePayments(),
                RecurringInvoiceTemplates(), RecurringInvoiceLines(),
                Payments(), Expenses(), StockLevels(), InventoryMovements(),
                JournalEntries(), JournalLines()
            }
        }
    };

    public static BundleDefinition? Find(string id) =>
        All().FirstOrDefault(b => string.Equals(b.Id, id, StringComparison.OrdinalIgnoreCase));
}
