using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;

namespace Techtoon.UI.Services
{
    /// <summary>نوع العملية على جدول ضمن مجموعة حذف.</summary>
    public enum DataStepKind
    {
        /// <summary>حذف السجلات المطابقة.</summary>
        Delete,
        /// <summary>تصفير قيم (بدون حذف السجل).</summary>
        Reset,
        /// <summary>فكّ الارتباط بجعل العمود NULL (يحفظ السجل الأصلي).</summary>
        Detach
    }

    /// <summary>مسارجك جدول آخر — يُستخدم لتحديد نطاق الحذف (عميل واحد / تاريخ معيّن).</summary>
    public class DataScopeRef
    {
        /// <summary>الجدول الأصل (مثل invoices).</summary>
        public string ParentTable { get; set; } = "";
        /// <summary>مفتاح الجدول الأصل (مثل id).</summary>
        public string ParentKey { get; set; } = "id";
        /// <summary>العمود في الجدول الحالي الذي يشير إلى الأصل (مثل invoice_id).</summary>
        public string ChildKey { get; set; } = "";
        /// <summary>عمود النطاق في الجدول الأصل (مثل party_id أو date).</summary>
        public string ParentScopeColumn { get; set; } = "";
    }

    /// <summary>خطوة واحدة داخل مجموعة حذف.</summary>
    public class DataStep
    {
        /// <summary>اسم الجدول.</summary>
        public string Table { get; set; } = "";
        /// <summary>شرط ثابت إضافي (بدون كلمة WHERE).</summary>
        public string? Where { get; set; }
        /// <summary>نوع العملية.</summary>
        public DataStepKind Kind { get; set; } = DataStepKind.Delete;
        /// <summary>قعدادات التصفير، مثل: quantity=0, reserved_quantity=0</summary>
        public string? SetAssignments { get; set; }
        /// <summary>وصف عربي للخطوة (يظهر في المعاينة).</summary>
        public string Label { get; set; } = "";
        /// <summary>عمود النطاق في هذا الجدول مباشرة (party_id أو date).</summary>
        public string? ScopeSelfColumn { get; set; }
        /// <summary>مسار النطاق عبر جدول آخر.</summary>
        public DataScopeRef? ScopeParent { get; set; }
    }

    /// <summary>مجموعة حذف (جزء من البيانات).</summary>
    public class DataGroup
    {
        /// <summary>المعرّف الفريد.</summary>
        public string Id { get; set; } = "";
        /// <summary>العنوان العربي.</summary>
        public string Title { get; set; } = "";
        /// <summary>الوصف العربي.</summary>
        public string Description { get; set; } = "";
        /// <summary>التصنيف: تشغيلية / أساسية / نظام.</summary>
        public string Category { get; set; } = "";
        /// <summary>الخطوات مرتبة: الأبناء قبل الآباء.</summary>
        public List<DataStep> Steps { get; set; } = new();
    }

    /// <summary>سطر في المعاينة.</summary>
    public class DataPreviewItem
    {
        public string TableName { get; set; } = "";
        public string Label { get; set; } = "";
        public long RowCount { get; set; }
        public string Action { get; set; } = "";
    }

    /// <summary>نتيجة معاينة.</summary>
    public class DataPreviewResult
    {
        public string Title { get; set; } = "";
        public List<DataPreviewItem> Items { get; set; } = new();
        public long TotalRows => Items.Sum(x => x.RowCount);
        public List<string> Notes { get; set; } = new();
    }

    /// <summary>نتيجة تنفيذ.</summary>
    public class DataDeleteResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        /// <summary>عدد السجلات التي حذفتها خطوات المجموعة (يطابق المعاينة تمامًا).</summary>
        public long AffectedRows { get; set; }
        /// <summary>عدد السجلات اليتيمة التي أزالها «كنس السلامة».</summary>
        public long SweptRows { get; set; }
        public List<string> Details { get; set; } = new();
        /// <summary>نتيجة كل خطوة على حدة (للمقارنة مع المعاينة).</summary>
        public List<DataStepResult> StepResults { get; set; } = new();
    }

    /// <summary>نتيجة تنفيذ خطوة واحدة.</summary>
    public class DataStepResult
    {
        public string Table { get; set; } = "";
        public string Label { get; set; } = "";
        public long Rows { get; set; }
        public string Action { get; set; } = "";
    }

    /// <summary>نطاق الحذف الجزئي.</summary>
    public class DeleteScope
    {
        /// <summary>نوع النطاق: party أو before_date.</summary>
        public string Kind { get; set; } = "";
        /// <summary>معرّف العميل (لـ party).</summary>
        public int? PartyId { get; set; }
        /// <summary>التاريخ (لـ before_date).</summary>
        public DateTime? Before { get; set; }
        /// <summary>حذف سجل العميل نفسه أيضًا (لـ party).</summary>
        public bool IncludePartyRecord { get; set; }
    }

    /// <summary>خيارات إعادة ضبط المصنع.</summary>
    public class FactoryResetOptions
    {
        /// <summary>الاحتفاظ بالمستخدمين والأدوار.</summary>
        public bool KeepUsersAndRoles { get; set; } = true;
        /// <summary>الاحتفاظ بالبيانات الأساسية (أطراف، أصناف، حسابات...).</summary>
        public bool KeepMasterData { get; set; } = false;
        /// <summary>الاحتفاظ بالإعدادات.</summary>
        public bool KeepSettings { get; set; } = true;
    }

    /// <summary>
    /// إدارة البيانات — حذف البيانات بالكامل أو بأجزاءها.
    /// <para><b>المبدأ:</b> كل جدول في القاعدة يجب أن يكون قابلاً للحذف، إمّا ضمن مجموعة
    /// عمل (فواتير، مخزون، مالية...) أو ضمن إعادة الضبط الكامل. لا يوجد جدول "خارج النطاق".</para>
    /// <para><b>السلامة:</b> كل عملية تُنفَّذ داخل معاملة واحدة (transaction) واحدة —
    /// إن فشل أي سطر يتم التراجع عن كل شيء. والخطوات مرتبة بحيث تُحذف السجلات الأبناء
    /// قبل الآباء حتى لا تترك سجلات يتيمة.</para>
    /// </summary>
    public static class DataManagementService
    {
        private static string GetCS() => SecureConnectionManager.GetConnectionString();

        /// <summary>
        /// شرط «الحساب غير مرتبط بأي سجل».
        /// القاعدة تحتوي قيود مفاتيح أجنبية قليلة، لذلك نمنع الحذف تجريبياً
        /// بتطبيق فحص صريح على كل الأعمدة التي تشير إلى accounts.
        /// </summary>
        private const string UnreferencedAccountsWhere = @"
id NOT IN (SELECT account_id FROM parties WHERE account_id IS NOT NULL)
AND id NOT IN (SELECT account_id FROM cash_boxes WHERE account_id IS NOT NULL)
AND id NOT IN (SELECT inventory_account_id FROM items WHERE inventory_account_id IS NOT NULL)
AND id NOT IN (SELECT cogs_account_id FROM items WHERE cogs_account_id IS NOT NULL)
AND id NOT IN (SELECT revenue_account_id FROM items WHERE revenue_account_id IS NOT NULL)
AND id NOT IN (SELECT account_id FROM expense_categories WHERE account_id IS NOT NULL)
AND id NOT IN (SELECT account_id FROM payments WHERE account_id IS NOT NULL)
AND id NOT IN (SELECT account_id FROM journal_lines WHERE account_id IS NOT NULL)
AND id NOT IN (SELECT account_id FROM budgets WHERE account_id IS NOT NULL)
AND id NOT IN (SELECT account_id FROM cost_allocations WHERE account_id IS NOT NULL)
AND id NOT IN (SELECT discount_account_id FROM invoice_discount_applications WHERE discount_account_id IS NOT NULL)
AND id NOT IN (SELECT fee_income_account_id FROM late_fee_policies WHERE fee_income_account_id IS NOT NULL)
AND id NOT IN (SELECT discount_account_id FROM early_payment_discount_policies WHERE discount_account_id IS NOT NULL)
AND id NOT IN (SELECT liability_account_id FROM loans WHERE liability_account_id IS NOT NULL)
AND id NOT IN (SELECT interest_expense_account_id FROM loans WHERE interest_expense_account_id IS NOT NULL)
AND id NOT IN (SELECT bank_account_id FROM loans WHERE bank_account_id IS NOT NULL)
AND id NOT IN (SELECT custody_account_id FROM petty_cash_custodies WHERE custody_account_id IS NOT NULL)
AND id NOT IN (SELECT expense_account_id FROM petty_cash_expenses WHERE expense_account_id IS NOT NULL)
AND id NOT IN (SELECT shortage_account_id FROM petty_cash_settlements WHERE shortage_account_id IS NOT NULL)
AND id NOT IN (SELECT fee_account_id FROM cash_transfers WHERE fee_account_id IS NOT NULL)
AND id NOT IN (SELECT in_transit_account_id FROM cash_transfers WHERE in_transit_account_id IS NOT NULL)
AND id NOT IN (SELECT target_account_id FROM reconciliation_adjustments WHERE target_account_id IS NOT NULL)
AND id NOT IN (SELECT account_id FROM recurring_journal_lines WHERE account_id IS NOT NULL)
AND id NOT IN (SELECT bad_debt_account_id FROM bad_debt_writeoffs WHERE bad_debt_account_id IS NOT NULL)
AND id NOT IN (SELECT ar_account_id FROM bad_debt_writeoffs WHERE ar_account_id IS NOT NULL)
AND id NOT IN (SELECT parent_id FROM accounts WHERE parent_id IS NOT NULL)";

        // ==================== 1) كتالوج المجموعات ====================

        private static DataStep Del(string table, string label, string? where = null,
            string? scopeSelf = null, DataScopeRef? scopeParent = null) => new()
        {
            Table = table,
            Label = label,
            Where = where,
            ScopeSelfColumn = scopeSelf,
            ScopeParent = scopeParent
        };

        private static DataScopeRef Ref(string parentTable, string childKey, string parentScopeColumn,
            string parentKey = "id") => new()
        {
            ParentTable = parentTable,
            ParentKey = parentKey,
            ChildKey = childKey,
            ParentScopeColumn = parentScopeColumn
        };

        /// <summary>القيود الناتجة عن الفواتير (تُحذف مع الفواتير حتى لا تبقى قيود بلا مصدر).</summary>
        private const string JournalOfInvoices =
            "((reference_type='invoice' AND reference_id IN (SELECT id FROM invoices)) " +
            "OR source_invoice_id IN (SELECT id FROM invoices))";

        /// <summary>خطوط القيود الناتجة عن الفواتير.</summary>
        private const string JournalLinesOfInvoices =
            "entry_id IN (SELECT id FROM journal_entries WHERE " + JournalOfInvoices + ")";

        /// <summary>
        /// يفكّ ارتباط السجلات التي تشير إلى مدفوعات م scheduled for removal.
        /// <paramref name="paymentWhere"/> شرط يحدد المدفوعات المستهدفة (null = كلها).
        /// </summary>
        private static void AddPaymentDetaches(List<DataStep> steps, string? paymentWhere,
            bool includeDiscountApplications = true)
        {
            string Scope(string column) => paymentWhere == null
                ? $"{column} IS NOT NULL"
                : $"{column} IS NOT NULL AND {column} IN (SELECT id FROM payments WHERE {paymentWhere})";

            var detaches = new List<DataStep>
            {
                new()
                {
                    Table = "bank_statement_lines",
                    Kind = DataStepKind.Detach,
                    SetAssignments = "matched_payment_id=NULL",
                    Where = Scope("matched_payment_id"),
                    Label = "فكّ ارتباط سطور كشف الحساب بالمدفوعات"
                },
                new()
                {
                    Table = "reconciliation_matches",
                    Kind = DataStepKind.Detach,
                    SetAssignments = "system_payment_id=NULL",
                    Where = Scope("system_payment_id"),
                    Label = "فكّ ارتباط المطابقات بالمدفوعات"
                }
            };

            if (includeDiscountApplications)
            {
                detaches.Add(new DataStep
                {
                    Table = "invoice_discount_applications",
                    Kind = DataStepKind.Detach,
                    SetAssignments = "payment_id=NULL",
                    Where = Scope("payment_id"),
                    Label = "فكّ ارتباط تطبيقات الخصم بالمدفوعات"
                });
            }

            // يجب أن تأتي قبل حذف المدفوعات نفسها — وإلا لم يبقَ ما نفكّ ارتباطه.
            var insertAt = steps.FindIndex(s => s.Table == "payments");
            if (insertAt < 0) insertAt = steps.Count;
            steps.InsertRange(insertAt, detaches);
        }

        /// <summary>
        /// يفكّ ارتباط السجلات التي تشير إلى حركات المخزون slated for removal.
        /// </summary>
        private static void AddMovementDetaches(List<DataStep> steps)
        {
            var detaches = new List<DataStep>
            {
                new()
                {
                    Table = "stock_adjustments",
                    Kind = DataStepKind.Detach,
                    SetAssignments = "movement_id=NULL",
                    Where = "movement_id IS NOT NULL",
                    Label = "فكّ ارتباط تسويات المخزون بالحركات"
                },
                new()
                {
                    Table = "writeoff_requests",
                    Kind = DataStepKind.Detach,
                    SetAssignments = "movement_id=NULL",
                    Where = "movement_id IS NOT NULL",
                    Label = "فكّ ارتباط طلبات الإتلاف بالحركات"
                }
            };

            var insertAt = steps.FindIndex(s => s.Table == "inventory_movements");
            if (insertAt < 0) insertAt = steps.Count;
            steps.InsertRange(insertAt, detaches);
        }

        /// <summary>كل مجموعات الحذف المتاحة في شاشة إدارة البيانات.</summary>
        public static IReadOnlyList<DataGroup> Groups { get; } = BuildGroups();

        /// <summary>الجداول التي يغطيها نظام إدارة البيانات (للتأكد من اكتمال التغطية).</summary>
        public static IReadOnlyList<string> CoveredTables { get; } = Groups
            .SelectMany(g => g.Steps)
            .Select(s => s.Table)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
            .ToList();

        private static IReadOnlyList<DataGroup> BuildGroups()
        {
            var list = new List<DataGroup>();

            // ---------- تشغيلية: الفواتير ----------
            var invoiceSteps = new List<DataStep>
            {
                Del("invoice_discount_applications", "تطبيقات الخصم على الفواتير"),
                Del("invoice_late_fees", "رسوم التأخير على الفواتير"),
                Del("invoice_payments", "دفعات الفواتير"),
                Del("invoice_lines", "سطور الفواتير"),
                Del("journal_lines", "خطوط قيود الفواتير", JournalLinesOfInvoices),
                Del("journal_entries", "القيود المحاسبية الناتجة عن الفواتير", JournalOfInvoices),
                Del("cost_history", "تاريخ تكلفة الفواتير", "invoice_id IN (SELECT id FROM invoices)"),
                new()
                {
                    Table = "bad_debt_writeoffs",
                    Kind = DataStepKind.Detach,
                    SetAssignments = "invoice_id=NULL",
                    Where = "invoice_id IS NOT NULL AND invoice_id IN (SELECT id FROM invoices)",
                    Label = "فكّ ارتباط شطب الديون بالفواتير"
                },
                Del("payments", "سندات القبض/الصرف المرتبطة بفواتير", "invoice_id IS NOT NULL"),
                Del("inventory_movements", "حركات المخزون الناتجة عن الفواتير", "reference_type='invoice'"),
                Del("invoices", "الفواتير")
            };
            AddPaymentDetaches(invoiceSteps, "invoice_id IS NOT NULL", includeDiscountApplications: false);

            list.Add(new DataGroup
            {
                Id = "invoices",
                Title = "الفواتير والمبيعات",
                Description = "يحذف كل الفواتير مع سطورها ومدفوعاتها ورسوم التأخير والخصومات المطبقة عليها.",
                Category = "تشغيلية",
                Steps = invoiceSteps
            });

            // ---------- تشغيلية: المخزون ----------
            var inventorySteps = new List<DataStep>
            {
                Del("inventory_movements", "حركات المخزون"),
                Del("cost_history", "تاريخ تكلفة الأصناف"),
                Del("kit_component_history", "سجل تعديلات مكونات الأصناف المجمّعة"),
                new()
                {
                    Table = "stock_levels",
                    Kind = DataStepKind.Reset,
                    SetAssignments = "quantity=0, reserved_quantity=0, damaged_quantity=0, under_inspection_quantity=0",
                    Label = "تصفير أرصدة الأصناف في المخازن"
                },
                new()
                {
                    Table = "items",
                    Kind = DataStepKind.Reset,
                    SetAssignments = "opening_quantity=0",
                    Label = "تصفير أرصدة الافتتاح للأصناف"
                }
            };
            AddMovementDetaches(inventorySteps);

            list.Add(new DataGroup
            {
                Id = "inventory",
                Title = "المخزون والأرصدة",
                Description = "يحذف حركات المخزون وتاريخ التكلفة ويصفّر أرصدة الأصناف warehouses وأرصدة الافتتاح.",
                Category = "تشغيلية",
                Steps = inventorySteps
            });

            // ---------- تشغيلية: الجرد والتلف ----------
            list.Add(new DataGroup
            {
                Id = "stock_ops",
                Title = "الجرد والتلف والتسويات",
                Description = "يحذف جلسات الجرد وخطوط الجرد وتسويات المخزون وسجلات التالف وطلبات الإتلاف.",
                Category = "تشغيلية",
                Steps = new List<DataStep>
                {
                    Del("damaged_item_inspection_log", "سجل معاينة الأصناف التالفة"),
                    Del("custody_increase_requests", "طلبات زيادة الحيازة"),
                    Del("writeoff_requests", "طلبات الإتلاف"),
                    Del("damaged_items", "سجلات الأصناف التالفة"),
                    Del("stock_counts", "خطوط الجرد"),
                    Del("stock_count_sessions", "جلسات الجرد"),
                    Del("stock_adjustments", "تسويات المخزون")
                }
            });

            // ---------- تشغيلية: الحركات المالية ----------
            var financeSteps = new List<DataStep>
            {
                Del("journal_lines", "خطوط القيود المحاسبية"),
                Del("cash_recognition_log", "سجل تحقق النقد"),
                Del("journal_entries", "القيود المحاسبية"),
                Del("cash_transfer_log", "سجل تحويلات الخزائن"),
                Del("cash_transfers", "التحويلات بين الخزائن"),
                Del("expenses", "المصروفات"),
                Del("payments", "سندات القبض والصرف"),
                Del("shift_closings", "إقفال الورديات")
            };
            AddPaymentDetaches(financeSteps, null);

            list.Add(new DataGroup
            {
                Id = "finance",
                Title = "الحركات المالية",
                Description = "يحذف القيود المحاسبية وخطوطها والمدفوعات والمصروفات والتحويلات بين الخزائن وإقفال الورديات.",
                Category = "تشغيلية",
                Steps = financeSteps
            });

            // ---------- تشغيلية: الاشتراكات ----------
            list.Add(new DataGroup
            {
                Id = "subscriptions",
                Title = "الاشتراكات",
                Description = "يحذف الاشتراكات وفتراتها الشهرية.ملاحظة: الفترات مرتبطة بحساب جار العملاء، لذا لن تبقى أي فاتورة مرتبطة.",
                Category = "تشغيلية",
                Steps = new List<DataStep>
                {
                    Del("subscription_periods", "فترات الاشتراك"),
                    Del("subscriptions", "الاشتراكات")
                }
            });

            // ---------- تشغيلية: السلف ----------
            list.Add(new DataGroup
            {
                Id = "loans",
                Title = "السلف والقرض",
                Description = "يحذف السلف وأقساطها ومدفوعاتها وسجل إعادة الجدولة وحركات السحب على الحساب.",
                Category = "تشغيلية",
                Steps = new List<DataStep>
                {
                    Del("loan_rescheduling_history", "سجل إعادة جدولة السلف"),
                    Del("loan_payments", "مدفوعات السلف"),
                    Del("loan_installments", "أقساط السلف"),
                    Del("overdraft_transactions", "حركات السحب على الحساب"),
                    Del("loans", "السلف")
                }
            });

            // ---------- تشغيلية: الخزينة المصغرة ----------
            list.Add(new DataGroup
            {
                Id = "petty_cash",
                Title = "الخزينة المصغرة",
                Description = "يحذف الحيازات الصغيرة ومصاريفها وتسوياتها.",
                Category = "تشغيلية",
                Steps = new List<DataStep>
                {
                    Del("petty_cash_expenses", "مصاريف الخزينة المصغرة"),
                    Del("petty_cash_settlements", "تسويات الخزينة المصغرة"),
                    Del("petty_cash_custodies", "حيازات الخزينة المصغرة")
                }
            });

            // ---------- تشغيلية: الرواتب ----------
            list.Add(new DataGroup
            {
                Id = "salaries",
                Title = "الرواتب",
                Description = "يحذف كشوف الرواتب الشهرية لكل الموظفين (سجلات الموظفين نفسها تبقى).",
                Category = "تشغيلية",
                Steps = new List<DataStep>
                {
                    Del("salary_records", "كشوف الرواتب")
                }
            });

            // ---------- تشغيلية: الموظفون ----------
            list.Add(new DataGroup
            {
                Id = "employees",
                Title = "الموظفون",
                Description = "يحذف سجلات الموظفين. تُحذف معها الرواتب والحيازات التي تخصهم، وتُفكّ ارتباط السلف حتى لا تترك سجلات يتيمة.",
                Category = "أساسية",
                Steps = new List<DataStep>
                {
                    Del("salary_records", "كشوف الرواتب"),
                    Del("petty_cash_custodies", "حيازات الخزينة المصغرة"),
                    new()
                    {
                        Table = "loans",
                        Kind = DataStepKind.Detach,
                        SetAssignments = "employee_id=NULL",
                        Where = "employee_id IS NOT NULL",
                        Label = "فكّ ارتباط السلف بالموظفين"
                    },
                    Del("employees", "سجلات الموظفين")
                }
            });

            // ---------- تشغيلية: الأصول الثابتة ----------
            list.Add(new DataGroup
            {
                Id = "fixed_assets",
                Title = "الأصول الثابتة",
                Description = "يحذف الأصول الثابتة وسجلات إهلاكها.",
                Category = "تشغيلية",
                Steps = new List<DataStep>
                {
                    Del("depreciation_records", "سجلات الإهلاك"),
                    Del("fixed_assets", "الأصول الثابتة")
                }
            });

            // ---------- تشغيلية: كشوف الحساب ----------
            list.Add(new DataGroup
            {
                Id = "bank",
                Title = "كشوف الحساب والمطابقة",
                Description = "يحذف كشوف الحساب البنكية وسطورها والمطابقات والتسويات وقوالب الاستيراد.",
                Category = "تشغيلية",
                Steps = new List<DataStep>
                {
                    Del("reconciliation_matches", "المطابقات بين السطور"),
                    Del("reconciliation_adjustments", "تسويات المطابقة"),
                    Del("bank_reconciliations", "عمليات مطابقة الحساب"),
                    Del("bank_statement_lines", "سطور كشوف الحساب"),
                    Del("bank_statements", "كشوف الحساب البنكية"),
                    Del("bank_statement_import_logs", "سجلات استيراد كشوف الحساب"),
                    Del("bank_statement_import_templates", "قوالب استيراد كشوف الحساب")
                }
            });

            // ---------- تشغيلية: الديون المعدومة ----------
            list.Add(new DataGroup
            {
                Id = "bad_debt",
                Title = "الديون المعدومة",
                Description = "يحذف عمليات شطب الديون والمرفقات وعمليات العكس.",
                Category = "تشغيلية",
                Steps = new List<DataStep>
                {
                    Del("bad_debt_attachments", "مرفقات شطب الديون"),
                    Del("bad_debt_reversals", "عمليات عكس شطب الديون"),
                    Del("bad_debt_writeoffs", "عمليات شطب الديون")
                }
            });

            // ---------- تشغيلية: المتكرر ----------
            list.Add(new DataGroup
            {
                Id = "recurring",
                Title = "الفواتير والقيود المتكررة",
                Description = "يحذف قوالب الفواتير والقيود المتكررة وسجلات تنفيذها.",
                Category = "تشغيلية",
                Steps = new List<DataStep>
                {
                    Del("recurring_journal_lines", "سطور القوالب المتكررة"),
                    Del("recurring_journal_executions", "سجلات تنفيذ القيود المتكررة"),
                    Del("recurring_journal_templates", "قوالب القيود المتكررة"),
                    Del("recurring_invoice_lines", "سطور قوالب الفواتير المتكررة"),
                    Del("recurring_invoice_templates", "قوالب الفواتير المتكررة")
                }
            });

            // ---------- تشغيلية: النقل بين المخازن ----------
            list.Add(new DataGroup
            {
                Id = "warehouses",
                Title = "النقل بين المخازن",
                Description = "يحذف أوامر النقل بين المخازن وسطورها وسجل التتبع.",
                Category = "تشغيلية",
                Steps = new List<DataStep>
                {
                    Del("warehouse_transfer_lines", "سطور النقل بين المخازن"),
                    Del("warehouse_transfer_log", "سجل تتبع النقل"),
                    Del("warehouse_transfers", "أوامر النقل بين المخازن")
                }
            });

            // ---------- تشغيلية: مراكز التكلفة ----------
            list.Add(new DataGroup
            {
                Id = "cost_centers",
                Title = "مراكز التكلفة",
                Description = "يحذف مراكز التكلفة وتوزيعاتها وحركاتها.",
                Category = "تشغيلية",
                Steps = new List<DataStep>
                {
                    Del("cost_center_transactions", "حركات مراكز التكلفة"),
                    Del("cost_allocations", "توزيعات التكاليف"),
                    Del("cost_centers", "مراكز التكلفة")
                }
            });

            // ---------- تشغيلية: السياسات ----------
            list.Add(new DataGroup
            {
                Id = "policies",
                Title = "سياسات الخصم ورسوم التأخير",
                Description = "يحذف سياسات الخصم المسبق ورسوم التأخير واستثناءات العملاء.",
                Category = "تشغيلية",
                Steps = new List<DataStep>
                {
                    Del("party_late_fee_exceptions", "استثناءات رسوم التأخير"),
                    Del("party_discount_policies", "سياسات الخصم المخصصة للعملاء"),
                    Del("early_payment_discount_policies", "سياسات خصم السداد المبكر"),
                    Del("late_fee_policies", "سياسات رسوم التأخير")
                }
            });

            // ---------- تشغيلية: الفترات ----------
            list.Add(new DataGroup
            {
                Id = "periods",
                Title = "الفترات المحاسبية والموازنات",
                Description = "يحذف الفترات المحاسبية والسنوية والموازنات المخصصة.",
                Category = "تشغيلية",
                Steps = new List<DataStep>
                {
                    Del("budgets", "الموازنات"),
                    Del("accounting_periods", "الفترات المحاسبية")
                }
            });

            // ---------- أساسية: الأطراف ----------
            var partiesSteps = new List<DataStep>
            {
                Del("invoice_discount_applications", "تطبيقات الخصم"),
                Del("invoice_late_fees", "رسوم التأخير"),
                Del("invoice_payments", "دفعات الفواتير"),
                Del("invoice_lines", "سطور الفواتير"),
                Del("payments", "سندات القبض والصرف"),
                Del("subscription_periods", "فترات الاشتراك"),
                Del("subscriptions", "الاشتراكات"),
                Del("bad_debt_attachments", "مرفقات شطب الديون"),
                Del("bad_debt_reversals", "عمليات عكس شطب الديون"),
                Del("bad_debt_writeoffs", "عمليات شطب الديون"),
                Del("party_late_fee_exceptions", "استثناءات رسوم التأخير"),
                Del("party_discount_policies", "سياسات الخصم المخصصة"),
                Del("recurring_invoice_lines", "سطور قوالب الفواتير المتكررة"),
                Del("recurring_invoice_templates", "قوالب الفواتير المتكررة"),
                new()
                {
                    Table = "journal_entries",
                    Kind = DataStepKind.Detach,
                    SetAssignments = "reference_id=NULL",
                    Where = "reference_type='party'",
                    Label = "فكّ ارتباط القيود بالأطراف"
                },
                Del("parties", "الأطراف (العملاء والموردون)")
            };
            AddPaymentDetaches(partiesSteps, null, includeDiscountApplications: false);

            list.Add(new DataGroup
            {
                Id = "master_parties",
                Title = "الأطراف (العملاء والموردون)",
                Description = "يحذف كل الأطراف. تُحذف معها فواتيرها ومدفوعاتها واشتراكاتها وسجلات شطب ديونها وسياساتها المخصصة، وتُفكّ ارتباطاتها الاختيارية.",
                Category = "أساسية",
                Steps = partiesSteps
            });

            // ---------- أساسية: الأصناف ----------
            list.Add(new DataGroup
            {
                Id = "master_items",
                Title = "الأصناف",
                Description = "يحذف كل الأصناف. تُحذف معها حركات المخزون والجرد والتلف والنقل وتكلفة الأصناف ومكوناتها.",
                Category = "أساسية",
                Steps = new List<DataStep>
                {
                    Del("damaged_item_inspection_log", "سجل معاينة الأصناف التالفة"),
                    Del("custody_increase_requests", "طلبات زيادة الحيازة"),
                    Del("writeoff_requests", "طلبات الإتلاف"),
                    Del("damaged_items", "سجلات الأصناف التالفة"),
                    Del("stock_counts", "خطوط الجرد"),
                    Del("stock_count_sessions", "جلسات الجرد"),
                    Del("stock_adjustments", "تسويات المخزون"),
                    Del("cost_history", "تاريخ تكلفة الأصناف"),
                    Del("kit_component_history", "سجل تعديلات مكونات الأصناف المجمّعة"),
                    Del("kit_components", "مكونات الأصناف المجمّعة"),
                    Del("inventory_movements", "حركات المخزون"),
                    Del("invoice_lines", "سطور الفواتير"),
                    Del("warehouse_transfer_lines", "سطور النقل بين المخازن"),
                    Del("recurring_invoice_lines", "سطور قوالب الفواتير المتكررة"),
                    Del("stock_levels", "أرصدة الأصناف"),
                    Del("items", "الأصناف")
                }
            });

            // ---------- أساسية: الحسابات ----------
            list.Add(new DataGroup
            {
                Id = "master_accounts",
                Title = "الحسابات المحاسبية",
                Description = "يحذف الحسابات غير المرتبطة بأي سجل. الحسابات المرتبطة بأطراف أو أصناف أو خزائن أو قيود تبقى كما هي — احذف المجموعات المرتبطة أولاً لتحريرها.",
                Category = "أساسية",
                Steps = new List<DataStep>
                {
                    Del("accounts", "الحسابات غير المرتبطة بأي سجل", UnreferencedAccountsWhere)
                }
            });

            // ---------- أساسية: بيانات أخرى ----------
            list.Add(new DataGroup
            {
                Id = "master_misc",
                Title = "الخزائن والتصنيفات والعملات",
                Description = "يحذف الخزائن وتصنيفات الأصناف وتصنيفات المصروفات والعملات وأسباب تسوية المخزون.",
                Category = "أساسية",
                Steps = new List<DataStep>
                {
                    Del("expense_categories", "تصنيفات المصروفات"),
                    Del("categories", "تصنيفات الأصناف"),
                    Del("stock_reasons", "أسباب تسوية المخزون"),
                    Del("cash_boxes", "الخزائن"),
                    Del("currencies", "العملات")
                }
            });

            // ---------- أساسية: المستودعات ----------
            list.Add(new DataGroup
            {
                Id = "master_warehouses",
                Title = "المستودعات",
                Description = "يحذف المستودعات بعد حذف حركات النقل والجرد المرتبطة بها.",
                Category = "أساسية",
                Steps = new List<DataStep>
                {
                    Del("warehouse_transfer_lines", "سطور النقل بين المخازن"),
                    Del("warehouse_transfer_log", "سجل تتبع النقل"),
                    Del("warehouse_transfers", "أوامر النقل بين المخازن"),
                    Del("stock_counts", "خطوط الجرد"),
                    Del("stock_count_sessions", "جلسات الجرد"),
                    Del("damaged_items", "سجلات الأصناف التالفة"),
                    Del("stock_adjustments", "تسويات المخزون"),
                    Del("stock_levels", "أرصدة الأصناف"),
                    Del("warehouses", "المستودعات")
                }
            });

            // ---------- نظام: سجلات التدقيق ----------
            list.Add(new DataGroup
            {
                Id = "system_logs",
                Title = "سجلات التدقيق والدخول",
                Description = "يحذف سجلات التدقيق وسجل محاولات الدخول ومفاتيح تكرار الطلبات (لا يمس المستخدمين).",
                Category = "نظام",
                Steps = new List<DataStep>
                {
                    Del("app_audit_logs", "سجلات تدقيق التطبيق"),
                    Del("audit_log", "سجل التدقيق"),
                    Del("login_attempts", "محاولات تسجيل الدخول"),
                    Del("idempotency_keys", "مفاتيح تكرار الطلبات")
                }
            });

            // ---------- نظام: الإعدادات ----------
            list.Add(new DataGroup
            {
                Id = "system_settings",
                Title = "الإعدادات",
                Description = "يحذف إعدادات التطبيق وإعدادات التقارير (لا يمس المستخدمين ولا الأدوار).",
                Category = "نظام",
                Steps = new List<DataStep>
                {
                    Del("reporting_settings", "إعدادات التقارير"),
                    Del("app_settings", "إعدادات التطبيق")
                }
            });

            // ---------- نظام: المستخدمون ----------
            list.Add(new DataGroup
            {
                Id = "system_users",
                Title = "المستخدمون والأجهزة",
                Description = "يحذف صلاحيات المستخدمين وأجهزتهم المسجلة. يبقى المدير الرئيسي (المستخدم رقم 1) دائماً.",
                Category = "نظام",
                Steps = new List<DataStep>
                {
                    Del("user_devices", "الأجهزة المسجلة"),
                    Del("user_permissions", "صلاحيات المستخدمين"),
                    Del("users", "المستخدمون (عدا المدير الرئيسي)", "id <> 1")
                }
            });

            // ---------- نظام: الأدوار والصلاحيات ----------
            list.Add(new DataGroup
            {
                Id = "system_roles",
                Title = "الأدوار والصلاحيات",
                Description = "يحذف الأدوار غير النظامية (المُنشأة يدويًا) وصلاحياتها. الأدوار النظامية السبعة تبقى كما هي لحماية صلاحيات النظام.",
                Category = "نظام",
                Steps = new List<DataStep>
                {
                    Del("role_permissions", "صلاحيات الأدوار غير النظامية",
                        "role_id IN (SELECT id FROM roles WHERE COALESCE(is_system_role,false) = false)"),
                    Del("roles", "الأدوار غير النظامية", "COALESCE(is_system_role,false) = false")
                }
            });

            return list;
        }

        // ==================== 2) المعاينة ====================

        /// <summary>معاينة مجموعة قبل الحذف.</summary>
        public static DataPreviewResult Preview(string groupId)
        {
            var group = Groups.FirstOrDefault(g => g.Id == groupId)
                       ?? throw new InvalidOperationException("مجموعة غير معروفة: " + groupId);

            var result = new DataPreviewResult { Title = group.Title };
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            foreach (var step in group.Steps)
            {
                var count = CountStep(conn, step, null);
                result.Items.Add(new DataPreviewItem
                {
                    TableName = step.Table,
                    Label = step.Label,
                    RowCount = count,
                    Action = ActionText(step.Kind)
                });
            }
            result.Notes.Add("العملية تُنفَّذ داخل معاملة واحدة: عند فشل أي جزء يتم التراجع عن كل التغييرات.");
            return result;
        }

        /// <summary>معاينة نطاق جزئي (عميل واحد أو تاريخ). </summary>
        public static DataPreviewResult PreviewScope(DeleteScope scope)
        {
            var steps = ScopeSteps(scope);
            var result = new DataPreviewResult { Title = ScopeTitle(scope) };
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            foreach (var step in steps)
            {
                result.Items.Add(new DataPreviewItem
                {
                    TableName = step.Table,
                    Label = step.Label,
                    RowCount = CountStep(conn, step, scope),
                    Action = ActionText(step.Kind)
                });
            }
            result.Notes.Add("العملية تُنفَّذ داخل معاملة واحدة: عند فشل أي جزء يتم التراجع عن كل التغييرات.");
            return result;
        }

        /// <summary>معاينة إعادة ضبط المصنع.</summary>
        public static DataPreviewResult PreviewFactoryReset(FactoryResetOptions options)
        {
            var steps = FactoryResetSteps(options);
            var result = new DataPreviewResult { Title = "إعادة ضبط المصنع" };
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            foreach (var step in steps)
            {
                result.Items.Add(new DataPreviewItem
                {
                    TableName = step.Table,
                    Label = step.Label,
                    RowCount = CountStep(conn, step, null),
                    Action = ActionText(step.Kind)
                });
            }
            result.Notes.Add(options.KeepUsersAndRoles
                ? "سيتم الاحتفاظ بالمستخدمين والأدوار."
                : "سيتم حذف كل المستخدمين والأدوار — لن able إلى الدخول بعد ذلك.");
            if (options.KeepMasterData)
                result.Notes.Add("سيتم الاحتفاظ بالبيانات الأساسية (الأطراف والأصناف والحسابات).");
            if (options.KeepSettings)
                result.Notes.Add("سيتم الاحتفاظ بالإعدادات.");
            return result;
        }

        // ==================== 3) التنفيذ ====================

        /// <summary>تنفيذ حذف مجموعة كاملة.</summary>
        public static DataDeleteResult Execute(string groupId)
        {
            var group = Groups.FirstOrDefault(g => g.Id == groupId)
                       ?? throw new InvalidOperationException("مجموعة غير معروفة: " + groupId);

            var result = new DataDeleteResult { Success = true };
            RunInTransaction(tx =>
            {
                foreach (var step in group.Steps)
                    result.AffectedRows += ExecuteStep(tx, step, null, result.Details, result.StepResults);
                result.SweptRows += RunIntegritySweep(tx, result.Details);
            });
            result.Message = $"تم حذف {result.AffectedRows} سجلًا من «{group.Title}».";
            return result;
        }

        /// <summary>تنفيذ حذف نطاق جزئي.</summary>
        public static DataDeleteResult ExecuteScope(DeleteScope scope)
        {
            var steps = ScopeSteps(scope);
            var result = new DataDeleteResult { Success = true };
            RunInTransaction(tx =>
            {
                foreach (var step in steps)
                    result.AffectedRows += ExecuteStep(tx, step, scope, result.Details, result.StepResults);
                result.SweptRows += RunIntegritySweep(tx, result.Details);
            });
            result.Message = $"تم حذف {result.AffectedRows} سجلًا ({ScopeTitle(scope)}).";
            return result;
        }

        /// <summary>تنفيذ إعادة ضبط المصنع.</summary>
        public static DataDeleteResult ExecuteFactoryReset(FactoryResetOptions options)
        {
            var steps = FactoryResetSteps(options);
            var result = new DataDeleteResult { Success = true };
            RunInTransaction(tx =>
            {
                foreach (var step in steps)
                    result.AffectedRows += ExecuteStep(tx, step, null, result.Details, result.StepResults);

                // ضبط الـ sequences على أعلى id الحالي لكل جدول.
                // ملاحظة مهمة: لا نستخدم RESTART WITH 1 لأن ذلك يسبّب تكرارًا في المفاتيح
                // إذا كان الجدول ما زال يحتوي على صفوف (عند الاحتفاظ بالبيانات الأساسية).
                ResyncSequences(tx);
                result.SweptRows += RunIntegritySweep(tx, result.Details);
            });
            result.Message = $"تمت إعادة الضبط: {result.AffectedRows} سجلًا.";
            return result;
        }

        // ==================== 4) النطاقات الجزئية ====================

        /// <summary>خطوات حذف نطاق عميل واحد.</summary>
        public static List<DataStep> PartyScopeSteps(int partyId, bool includePartyRecord)
        {
            var s = new DeleteScope { Kind = "party", PartyId = partyId, IncludePartyRecord = includePartyRecord };
            return ScopeSteps(s);
        }

        private static List<DataStep> ScopeSteps(DeleteScope scope)
        {
            if (scope.Kind == "party")
            {
                var steps = new List<DataStep>
                {
                    Del("invoice_discount_applications", "تطبيقات الخصم", null, null, Ref("invoices", "invoice_id", "party_id")),
                    Del("invoice_late_fees", "رسوم التأخير", null, null, Ref("invoices", "invoice_id", "party_id")),
                    Del("invoice_payments", "دفعات الفواتير", null, null, Ref("invoices", "invoice_id", "party_id")),
                    Del("invoice_lines", "سطور الفواتير", null, null, Ref("invoices", "invoice_id", "party_id")),
                    Del("cost_history", "تاريخ تكلفة الفواتير", null, null, Ref("invoices", "invoice_id", "party_id")),
                    new()
                    {
                        Table = "bad_debt_writeoffs",
                        Kind = DataStepKind.Detach,
                        SetAssignments = "invoice_id=NULL",
                        ScopeParent = Ref("invoices", "invoice_id", "party_id"),
                        Label = "فكّ ارتباط شطب الديون بالفواتير"
                    },
                    Del("inventory_movements", "حركات المخزون الناتجة عن الفواتير", "reference_type='invoice'",
                        null, Ref("invoices", "reference_id", "party_id")),
                    Del("invoices", "الفواتير", null, "party_id"),
                    Del("payments", "سندات القبض والصرف", null, "party_id"),
                    Del("subscription_periods", "فترات الاشتراك", null, null, Ref("subscriptions", "subscription_id", "party_id")),
                    Del("subscriptions", "الاشتراكات", null, "party_id"),
                    Del("bad_debt_attachments", "مرفقات شطب الديون", null, null, Ref("bad_debt_writeoffs", "writeoff_id", "party_id")),
                    Del("bad_debt_reversals", "عمليات عكس شطب الديون", null, null, Ref("bad_debt_writeoffs", "writeoff_id", "party_id")),
                    Del("bad_debt_writeoffs", "عمليات شطب الديون", null, "party_id"),
                    Del("party_discount_policies", "سياسات الخصم المخصصة", null, "party_id"),
                    Del("party_late_fee_exceptions", "استثناءات رسوم التأخير", null, "party_id"),
                    Del("recurring_invoice_lines", "سطور قوالب الفواتير المتكررة", null, null,
                        Ref("recurring_invoice_templates", "template_id", "party_id")),
                    Del("recurring_invoice_templates", "قوالب الفواتير المتكررة", null, "party_id"),
                    new()
                    {
                        Table = "journal_entries",
                        Kind = DataStepKind.Detach,
                        SetAssignments = "reference_id=NULL",
                        Where = "reference_type='party'",
                        Label = "فكّ ارتباط القيود بالعميل"
                    }
                };
                if (scope.IncludePartyRecord == true)
                    steps.Add(Del("parties", "سجل العميل نفسه", null, "id"));
                return steps;
            }

            if (scope.Kind == "before_date")
            {
                var steps = new List<DataStep>
                {
                    Del("invoice_discount_applications", "تطبيقات الخصم", null, null, Ref("invoices", "invoice_id", "date")),
                    Del("invoice_late_fees", "رسوم التأخير", null, null, Ref("invoices", "invoice_id", "date")),
                    Del("invoice_payments", "دفعات الفواتير", null, null, Ref("invoices", "invoice_id", "date")),
                    Del("invoice_lines", "سطور الفواتير", null, null, Ref("invoices", "invoice_id", "date")),
                    Del("cost_history", "تاريخ تكلفة الفواتير", null, null, Ref("invoices", "invoice_id", "date")),
                    new()
                    {
                        Table = "bad_debt_writeoffs",
                        Kind = DataStepKind.Detach,
                        SetAssignments = "invoice_id=NULL",
                        ScopeParent = Ref("invoices", "invoice_id", "date"),
                        Label = "فكّ ارتباط شطب الديون بالفواتير"
                    },
                    Del("inventory_movements", "حركات المخزون الناتجة عن الفواتير", "reference_type='invoice'",
                        null, Ref("invoices", "reference_id", "date")),
                    Del("invoices", "الفواتير", null, "date"),
                    Del("payments", "سندات القبض والصرف", null, "date"),
                    Del("journal_lines", "خطوط القيود المحاسبية", null, null, Ref("journal_entries", "entry_id", "date")),
                    Del("cash_recognition_log", "سجل تحقق النقد", null, null, Ref("journal_entries", "journal_entry_id", "date")),
                    Del("journal_entries", "القيود المحاسبية", null, "date"),
                    Del("expenses", "المصروفات", null, "date"),
                    Del("cash_transfer_log", "سجل تحويلات الخزائن", null, null, Ref("cash_transfers", "transfer_id", "date")),
                    Del("cash_transfers", "التحويلات بين الخزائن", null, "date"),
                    Del("shift_closings", "إقفال الورديات", null, "shift_date"),
                    Del("stock_adjustments", "تسويات المخزون", null, "adjustment_date"),
                    Del("stock_counts", "خطوط الجرد", null, "date"),
                    Del("stock_count_sessions", "جلسات الجرد", null, "session_date"),
                    Del("inventory_movements", "حركات المخزون", null, "date"),
                    Del("subscription_periods", "فترات الاشتراك", null, null, Ref("subscriptions", "subscription_id", "start_date")),
                    Del("subscriptions", "الاشتراكات", null, "start_date"),
                    Del("loan_payments", "مدفوعات السلف", null, null, Ref("loans", "loan_id", "loan_date")),
                    Del("loan_installments", "أقساط السلف", null, null, Ref("loans", "loan_id", "loan_date")),
                    Del("loan_rescheduling_history", "سجل إعادة جدولة السلف", null, null, Ref("loans", "loan_id", "loan_date")),
                    Del("overdraft_transactions", "حركات السحب على الحساب", null, null, Ref("loans", "loan_id", "loan_date")),
                    Del("loans", "السلف", null, "loan_date"),
                    Del("depreciation_records", "سجلات الإهلاك", null, "date"),
                    Del("salary_records", "كشوف الرواتب", null, "payment_date"),
                    Del("bank_statement_lines", "سطور كشوف الحساب", null, null, Ref("bank_statements", "statement_id", "statement_date")),
                    Del("bank_statements", "كشوف الحساب البنكية", null, "statement_date"),
                    Del("reconciliation_matches", "المطابقات", null, null, Ref("bank_reconciliations", "reconciliation_id", "period_end")),
                    Del("reconciliation_adjustments", "تسويات المطابقة", null, null, Ref("bank_reconciliations", "reconciliation_id", "period_end")),
                    Del("bank_reconciliations", "عمليات مطابقة الحساب", null, "period_end"),
                    Del("petty_cash_expenses", "مصاريف الخزينة المصغرة", null, "expense_date"),
                    Del("petty_cash_settlements", "تسويات الخزينة المصغرة", null, "settlement_date"),
                    Del("petty_cash_custodies", "حيازات الخزينة المصغرة", null, "issued_date"),
                    Del("recurring_journal_executions", "سجلات تنفيذ القيود المتكررة", null, "execution_date"),
                    Del("warehouse_transfer_lines", "سطور النقل بين المخازن", null, null, Ref("warehouse_transfers", "transfer_id", "date")),
                    Del("warehouse_transfers", "أوامر النقل بين المخازن", null, "date"),
                    Del("cost_center_transactions", "حركات مراكز التكلفة", null, "date"),
                    Del("bad_debt_attachments", "مرفقات شطب الديون", null, null, Ref("bad_debt_writeoffs", "writeoff_id", "writeoff_date")),
                    Del("bad_debt_reversals", "عمليات عكس شطب الديون", null, null, Ref("bad_debt_writeoffs", "writeoff_id", "writeoff_date")),
                    Del("bad_debt_writeoffs", "عمليات شطب الديون", null, "writeoff_date")
                };
                AddMovementDetaches(steps);
                return steps;
            }

            throw new InvalidOperationException("نطاق غير معروف: " + scope.Kind);
        }

        private static string ScopeTitle(DeleteScope scope)
        {
            if (scope.Kind == "party")
                return "بيانات العميل رقم " + scope.PartyId;
            if (scope.Kind == "before_date")
                return "الحركات السابقة لتاريخ " +
                       (scope.Before?.ToString("yyyy-MM-dd") ?? "-");
            return "نطاق";
        }

        // ==================== 5) خطوات إعادة ضبط المصنع ====================

        /// <summary>
        /// ترتيب إعادة الضبط: الأبناء قبل الآباء.
        /// مبني على روابط الجداول الفعلية في القاعدة (parties → subscriptions → subscription_periods).
        /// </summary>
        private static List<DataStep> FactoryResetSteps(FactoryResetOptions options)
        {
            var steps = new List<DataStep>();
            void Add(string table, string label, string? where = null, DataStepKind kind = DataStepKind.Delete,
                string? set = null)
            {
                steps.Add(new DataStep
                {
                    Table = table,
                    Label = label,
                    Where = where,
                    Kind = kind,
                    SetAssignments = set
                });
            }

            // --- تشغيلية ---
            Add("invoice_discount_applications", "تطبيقات الخصم");
            Add("invoice_late_fees", "رسوم التأخير");
            Add("invoice_payments", "دفعات الفواتير");
            Add("invoice_lines", "سطور الفواتير");
            Add("kit_component_history", "سجل تعديلات مكونات الأصناف المجمّعة");
            Add("kit_components", "مكونات الأصناف المجمّعة");
            Add("damaged_item_inspection_log", "سجل معاينة الأصناف التالفة");
            Add("custody_increase_requests", "طلبات زيادة الحيازة");
            Add("writeoff_requests", "طلبات الإتلاف");
            Add("damaged_items", "سجلات الأصناف التالفة");
            Add("stock_counts", "خطوط الجرد");
            Add("stock_count_sessions", "جلسات الجرد");
            Add("stock_adjustments", "تسويات المخزون");
            Add("cost_center_transactions", "حركات مراكز التكلفة");
            Add("cost_allocations", "توزيعات التكاليف");
            Add("cost_history", "تاريخ تكلفة الأصناف");
            Add("reconciliation_matches", "المطابقات");
            Add("reconciliation_adjustments", "تسويات المطابقة");
            Add("bank_reconciliations", "عمليات مطابقة الحساب");
            Add("bank_statement_lines", "سطور كشوف الحساب");
            Add("bank_statements", "كشوف الحساب البنكية");
            Add("bank_statement_import_logs", "سجلات استيراد كشوف الحساب");
            Add("bank_statement_import_templates", "قوالب استيراد كشوف الحساب");
            Add("bad_debt_attachments", "مرفقات شطب الديون");
            Add("bad_debt_reversals", "عمليات عكس شطب الديون");
            Add("bad_debt_writeoffs", "عمليات شطب الديون");
            Add("depreciation_records", "سجلات الإهلاك");
            Add("fixed_assets", "الأصول الثابتة");
            Add("salary_records", "كشوف الرواتب");
            Add("petty_cash_expenses", "مصاريف الخزينة المصغرة");
            Add("petty_cash_settlements", "تسويات الخزينة المصغرة");
            Add("petty_cash_custodies", "حيازات الخزينة المصغرة");
            Add("loan_rescheduling_history", "سجل إعادة جدولة السلف");
            Add("loan_payments", "مدفوعات السلف");
            Add("loan_installments", "أقساط السلف");
            Add("overdraft_transactions", "حركات السحب على الحساب");
            Add("loans", "السلف");
            Add("recurring_journal_lines", "سطور القوالب المتكررة");
            Add("recurring_journal_executions", "سجلات تنفيذ القيود المتكررة");
            Add("recurring_journal_templates", "قوالب القيود المتكررة");
            Add("recurring_invoice_lines", "سطور قوالب الفواتير المتكررة");
            Add("recurring_invoice_templates", "قوالب الفواتير المتكررة");
            Add("warehouse_transfer_lines", "سطور النقل بين المخازن");
            Add("warehouse_transfer_log", "سجل تتبع النقل");
            Add("warehouse_transfers", "أوامر النقل بين المخازن");
            Add("subscription_periods", "فترات الاشتراك");
            Add("subscriptions", "الاشتراكات");
            Add("shift_closings", "إقفال الورديات");
            Add("cash_recognition_log", "سجل تحقق النقد");
            Add("cash_transfer_log", "سجل تحويلات الخزائن");
            Add("cash_transfers", "التحويلات بين الخزائن");
            Add("journal_lines", "خطوط القيود المحاسبية");
            Add("journal_entries", "القيود المحاسبية");
            Add("expenses", "المصروفات");
            Add("payments", "سندات القبض والصرف");
            Add("inventory_movements", "حركات المخزون");
            Add("invoices", "الفواتير");
            Add("budgets", "الموازنات");
            Add("accounting_periods", "الفترات المحاسبية");
            Add("party_late_fee_exceptions", "استثناءات رسوم التأخير");
            Add("party_discount_policies", "سياسات الخصم المخصصة");
            Add("late_fee_policies", "سياسات رسوم التأخير");
            Add("early_payment_discount_policies", "سياسات خصم السداد المبكر");

            // --- أساسية (اختيارية) ---
            if (!options.KeepMasterData)
            {
                Add("warehouses", "المستودعات");
                Add("cost_centers", "مراكز التكلفة");
                Add("employees", "الموظفون");
                Add("expense_categories", "تصنيفات المصروفات");
                Add("categories", "تصنيفات الأصناف");
                Add("stock_reasons", "أسباب تسوية المخزون");
                Add("cash_boxes", "الخزائن");
                Add("currencies", "العملات");
                // الأرصدة تُحذف بالكامل هنا (وليست تُصفَّر) — لأن الأصناف ستُحذف،
                // وإلا بقيت أرصدة تشير إلى أصناف محذوفة.
                Add("stock_levels", "أرصدة الأصناف");
                Add("items", "الأصناف");
                Add("parties", "الأطراف (العملاء والموردون)");
                Add("accounts", "الحسابات المحاسبية");
            }
            else
            {
                // الاحتفاظ بالبيانات الأساسية → تصفير الأرصدة فقط (لا تُحذف)
                Add("stock_levels", "تصفير أرصدة الأصناف", null, DataStepKind.Reset,
                    "quantity=0, reserved_quantity=0, damaged_quantity=0, under_inspection_quantity=0");
                Add("items", "تصفير أرصدة الافتتاح للأصناف", null, DataStepKind.Reset, "opening_quantity=0");
            }

            // --- نظام ---
            Add("app_audit_logs", "سجلات تدقيق التطبيق");
            Add("audit_log", "سجل التدقيق");
            Add("login_attempts", "محاولات تسجيل الدخول");
            Add("idempotency_keys", "مفاتيح تكرار الطلبات");

            if (!options.KeepSettings)
            {
                Add("reporting_settings", "إعدادات التقارير");
                Add("app_settings", "إعدادات التطبيق");
            }

            if (!options.KeepUsersAndRoles)
            {
                Add("user_devices", "الأجهزة المسجلة");
                Add("user_permissions", "صلاحيات المستخدمين");
                Add("users", "المستخدمون");
                Add("role_permissions", "صلاحيات الأدوار");
                Add("roles", "الأدوار");
            }

            return steps;
        }

        // ==================== 6) محرّك SQL ====================

        /// <summary>
        /// مراجع journal_entries متعددة الأشكال (reference_type + reference_id)
        /// ← الجدول الأصل. تُستخدم في «كنس السلامة» لحذف القيود التي فقدت مصدرها.
        /// </summary>
        private static readonly (string ReferenceType, string Table)[] JournalReferences =
        {
            ("invoice", "invoices"),
            ("payment", "payments"),
            ("expense", "expenses"),
            ("cash_transfer", "cash_transfers"),
            ("bank_reconciliation", "bank_reconciliations"),
            ("bad_debt_writeoff", "bad_debt_writeoffs"),
            ("bad_debt_reversal", "bad_debt_reversals"),
            ("custody", "petty_cash_custodies"),
            ("petty_cash_expense", "petty_cash_expenses"),
            ("custody_settlement", "petty_cash_settlements"),
            ("recurring_journal", "recurring_journal_templates"),
            ("recurring_invoice", "recurring_invoice_templates"),
            ("loan", "loans"),
            ("subscription", "subscriptions"),
            ("fixed_asset", "fixed_assets"),
            ("salary", "salary_records"),
            ("item", "items"),
            ("party", "parties")
        };

        /// <summary>أعمدة journal_entry_id في باقي الجداول — تُفكّ ارتباطاتها عند فقدان القيد.</summary>
        private static readonly (string Table, string Column)[] JournalEntryIdColumns =
        {
            ("bad_debt_writeoffs", "journal_entry_id"),
            ("bad_debt_reversals", "journal_entry_id"),
            ("invoice_late_fees", "journal_entry_id"),
            ("invoice_discount_applications", "journal_entry_id"),
            ("custody_increase_requests", "journal_entry_id"),
            ("petty_cash_custodies", "journal_entry_id"),
            ("petty_cash_expenses", "journal_entry_id"),
            ("petty_cash_settlements", "journal_entry_id"),
            ("recurring_journal_executions", "journal_entry_id"),
            ("reconciliation_adjustments", "journal_entry_id")
        };

        /// <summary>
        /// أعمدة journal_entry_id الإلزامية (NOT NULL) — لا يمكن فكّ ارتباطها،
        /// لذلك يُحذف السجل كله إذا فقد قيده المحاسبي.
        /// </summary>
        private static readonly (string Table, string Column)[] RequiredJournalEntryColumns =
        {
            ("cash_recognition_log", "journal_entry_id")
        };

        /// <summary>مراجع حركات المخزون متعددة الأشكال.</summary>
        private static readonly (string ReferenceType, string Table)[] MovementReferences =
        {
            ("invoice", "invoices"),
            ("return", "invoices"),
            ("adjustment", "stock_adjustments"),
            ("transfer", "warehouse_transfers")
        };

        /// <summary>مراجع حركات مراكز التكلفة.</summary>
        private static readonly (string ReferenceType, string Table)[] CostCenterReferences =
        {
            ("invoice", "invoices"),
            ("expense", "expenses"),
            ("payment", "payments"),
            ("journal_entry", "journal_entries")
        };

        /// <summary>
        /// يضبط كل sequence على أعلى id في جدوله (أو 1 إذا كان الجدول فارغًا).
        /// <para><b>لماذا لا RESTART WITH 1؟</b> لأن ذلك يسبّب خطأ «قيمة مكررة» عند
        /// الإدراج التالي إذا بقيت صفوف في الجدول (مثلًا عند الاحتفاظ بالبيانات الأساسية).</para>
        /// </summary>
        private static void ResyncSequences(NpgsqlTransaction tx)
        {
            try
            {
                using var cmd = new NpgsqlCommand(@"
                    SELECT c.relname AS seq_name, split_part(c.relname, '_id_seq', 1) AS table_name
                    FROM pg_class c
                    JOIN pg_namespace n ON n.oid = c.relnamespace
                    WHERE c.relkind = 'S' AND n.nspname = 'public' AND c.relname LIKE '%_id_seq'", tx.Connection, tx);
                var pairs = new List<(string Seq, string Table)>();
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        pairs.Add((r.GetString(0), r.GetString(1)));

                foreach (var (seq, table) in pairs)
                {
                    try
                    {
                        ExecuteNonQuery(tx,
                            $"SELECT setval('public.\"{seq}\"', " +
                            $"GREATEST(COALESCE((SELECT MAX(id) FROM \"{table}\"), 0), 1), " +
                            $"(SELECT COUNT(*) > 0 FROM \"{table}\"))");
                    }
                    catch { /* جدول بلا id أو تسلسل غير موجود */ }
                }
            }
            catch { }
        }

        /// <summary>
        /// «كنس السلامة» — يُنفَّذ في نهاية كل عملية حذف.
        /// <para>يضمن أن لا يبقى أي سجل يشير إلى سجل محذوف:</para>
        /// <list type="bullet">
        /// <item>يحذف القيود المحاسبية التي فقدت مصدرها (فاتورة/دفعة/مصروف...).</item>
        /// <item>يفكّ ارتباط journal_entry_id في باقي الجداول.</item>
        /// <item>يحذف حركات المخزون وحركات مراكز التكلفة التي فقدت مصدرها.</item>
        /// </list>
        /// </summary>
        private static long RunIntegritySweep(NpgsqlTransaction tx, List<string> details)
        {
            long affected = 0;

            foreach (var (refType, table) in JournalReferences)
            {
                affected += ExecuteNonQuery(tx,
                    "DELETE FROM journal_lines WHERE entry_id IN (SELECT id FROM journal_entries " +
                    $"WHERE reference_type='{refType}' AND reference_id IS NOT NULL " +
                    $"AND reference_id NOT IN (SELECT id FROM \"{table}\"))");
                affected += ExecuteNonQuery(tx,
                    "DELETE FROM journal_entries " +
                    $"WHERE reference_type='{refType}' AND reference_id IS NOT NULL " +
                    $"AND reference_id NOT IN (SELECT id FROM \"{table}\")");
            }

            // مصادر مباشرة داخل journal_entries
            affected += ExecuteNonQuery(tx,
                "DELETE FROM journal_lines WHERE entry_id IN (SELECT id FROM journal_entries " +
                "WHERE source_invoice_id IS NOT NULL AND source_invoice_id NOT IN (SELECT id FROM invoices))");
            affected += ExecuteNonQuery(tx,
                "DELETE FROM journal_entries WHERE source_invoice_id IS NOT NULL " +
                "AND source_invoice_id NOT IN (SELECT id FROM invoices)");
            affected += ExecuteNonQuery(tx,
                "DELETE FROM journal_lines WHERE entry_id IN (SELECT id FROM journal_entries " +
                "WHERE source_payment_id IS NOT NULL AND source_payment_id NOT IN (SELECT id FROM payments))");
            affected += ExecuteNonQuery(tx,
                "DELETE FROM journal_entries WHERE source_payment_id IS NOT NULL " +
                "AND source_payment_id NOT IN (SELECT id FROM payments)");
            affected += ExecuteNonQuery(tx,
                "DELETE FROM journal_lines WHERE entry_id IN (SELECT id FROM journal_entries " +
                "WHERE period_id IS NOT NULL AND period_id NOT IN (SELECT id FROM accounting_periods))");
            affected += ExecuteNonQuery(tx,
                "DELETE FROM journal_entries WHERE period_id IS NOT NULL " +
                "AND period_id NOT IN (SELECT id FROM accounting_periods)");

            foreach (var (table, column) in JournalEntryIdColumns)
            {
                affected += ExecuteNonQuery(tx,
                    $"UPDATE \"{table}\" SET \"{column}\"=NULL " +
                    $"WHERE \"{column}\" IS NOT NULL AND \"{column}\" NOT IN (SELECT id FROM journal_entries)");
            }

            // أعمدة إلزامية: السجل يُحذف بالكامل إذا فقد قيده
            foreach (var (table, column) in RequiredJournalEntryColumns)
            {
                affected += ExecuteNonQuery(tx,
                    $"DELETE FROM \"{table}\" " +
                    $"WHERE \"{column}\" IS NOT NULL AND \"{column}\" NOT IN (SELECT id FROM journal_entries)");
            }

            foreach (var (refType, table) in MovementReferences)
            {
                affected += ExecuteNonQuery(tx,
                    "DELETE FROM inventory_movements " +
                    $"WHERE reference_type='{refType}' AND reference_id IS NOT NULL " +
                    $"AND reference_id NOT IN (SELECT id FROM \"{table}\")");
            }

            foreach (var (refType, table) in CostCenterReferences)
            {
                affected += ExecuteNonQuery(tx,
                    "DELETE FROM cost_center_transactions " +
                    $"WHERE reference_type='{refType}' AND reference_id IS NOT NULL " +
                    $"AND reference_id NOT IN (SELECT id FROM \"{table}\")");
            }

            if (affected > 0)
                details.Add($"كنس السلامة: {affected} سجل يتيم أُزيل");
            return affected;
        }

        private static string ActionText(DataStepKind kind) => kind switch
        {
            DataStepKind.Reset => "تصفير",
            DataStepKind.Detach => "فكّ ارتباط",
            _ => "حذف"
        };

        private static long CountStep(NpgsqlConnection conn, DataStep step, DeleteScope? scope)
        {
            try
            {
                var where = BuildWhere(step, scope, out var ps);
                var sql = $"SELECT COUNT(*) FROM \"{step.Table}\"" + (string.IsNullOrWhiteSpace(where) ? "" : " WHERE " + where);
                using var cmd = new NpgsqlCommand(sql, conn);
                foreach (var p in ps) cmd.Parameters.AddWithValue(p.ParameterName, p.Value);
                return Convert.ToInt64(cmd.ExecuteScalar());
            }
            catch { return 0; }
        }

        private static long ExecuteStep(NpgsqlTransaction tx, DataStep step, DeleteScope? scope,
            List<string> details, List<DataStepResult>? stepResults = null)
        {
            var where = BuildWhere(step, scope, out var ps);
            long affected;
            switch (step.Kind)
            {
                case DataStepKind.Reset:
                {
                    var sql = $"UPDATE \"{step.Table}\" SET {step.SetAssignments}";
                    if (!string.IsNullOrWhiteSpace(where)) sql += " WHERE " + where;
                    affected = ExecuteNonQuery(tx, sql, ps);
                    break;
                }
                case DataStepKind.Detach:
                {
                    var sql = $"UPDATE \"{step.Table}\" SET {step.SetAssignments}";
                    if (!string.IsNullOrWhiteSpace(where)) sql += " WHERE " + where;
                    affected = ExecuteNonQuery(tx, sql, ps);
                    break;
                }
                default:
                {
                    var sql = $"DELETE FROM \"{step.Table}\"";
                    if (!string.IsNullOrWhiteSpace(where)) sql += " WHERE " + where;
                    affected = ExecuteNonQuery(tx, sql, ps);
                    break;
                }
            }

            if (affected > 0)
                details.Add($"{step.Label}: {affected} سجل");
            stepResults?.Add(new DataStepResult
            {
                Table = step.Table,
                Label = step.Label,
                Rows = affected,
                Action = ActionText(step.Kind)
            });
            return affected;
        }

        /// <summary>يبني شرط WHERE مع دمج النطاق الجزئي إن وُجد.</summary>
        private static string BuildWhere(DataStep step, DeleteScope? scope, out List<NpgsqlParameter> ps)
        {
            ps = new List<NpgsqlParameter>();
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(step.Where))
                parts.Add(step.Where);

            if (scope != null)
            {
                var scopeCond = BuildScopeCondition(step, scope, ps);
                if (!string.IsNullOrWhiteSpace(scopeCond))
                    parts.Add(scopeCond);
            }

            return string.Join(" AND ", parts);
        }

        private static string? BuildScopeCondition(DataStep step, DeleteScope scope, List<NpgsqlParameter> ps)
        {
            if (scope.Kind == "party" && scope.PartyId.HasValue)
            {
                ps.Add(new NpgsqlParameter("@scopeParty", scope.PartyId.Value));
                if (!string.IsNullOrWhiteSpace(step.ScopeSelfColumn))
                    return $"\"{step.ScopeSelfColumn}\" = @scopeParty";
                if (step.ScopeParent != null)
                    return $"\"{step.ScopeParent.ChildKey}\" IN (SELECT \"{step.ScopeParent.ParentKey}\" FROM \"{step.ScopeParent.ParentTable}\" WHERE \"{step.ScopeParent.ParentScopeColumn}\" = @scopeParty)";
                return null;
            }

            if (scope.Kind == "before_date" && scope.Before.HasValue)
            {
                ps.Add(new NpgsqlParameter("@scopeDate", scope.Before.Value));
                if (!string.IsNullOrWhiteSpace(step.ScopeSelfColumn))
                    return $"\"{step.ScopeSelfColumn}\" < @scopeDate";
                if (step.ScopeParent != null)
                    return $"\"{step.ScopeParent.ChildKey}\" IN (SELECT \"{step.ScopeParent.ParentKey}\" FROM \"{step.ScopeParent.ParentTable}\" WHERE \"{step.ScopeParent.ParentScopeColumn}\" < @scopeDate)";
                return null;
            }

            return null;
        }

        private static long ExecuteNonQuery(NpgsqlTransaction tx, string sql, List<NpgsqlParameter>? ps = null)
        {
            using var cmd = new NpgsqlCommand(sql, tx.Connection, tx);
            if (ps != null)
                foreach (var p in ps) cmd.Parameters.AddWithValue(p.ParameterName, p.Value);
            try
            {
                return cmd.ExecuteNonQuery();
            }
            catch (Npgsql.PostgresException ex)
            {
                // نرفق نص الجملة ليسهل تشخيص الخطأ فورًا
                throw new InvalidOperationException(
                    "فشل تنفيذ أمر على قاعدة البيانات: " + ex.MessageText +
                    " | الجملة: " + sql, ex);
            }
        }

        /// <summary>ينفّذ كل الخطوات داخل معاملة واحدة — فشل أي خطوة يتراجع عن كل شيء.</summary>
        private static void RunInTransaction(Action<NpgsqlTransaction> body)
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                body(tx);
                tx.Commit();
            }
            catch
            {
                try { tx.Rollback(); } catch { }
                throw;
            }
        }
    }
}
