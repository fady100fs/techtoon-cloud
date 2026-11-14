using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Npgsql;
using Techtoon.UI.Services;
using Xunit;

namespace Techtoon.Tests;

/// <summary>
/// إدارة البيانات — اختبارات حقيقية على قاعدة معزولة منفصلة.
/// <para><b>الالمبدأ:</b> كل جدول في القاعدة يجب أن يكون قابلاً للحذف، وكل عملية يجب أن
/// تكون ذرّية (لا تترك بيانات نصف محذوفة) ولا تترك سجلات يتيمة.</para>
/// </summary>
[Collection("DatabaseIntegration")]
public class DataManagementTests : IDisposable
{
    /// <summary>قاعدة خردة خاصة بهذه الاختبارات — لا تمس قاعدة الاختبار المشتركة.</summary>
    private const string ScratchDatabaseName = "techtoon_test_dm";

    private static readonly Lazy<string> Scratch = new(() => CreateScratchDatabase());
    private readonly string _originalCs;

    public DataManagementTests()
    {
        // نوجّه الخدمة إلى قاعدة الاختبار الخردة أثناء الاختبار
        _originalCs = Environment.GetEnvironmentVariable("TECHTOON_PG_CONNECTION") ?? "";
        Environment.SetEnvironmentVariable("TECHTOON_PG_CONNECTION", Scratch.Value);
    }

    /// <summary>نعيد المتغيّر كما كان حتى لا تتأثر بقية الاختبارات.</summary>
    public void Dispose()
    {
        Environment.SetEnvironmentVariable("TECHTOON_PG_CONNECTION", _originalCs);
    }

    // ==================== أدوات ====================

    private static string CreateScratchDatabase()
    {
        var current = Environment.GetEnvironmentVariable("TECHTOON_PG_CONNECTION") ?? "";
        var adminCs = Regex.Replace(current, @"(?i)(Database=)[^;]*", "$1postgres");
        var scratchCs = Regex.Replace(current, @"(?i)(Database=)[^;]*", "$1" + ScratchDatabaseName);

        var dumpDir = Path.Combine(Path.GetTempPath(), "techtoon_dm_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dumpDir);
        try
        {
            var dumpFile = BackupService.CreateBackup(dumpDir);
            using (var admin = new NpgsqlConnection(adminCs))
            {
                admin.Open();
                using (var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{ScratchDatabaseName}\" WITH (FORCE)", admin))
                    drop.ExecuteNonQuery();
                using (var create = new NpgsqlCommand($"CREATE DATABASE \"{ScratchDatabaseName}\"", admin))
                    create.ExecuteNonQuery();
            }
            if (!BackupService.RestoreBackup(dumpFile, ScratchDatabaseName))
                throw new InvalidOperationException("تعذّر استعادة النسخة في قاعدة الاختبار الخردة.");
            SyncSequences(scratchCs);
            return scratchCs;
        }
        finally
        {
            try { if (Directory.Exists(dumpDir)) Directory.Delete(dumpDir, true); } catch { }
        }
    }

    /// <summary>
    /// يزامن الـ sequences مع أعلى id في كل جدول.
    /// ضروري بعد استعادة نسخة احتياطية (الصفوف تُدرج بأرقام صريحة فتتأخر الـ sequences).
    /// </summary>
    private static void SyncSequences(string connectionString)
    {
        try
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            var seqs = new List<(string Seq, string Table)>();
            using (var cmd = new NpgsqlCommand(
                "SELECT c.relname, split_part(c.relname, '_id_seq', 1) FROM pg_class c " +
                "JOIN pg_namespace n ON n.oid = c.relnamespace WHERE c.relkind='S' " +
                "AND n.nspname='public' AND c.relname LIKE '%_id_seq'", conn))
            using (var r = cmd.ExecuteReader())
                while (r.Read()) seqs.Add((r.GetString(0), r.GetString(1)));

            foreach (var (seq, table) in seqs)
            {
                try
                {
                    using var maxCmd = new NpgsqlCommand(
                        $"SELECT COALESCE(MAX(id),0) FROM \"{table}\"", conn);
                    var max = Convert.ToInt64(maxCmd.ExecuteScalar());
                    if (max <= 0) continue;
                    using var setCmd = new NpgsqlCommand(
                        $"SELECT setval('public.\"{seq}\"', @v, true)", conn);
                    setCmd.Parameters.AddWithValue("@v", max);
                    setCmd.ExecuteScalar();
                }
                catch { /* جدول بلا id */ }
            }
        }
        catch { }
    }

    private static NpgsqlConnection Open()
    {
        var conn = new NpgsqlConnection(Environment.GetEnvironmentVariable("TECHTOON_PG_CONNECTION"));
        conn.Open();
        return conn;
    }

    private static long Count(string table, string? where = null)
    {
        using var conn = Open();
        using var cmd = new NpgsqlCommand(
            $"SELECT COUNT(*) FROM \"{table}\"" + (string.IsNullOrWhiteSpace(where) ? "" : " WHERE " + where), conn);
        return Convert.ToInt64(cmd.ExecuteScalar());
    }

    private static List<string> BaseTables()
    {
        using var conn = Open();
        using var cmd = new NpgsqlCommand(
            "SELECT table_name FROM information_schema.tables " +
            "WHERE table_schema='public' AND table_type='BASE TABLE' ORDER BY table_name", conn);
        var list = new List<string>();
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(r.GetString(0));
        return list;
    }

    // ==================== 1) التغطية ====================

    /// <summary>
    /// لا يوجد جدول خارج نطاق إدارة البيانات — هذا هو المبدأ الأساسي.
    /// </summary>
    [Fact]
    public void Every_Base_Table_Is_Covered_By_The_Data_Management_Catalog()
    {
        var tables = BaseTables();
        var covered = DataManagementService.CoveredTables.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missing = tables.Where(t => !covered.Contains(t)).ToList();
        Assert.True(missing.Count == 0,
            "جداول خارج نطاق إدارة البيانات (" + missing.Count + "): " + string.Join(", ", missing));

        // والعكس: لا يشير الكتالوج إلى جدول غير موجود
        var unknown = covered.Where(t => !tables.Contains(t, StringComparer.OrdinalIgnoreCase)).ToList();
        Assert.True(unknown.Count == 0,
            "جداول مذكورة في الكتالوج وغير موجودة في القاعدة: " + string.Join(", ", unknown));
    }

    /// <summary>لكل مجموعة معرّف فريد وخطوات مرتبة.</summary>
    [Fact]
    public void Every_Group_Has_A_Unique_Id_And_At_Least_One_Step()
    {
        var groups = DataManagementService.Groups;
        Assert.NotEmpty(groups);
        Assert.Equal(groups.Count, groups.Select(g => g.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        foreach (var g in groups)
        {
            Assert.NotEmpty(g.Steps);
            Assert.False(string.IsNullOrWhiteSpace(g.Title));
            Assert.False(string.IsNullOrWhiteSpace(g.Description));
        }
    }

    // ==================== 2) الحذف لكل مجموعة ====================

    /// <summary>
    /// ينشئ صفًا في كل جدول ثم يشغّل كل مجموعة حذف ويتأكد أن:
    /// 1) العملية نجحت (لم ترمِ استثناءً).
    /// 2) عدد السجلات المتأثرة يطابق المعاينة (المعاينة صادقة).
    /// 3) لم تظهر سجلات يتيمة جديدة.
    /// </summary>
    [Fact]
    public void Every_Group_Deletes_Its_Own_Tables_Without_Creating_Orphans()
    {
        var seeded = SeedAllTables();
        Assert.True(seeded.Count >= 80,
            "عدد الجداول التي أمكن زرعها قليل: " + seeded.Count + " من " + BaseTables().Count);

        foreach (var group in DataManagementService.Groups)
        {
            var orphansBefore = OrphanCounts();
            var preview = DataManagementService.Preview(group.Id);

            DataDeleteResult result;
            try
            {
                result = DataManagementService.Execute(group.Id);
            }
            catch (Exception ex)
            {
                Assert.Fail("المجموعة «" + group.Title + "» (" + group.Id + ") فشلت: " + ex.Message);
                return;
            }

            Assert.True(result.Success, group.Id);

            // 2) المعاينة صادقة — كل خطوة تُطابق ما عُرض على المستخدم
            foreach (var item in preview.Items)
            {
                var stepResult = result.StepResults.FirstOrDefault(s => s.Table == item.TableName && s.Label == item.Label);
                Assert.True(stepResult != null,
                    "المجموعة «" + group.Title + "»: لم تُنفَّذ خطوة " + item.Label);
                Assert.True(item.RowCount == stepResult!.Rows,
                    "المجموعة «" + group.Title + "» — خطوة «" + item.Label + "» (" + item.TableName +
                    "): المعاينة كشفت " + item.RowCount + " صف والتنفيذ/delete أثر " + stepResult.Rows);
            }

            // 3) لا سجلات يتيمة جديدة
            var orphansAfter = OrphanCounts();
            foreach (var key in orphansBefore.Keys)
            {
                Assert.True(orphansAfter[key] <= orphansBefore[key],
                    "المجموعة «" + group.Title + "» تركت سجلات يتيمة في " + key +
                    " (" + orphansBefore[key] + " -> " + orphansAfter[key] + ")");
            }
        }

        // بعد كل المجموعات: لا يبقى أي سجل يتيم
        foreach (var kv in OrphanCounts())
            Assert.True(kv.Value == 0, "سجلات يتيمة بعد كل المجموعات: " + kv.Key + " = " + kv.Value);
    }

    // ==================== 3) الذرّية ====================

    /// <summary>
    /// إذا فشل أي جزء من العملية تُلغى العملية كلها (لا حذف نصّي).
    /// </summary>
    [Fact]
    public void A_Failing_Step_Rolls_Back_The_Entire_Delete()
    {
        SeedAllTables();
        var invoicesBefore = Count("invoices");
        var linesBefore = Count("invoice_lines");
        Assert.True(invoicesBefore > 0, "لا توجد فواتير لاختبار الذرّية");

        // ن-plant trigger يُفشل حذف سطور الفواتير
        using (var conn = Open())
        {
            Execute(conn, @"CREATE OR REPLACE FUNCTION dm_test_fail() RETURNS trigger AS $$
                BEGIN RAISE EXCEPTION 'فشل مقصود من اختبار الذرّية'; END; $$ LANGUAGE plpgsql;");
            Execute(conn, @"DROP TRIGGER IF EXISTS dm_test_fail_trg ON invoice_lines;");
            Execute(conn, @"CREATE TRIGGER dm_test_fail_trg BEFORE DELETE ON invoice_lines
                            FOR EACH ROW EXECUTE FUNCTION dm_test_fail();");
        }

        try
        {
            var threw = false;
            try { DataManagementService.Execute("invoices"); }
            catch { threw = true; }
            Assert.True(threw, "كان يجب أن تفشل العملية بسبب القيد المُضاف");

            // لا شيء حُذف — المعاملة أُلغيت بالكامل
            Assert.Equal(invoicesBefore, Count("invoices"));
            Assert.Equal(linesBefore, Count("invoice_lines"));
        }
        finally
        {
            using var conn = Open();
            Execute(conn, "DROP TRIGGER IF EXISTS dm_test_fail_trg ON invoice_lines;");
            Execute(conn, "DROP FUNCTION IF EXISTS dm_test_fail();");
        }
    }

    // ==================== 4) إعادة ضبط المصنع ====================

    /// <summary>إعادة الضبط تمسح كل البيانات التشغيلية وتُبقي المستخدمين.</summary>
    [Fact]
    public void Factory_Reset_Clears_Every_Operational_Table_And_Keeps_Users()
    {
        SeedAllTables();
        var usersBefore = Count("users");
        Assert.True(usersBefore > 0);

        var options = new FactoryResetOptions
        {
            KeepUsersAndRoles = true,
            KeepMasterData = false,
            KeepSettings = true
        };

        var preview = DataManagementService.PreviewFactoryReset(options);
        Assert.True(preview.TotalRows > 0, "المعاينة فارغة — لا يوجد ما يُحذف");

        var result = DataManagementService.ExecuteFactoryReset(options);
        Assert.True(result.Success);

        // كل الجداول التشغيلية فارغة
        var operational = new[]
        {
            "invoices", "invoice_lines", "invoice_payments", "invoice_discount_applications",
            "invoice_late_fees", "payments", "expenses", "journal_entries", "journal_lines",
            "cash_transfers", "cash_transfer_log", "cash_recognition_log", "shift_closings",
            "inventory_movements", "stock_counts", "stock_count_sessions", "stock_adjustments",
            "damaged_items", "damaged_item_inspection_log", "writeoff_requests",
            "custody_increase_requests", "subscriptions", "subscription_periods",
            "loans", "loan_installments", "loan_payments", "loan_rescheduling_history",
            "overdraft_transactions", "petty_cash_custodies", "petty_cash_expenses",
            "petty_cash_settlements", "salary_records", "fixed_assets", "depreciation_records",
            "bank_statements", "bank_statement_lines", "bank_reconciliations",
            "reconciliation_matches", "reconciliation_adjustments", "bad_debt_writeoffs",
            "bad_debt_reversals", "bad_debt_attachments", "recurring_journal_templates",
            "recurring_journal_lines", "recurring_journal_executions",
            "recurring_invoice_templates", "recurring_invoice_lines",
            "warehouse_transfers", "warehouse_transfer_lines", "warehouse_transfer_log",
            "cost_centers", "cost_allocations", "cost_center_transactions", "cost_history",
            "budgets", "accounting_periods", "late_fee_policies",
            "early_payment_discount_policies", "party_discount_policies",
            "party_late_fee_exceptions", "kit_components", "kit_component_history",
            "audit_log", "app_audit_logs", "login_attempts", "idempotency_keys"
        };
        foreach (var t in operational)
        {
            var left = Count(t);
            Assert.True(left == 0, "بقيت " + left + " صف في الجدول " + t + " بعد إعادة الضبط");
        }

        // البيانات الأساسية حُذفت (لأن KeepMasterData = false)
        Assert.Equal(0, Count("parties"));
        Assert.Equal(0, Count("items"));
        Assert.Equal(0, Count("accounts"));
        Assert.Equal(0, Count("cash_boxes"));

        // والمستخدمون باقون
        Assert.Equal(usersBefore, Count("users"));

        // ولا سجلات يتيمة
        foreach (var kv in OrphanCounts())
            Assert.True(kv.Value == 0, "سجلات يتيمة بعد إعادة الضبط: " + kv.Key + " = " + kv.Value);
    }

    /// <summary>إعادة الضبط مع الاحتفاظ بالبيانات الأساسية تُبقي الأطراف والأصناف.</summary>
    [Fact]
    public void Factory_Reset_Can_Keep_Master_Data()
    {
        SeedAllTables();
        var partiesBefore = Count("parties");
        var itemsBefore = Count("items");
        Assert.True(partiesBefore > 0 && itemsBefore > 0);

        var options = new FactoryResetOptions
        {
            KeepUsersAndRoles = true,
            KeepMasterData = true,
            KeepSettings = true
        };

        DataManagementService.ExecuteFactoryReset(options);

        Assert.Equal(partiesBefore, Count("parties"));
        Assert.Equal(itemsBefore, Count("items"));
        Assert.Equal(0, Count("invoices"));
        Assert.Equal(0, Count("payments"));
    }

    // ==================== 5) الحذف الجزئي ====================

    /// <summary>حذف بيانات عميل واحد لا يمس بيانات عميل آخر.</summary>
    [Fact]
    public void Party_Scope_Deletes_Only_That_Partys_Data()
    {
        SeedAllTables();

        // عميلان: الأول له بيانات، الثاني يبقى
        using (var conn = Open())
        {
            var partyA = InsertParty(conn, "DM-PARTY-A");
            var partyB = InsertParty(conn, "DM-PARTY-B");
            InsertInvoice(conn, partyA, "DM-INV-A");
            InsertInvoice(conn, partyB, "DM-INV-B");

            var scope = new DeleteScope { Kind = "party", PartyId = partyA };
            var preview = DataManagementService.PreviewScope(scope);
            Assert.True(preview.TotalRows > 0);

            var result = DataManagementService.ExecuteScope(scope);
            Assert.True(result.Success);

            // فاتورة العميل الأول حُذفت، والثانية باقية
            Assert.Equal(0, Count("invoices", "party_id = " + partyA));
            Assert.Equal(1, Count("invoices", "party_id = " + partyB));

            // سطرا الفاتورة المرتبطان حُذفا أيضًا
            Assert.Equal(0, Count("invoice_lines",
                "invoice_id IN (SELECT id FROM invoices WHERE party_id = " + partyA + ")"));

            // والعميلان ما زالا موجودين (لم يُطلب حذف سجل العميل نفسه)
            Assert.True(Count("parties", "id = " + partyA) == 1);
            Assert.True(Count("parties", "id = " + partyB) == 1);
        }
    }

    /// <summary>حذف بيانات عميل مع سجله نفسه.</summary>
    [Fact]
    public void Party_Scope_Can_Delete_The_Party_Record_Too()
    {
        SeedAllTables();
        int partyId;
        using (var conn = Open())
        {
            partyId = InsertParty(conn, "DM-PARTY-DEL");
            InsertInvoice(conn, partyId, "DM-INV-DEL");
        }

        var scope = new DeleteScope { Kind = "party", PartyId = partyId, IncludePartyRecord = true };
        DataManagementService.ExecuteScope(scope);

        Assert.Equal(0, Count("parties", "id = " + partyId));
        Assert.Equal(0, Count("invoices", "party_id = " + partyId));
    }

    /// <summary>حذف الحركات قبل تاريخ معيّن لا يمس ما بعده.</summary>
    [Fact]
    public void Date_Scope_Deletes_Only_Older_Transactions()
    {
        SeedAllTables();

        int oldInvoiceId, newInvoiceId;
        using (var conn = Open())
        {
            oldInvoiceId = InsertInvoice(conn, InsertParty(conn, "DM-DT-OLD"), "DM-OLD", "2020-01-10");
            newInvoiceId = InsertInvoice(conn, InsertParty(conn, "DM-DT-NEW"), "DM-NEW", "2099-01-10");
        }

        var scope = new DeleteScope { Kind = "before_date", Before = new DateTime(2025, 1, 1) };
        var result = DataManagementService.ExecuteScope(scope);
        Assert.True(result.Success);

        Assert.Equal(0, Count("invoices", "id = " + oldInvoiceId));
        Assert.Equal(1, Count("invoices", "id = " + newInvoiceId));
    }

    // ==================== 6) فحص السجلات اليتيمة ====================

    /// <summary>كل علاقة (جدول ابن → جدول أب) في القاعدة — تُستخدم لكشف اليتّم.</summary>
    private static readonly (string Child, string Column, string Parent)[] Relations =
    {
        ("invoice_lines", "invoice_id", "invoices"),
        ("invoice_lines", "item_id", "items"),
        ("invoice_payments", "invoice_id", "invoices"),
        ("invoice_discount_applications", "invoice_id", "invoices"),
        ("invoice_discount_applications", "payment_id", "payments"),
        ("invoice_late_fees", "invoice_id", "invoices"),
        ("payments", "invoice_id", "invoices"),
        ("payments", "party_id", "parties"),
        ("payments", "cash_box_id", "cash_boxes"),
        ("payments", "currency_id", "currencies"),
        ("payments", "account_id", "accounts"),
        ("invoices", "party_id", "parties"),
        ("invoices", "currency_id", "currencies"),
        ("invoices", "cash_box_id", "cash_boxes"),
        ("invoices", "warehouse_id", "warehouses"),
        ("invoices", "reference_invoice_id", "invoices"),
        ("subscriptions", "party_id", "parties"),
        ("subscription_periods", "subscription_id", "subscriptions"),
        ("subscription_periods", "invoice_id", "invoices"),
        ("subscription_periods", "payment_id", "payments"),
        ("journal_entries", "period_id", "accounting_periods"),
        ("journal_entries", "source_invoice_id", "invoices"),
        ("journal_entries", "source_payment_id", "payments"),
        ("journal_lines", "entry_id", "journal_entries"),
        ("journal_lines", "account_id", "accounts"),
        ("cash_recognition_log", "journal_entry_id", "journal_entries"),
        ("cash_recognition_log", "payment_id", "payments"),
        ("cash_transfers", "from_cash_box_id", "cash_boxes"),
        ("cash_transfers", "to_cash_box_id", "cash_boxes"),
        ("cash_transfers", "fee_account_id", "accounts"),
        ("cash_transfer_log", "transfer_id", "cash_transfers"),
        ("expenses", "category_id", "expense_categories"),
        ("expenses", "cash_box_id", "cash_boxes"),
        ("shift_closings", "cash_box_id", "cash_boxes"),
        ("inventory_movements", "item_id", "items"),
        ("inventory_movements", "warehouse_id", "warehouses"),
        ("stock_levels", "item_id", "items"),
        ("stock_levels", "warehouse_id", "warehouses"),
        ("stock_counts", "item_id", "items"),
        ("stock_counts", "warehouse_id", "warehouses"),
        ("stock_counts", "session_id", "stock_count_sessions"),
        ("stock_count_sessions", "warehouse_id", "warehouses"),
        ("stock_adjustments", "item_id", "items"),
        ("stock_adjustments", "warehouse_id", "warehouses"),
        ("stock_adjustments", "movement_id", "inventory_movements"),
        ("damaged_items", "item_id", "items"),
        ("damaged_items", "warehouse_id", "warehouses"),
        ("damaged_item_inspection_log", "damaged_item_id", "damaged_items"),
        ("writeoff_requests", "item_id", "items"),
        ("writeoff_requests", "damaged_item_id", "damaged_items"),
        ("writeoff_requests", "movement_id", "inventory_movements"),
        ("custody_increase_requests", "custody_id", "petty_cash_custodies"),
        ("cost_history", "item_id", "items"),
        ("cost_history", "invoice_id", "invoices"),
        ("kit_components", "kit_item_id", "items"),
        ("kit_components", "component_item_id", "items"),
        ("kit_component_history", "kit_item_id", "items"),
        ("loan_installments", "loan_id", "loans"),
        ("loan_payments", "loan_id", "loans"),
        ("loan_payments", "installment_id", "loan_installments"),
        ("loan_rescheduling_history", "loan_id", "loans"),
        ("overdraft_transactions", "loan_id", "loans"),
        ("petty_cash_custodies", "employee_id", "employees"),
        ("petty_cash_custodies", "cash_box_id", "cash_boxes"),
        ("petty_cash_expenses", "custody_id", "petty_cash_custodies"),
        ("petty_cash_settlements", "custody_id", "petty_cash_custodies"),
        ("salary_records", "employee_id", "employees"),
        ("depreciation_records", "asset_id", "fixed_assets"),
        ("bank_statements", "cash_box_id", "cash_boxes"),
        ("bank_statement_lines", "statement_id", "bank_statements"),
        ("bank_statement_lines", "matched_payment_id", "payments"),
        ("bank_statement_import_logs", "statement_id", "bank_statements"),
        ("bank_reconciliations", "cash_box_id", "cash_boxes"),
        ("bank_reconciliations", "bank_statement_id", "bank_statements"),
        ("reconciliation_matches", "reconciliation_id", "bank_reconciliations"),
        ("reconciliation_matches", "bank_statement_line_id", "bank_statement_lines"),
        ("reconciliation_matches", "system_payment_id", "payments"),
        ("reconciliation_adjustments", "reconciliation_id", "bank_reconciliations"),
        ("bad_debt_writeoffs", "party_id", "parties"),
        ("bad_debt_writeoffs", "invoice_id", "invoices"),
        ("bad_debt_reversals", "writeoff_id", "bad_debt_writeoffs"),
        ("bad_debt_attachments", "writeoff_id", "bad_debt_writeoffs"),
        ("recurring_invoice_templates", "party_id", "parties"),
        ("recurring_invoice_lines", "template_id", "recurring_invoice_templates"),
        ("recurring_journal_lines", "template_id", "recurring_journal_templates"),
        ("recurring_journal_executions", "template_id", "recurring_journal_templates"),
        ("warehouse_transfers", "from_warehouse_id", "warehouses"),
        ("warehouse_transfers", "to_warehouse_id", "warehouses"),
        ("warehouse_transfer_lines", "transfer_id", "warehouse_transfers"),
        ("warehouse_transfer_log", "transfer_id", "warehouse_transfers"),
        ("cost_centers", "parent_id", "cost_centers"),
        ("cost_allocations", "cost_center_id", "cost_centers"),
        ("cost_allocations", "account_id", "accounts"),
        ("cost_center_transactions", "cost_center_id", "cost_centers"),
        ("budgets", "account_id", "accounts"),
        ("late_fee_policies", "fee_income_account_id", "accounts"),
        ("early_payment_discount_policies", "discount_account_id", "accounts"),
        ("party_discount_policies", "party_id", "parties"),
        ("party_late_fee_exceptions", "party_id", "parties"),
        ("items", "category_id", "categories"),
        ("items", "inventory_account_id", "accounts"),
        ("items", "cogs_account_id", "accounts"),
        ("items", "revenue_account_id", "accounts"),
        ("parties", "account_id", "accounts"),
        ("cash_boxes", "account_id", "accounts"),
        ("expense_categories", "account_id", "accounts"),
        ("user_permissions", "user_id", "users"),
        ("user_devices", "user_id", "users"),
        ("role_permissions", "role_id", "roles"),
        ("users", "role_id", "roles"),
        ("accounts", "parent_id", "accounts"),
        ("categories", "parent_id", "categories"),
        ("cost_centers", "parent_id", "cost_centers"),
        ("journal_entries", "source_invoice_id", "invoices"),
        ("journal_entries", "source_payment_id", "payments"),
        ("journal_entries", "period_id", "accounting_periods"),
        ("bad_debt_writeoffs", "journal_entry_id", "journal_entries"),
        ("bad_debt_reversals", "journal_entry_id", "journal_entries"),
        ("invoice_late_fees", "journal_entry_id", "journal_entries"),
        ("invoice_discount_applications", "journal_entry_id", "journal_entries"),
        ("custody_increase_requests", "journal_entry_id", "journal_entries"),
        ("petty_cash_custodies", "journal_entry_id", "journal_entries"),
        ("petty_cash_expenses", "journal_entry_id", "journal_entries"),
        ("petty_cash_settlements", "journal_entry_id", "journal_entries"),
        ("recurring_journal_executions", "journal_entry_id", "journal_entries"),
        ("reconciliation_adjustments", "journal_entry_id", "journal_entries"),
    };

    /// <summary>عدد السجلات اليتيمة لكل علاقة.</summary>
    private static Dictionary<string, long> OrphanCounts()
    {
        var result = new Dictionary<string, long>();
        using var conn = Open();
        foreach (var (child, column, parent) in Relations)
        {
            try
            {
                using var cmd = new NpgsqlCommand(
                    $"SELECT COUNT(*) FROM \"{child}\" c LEFT JOIN \"{parent}\" p ON p.id = c.\"{column}\" " +
                    $"WHERE c.\"{column}\" IS NOT NULL AND p.id IS NULL", conn);
                result[$"{child}.{column}->{parent}"] = Convert.ToInt64(cmd.ExecuteScalar());
            }
            catch
            {
                result[$"{child}.{column}->{parent}"] = 0;
            }
        }
        return result;
    }

    // ==================== 7) الزرع ====================

    /// <summary>خريطة أعمدة المفاتيح الأجنبية: الجدول → (العمود → الجدول الأصل).</summary>
    private static readonly Dictionary<string, Dictionary<string, string>> FkMap = new()
    {
        ["roles"] = new() { ["id"] = "roles" },
        ["users"] = new() { ["role_id"] = "roles" },
        ["user_permissions"] = new() { ["user_id"] = "users" },
        ["user_devices"] = new() { ["user_id"] = "users" },
        ["role_permissions"] = new() { ["role_id"] = "roles" },
        ["accounts"] = new() { ["parent_id"] = "accounts" },
        ["currencies"] = new(),
        ["categories"] = new() { ["parent_id"] = "categories" },
        ["cash_boxes"] = new() { ["account_id"] = "accounts" },
        ["warehouses"] = new(),
        ["items"] = new()
        {
            ["category_id"] = "categories", ["inventory_account_id"] = "accounts",
            ["cogs_account_id"] = "accounts", ["revenue_account_id"] = "accounts"
        },
        ["parties"] = new() { ["account_id"] = "accounts" },
        ["expense_categories"] = new() { ["account_id"] = "accounts" },
        ["employees"] = new(),
        ["stock_reasons"] = new(),
        ["cost_centers"] = new() { ["parent_id"] = "cost_centers" },
        ["late_fee_policies"] = new() { ["fee_income_account_id"] = "accounts" },
        ["early_payment_discount_policies"] = new() { ["discount_account_id"] = "accounts" },
        ["app_settings"] = new(),
        ["reporting_settings"] = new(),
        ["accounting_periods"] = new(),
        ["fixed_assets"] = new(),
        ["recurring_journal_templates"] = new(),
        ["recurring_invoice_templates"] = new() { ["party_id"] = "parties" },
        ["invoices"] = new()
        {
            ["party_id"] = "parties", ["currency_id"] = "currencies", ["cash_box_id"] = "cash_boxes",
            ["warehouse_id"] = "warehouses", ["reference_invoice_id"] = "invoices"
        },
        ["invoice_lines"] = new() { ["invoice_id"] = "invoices", ["item_id"] = "items" },
        ["invoice_payments"] = new() { ["invoice_id"] = "invoices" },
        ["payments"] = new()
        {
            ["party_id"] = "parties", ["currency_id"] = "currencies", ["cash_box_id"] = "cash_boxes",
            ["account_id"] = "accounts", ["invoice_id"] = "invoices"
        },
        ["journal_entries"] = new()
        {
            ["period_id"] = "accounting_periods", ["source_invoice_id"] = "invoices",
            ["source_payment_id"] = "payments"
        },
        ["journal_lines"] = new() { ["entry_id"] = "journal_entries", ["account_id"] = "accounts" },
        ["cash_recognition_log"] = new() { ["journal_entry_id"] = "journal_entries", ["payment_id"] = "payments" },
        ["cash_transfers"] = new()
        {
            ["from_cash_box_id"] = "cash_boxes", ["to_cash_box_id"] = "cash_boxes",
            ["fee_account_id"] = "accounts", ["in_transit_account_id"] = "accounts"
        },
        ["cash_transfer_log"] = new() { ["transfer_id"] = "cash_transfers" },
        ["expenses"] = new() { ["category_id"] = "expense_categories", ["cash_box_id"] = "cash_boxes" },
        ["shift_closings"] = new() { ["cash_box_id"] = "cash_boxes" },
        ["inventory_movements"] = new()
        {
            ["item_id"] = "items", ["warehouse_id"] = "warehouses", ["reversal_of_id"] = "inventory_movements"
        },
        ["stock_levels"] = new() { ["item_id"] = "items", ["warehouse_id"] = "warehouses" },
        ["stock_count_sessions"] = new() { ["warehouse_id"] = "warehouses" },
        ["stock_counts"] = new()
        {
            ["item_id"] = "items", ["warehouse_id"] = "warehouses", ["session_id"] = "stock_count_sessions"
        },
        ["stock_adjustments"] = new()
        {
            ["item_id"] = "items", ["warehouse_id"] = "warehouses", ["movement_id"] = "inventory_movements"
        },
        ["damaged_items"] = new()
        {
            ["item_id"] = "items", ["warehouse_id"] = "warehouses", ["damage_reason_id"] = "stock_reasons"
        },
        ["damaged_item_inspection_log"] = new() { ["damaged_item_id"] = "damaged_items" },
        ["writeoff_requests"] = new()
        {
            ["item_id"] = "items", ["damaged_item_id"] = "damaged_items", ["warehouse_id"] = "warehouses",
            ["writeoff_reason_id"] = "stock_reasons", ["movement_id"] = "inventory_movements"
        },
        ["custody_increase_requests"] = new() { ["custody_id"] = "petty_cash_custodies" },
        ["cost_history"] = new() { ["item_id"] = "items", ["invoice_id"] = "invoices", ["supplier_id"] = "parties" },
        ["kit_components"] = new()
        {
            ["kit_item_id"] = "items", ["component_item_id"] = "items", ["warehouse_id"] = "warehouses"
        },
        ["kit_component_history"] = new()
        {
            ["kit_item_id"] = "items", ["component_item_id"] = "items", ["warehouse_id"] = "warehouses"
        },
        ["loans"] = new()
        {
            ["cash_box_id"] = "cash_boxes", ["bank_account_id"] = "accounts",
            ["liability_account_id"] = "accounts", ["interest_expense_account_id"] = "accounts",
            ["employee_id"] = "employees", ["custody_id"] = "petty_cash_custodies"
        },
        ["loan_installments"] = new() { ["loan_id"] = "loans" },
        ["loan_payments"] = new()
        {
            ["loan_id"] = "loans", ["installment_id"] = "loan_installments", ["cash_box_id"] = "cash_boxes"
        },
        ["loan_rescheduling_history"] = new() { ["loan_id"] = "loans" },
        ["overdraft_transactions"] = new() { ["loan_id"] = "loans" },
        ["petty_cash_custodies"] = new()
        {
            ["employee_id"] = "employees", ["cash_box_id"] = "cash_boxes", ["custody_account_id"] = "accounts"
        },
        ["petty_cash_expenses"] = new()
        {
            ["custody_id"] = "petty_cash_custodies", ["expense_category_id"] = "expense_categories",
            ["expense_account_id"] = "accounts"
        },
        ["petty_cash_settlements"] = new()
        {
            ["custody_id"] = "petty_cash_custodies", ["shortage_account_id"] = "accounts",
            ["cash_box_id"] = "cash_boxes"
        },
        ["salary_records"] = new() { ["employee_id"] = "employees" },
        ["depreciation_records"] = new() { ["asset_id"] = "fixed_assets" },
        ["bank_statements"] = new() { ["cash_box_id"] = "cash_boxes" },
        ["bank_statement_lines"] = new()
        {
            ["statement_id"] = "bank_statements", ["matched_payment_id"] = "payments",
            ["matched_journal_line_id"] = "journal_lines"
        },
        ["bank_statement_import_logs"] = new()
        {
            ["statement_id"] = "bank_statements", ["template_id"] = "bank_statement_import_templates"
        },
        ["bank_statement_import_templates"] = new(),
        ["bank_reconciliations"] = new()
        {
            ["cash_box_id"] = "cash_boxes", ["bank_statement_id"] = "bank_statements"
        },
        ["reconciliation_matches"] = new()
        {
            ["reconciliation_id"] = "bank_reconciliations", ["bank_statement_line_id"] = "bank_statement_lines",
            ["system_payment_id"] = "payments", ["system_journal_line_id"] = "journal_lines"
        },
        ["reconciliation_adjustments"] = new()
        {
            ["reconciliation_id"] = "bank_reconciliations", ["bank_statement_line_id"] = "bank_statement_lines",
            ["target_account_id"] = "accounts"
        },
        ["bad_debt_writeoffs"] = new()
        {
            ["party_id"] = "parties", ["invoice_id"] = "invoices", ["bad_debt_account_id"] = "accounts",
            ["ar_account_id"] = "accounts"
        },
        ["bad_debt_reversals"] = new() { ["writeoff_id"] = "bad_debt_writeoffs" },
        ["bad_debt_attachments"] = new() { ["writeoff_id"] = "bad_debt_writeoffs" },
        ["invoice_discount_applications"] = new()
        {
            ["invoice_id"] = "invoices", ["payment_id"] = "payments",
            ["policy_id"] = "early_payment_discount_policies", ["party_policy_id"] = "party_discount_policies",
            ["discount_account_id"] = "accounts"
        },
        ["invoice_late_fees"] = new() { ["invoice_id"] = "invoices", ["policy_id"] = "late_fee_policies" },
        ["party_discount_policies"] = new()
        {
            ["party_id"] = "parties", ["policy_id"] = "early_payment_discount_policies"
        },
        ["party_late_fee_exceptions"] = new() { ["party_id"] = "parties", ["policy_id"] = "late_fee_policies" },
        ["recurring_invoice_lines"] = new()
        {
            ["template_id"] = "recurring_invoice_templates", ["item_id"] = "items"
        },
        ["recurring_journal_lines"] = new()
        {
            ["template_id"] = "recurring_journal_templates", ["account_id"] = "accounts",
            ["cost_center_id"] = "cost_centers"
        },
        ["recurring_journal_executions"] = new()
        {
            ["template_id"] = "recurring_journal_templates", ["journal_entry_id"] = "journal_entries"
        },
        ["warehouse_transfers"] = new()
        {
            ["from_warehouse_id"] = "warehouses", ["to_warehouse_id"] = "warehouses", ["item_id"] = "items"
        },
        ["warehouse_transfer_lines"] = new() { ["transfer_id"] = "warehouse_transfers", ["item_id"] = "items" },
        ["warehouse_transfer_log"] = new() { ["transfer_id"] = "warehouse_transfers" },
        ["cost_allocations"] = new() { ["cost_center_id"] = "cost_centers", ["account_id"] = "accounts" },
        ["cost_center_transactions"] = new() { ["cost_center_id"] = "cost_centers" },
        ["budgets"] = new() { ["account_id"] = "accounts" },
        ["subscriptions"] = new() { ["party_id"] = "parties" },
        ["subscription_periods"] = new()
        {
            ["subscription_id"] = "subscriptions", ["invoice_id"] = "invoices", ["payment_id"] = "payments"
        },
        ["audit_log"] = new(),
        ["app_audit_logs"] = new(),
        ["login_attempts"] = new(),
        ["idempotency_keys"] = new(),
    };

    /// <summary>ترتيب الزرع: الأبناء بعد الآباء.</summary>
    private static readonly string[] SeedOrder =
    {
        "roles", "users", "user_permissions", "user_devices", "role_permissions",
        "accounts", "currencies", "categories", "cash_boxes", "warehouses", "items", "parties",
        "expense_categories", "employees", "stock_reasons", "cost_centers",
        "late_fee_policies", "early_payment_discount_policies", "app_settings", "reporting_settings",
        "accounting_periods", "fixed_assets", "recurring_journal_templates", "recurring_invoice_templates",
        "invoices", "invoice_lines", "invoice_payments", "payments",
        "journal_entries", "journal_lines", "cash_recognition_log",
        "cash_transfers", "cash_transfer_log", "expenses", "shift_closings",
        "inventory_movements", "stock_levels", "stock_count_sessions", "stock_counts", "stock_adjustments",
        "damaged_items", "damaged_item_inspection_log", "writeoff_requests", "custody_increase_requests",
        "cost_history", "kit_components", "kit_component_history",
        "loans", "loan_installments", "loan_payments", "loan_rescheduling_history", "overdraft_transactions",
        "petty_cash_custodies", "petty_cash_expenses", "petty_cash_settlements", "salary_records",
        "depreciation_records",
        "bank_statement_import_templates", "bank_statements", "bank_statement_lines",
        "bank_statement_import_logs", "bank_reconciliations", "reconciliation_matches",
        "reconciliation_adjustments",
        "bad_debt_writeoffs", "bad_debt_reversals", "bad_debt_attachments",
        "invoice_discount_applications", "invoice_late_fees",
        "party_discount_policies", "party_late_fee_exceptions",
        "recurring_invoice_lines", "recurring_journal_lines", "recurring_journal_executions",
        "warehouse_transfers", "warehouse_transfer_lines", "warehouse_transfer_log",
        "cost_allocations", "cost_center_transactions", "budgets",
        "subscriptions", "subscription_periods",
        "audit_log", "app_audit_logs", "login_attempts", "idempotency_keys"
    };

    /// <summary>يزرع صفًا واحدًا في كل جدول — ويعيد قائمة الجداول التي نجح زرعها.</summary>
    private static List<string> SeedAllTables()
    {
        var seeded = new List<string>();
        using var conn = Open();
        foreach (var table in SeedOrder)
        {
            try
            {
                InsertRow(conn, table);
                seeded.Add(table);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[seed] تعذّر زرع " + table + ": " + ex.Message);
            }
        }
        return seeded;
    }

    private static void InsertRow(NpgsqlConnection conn, string table)
    {
        var columns = new List<(string Name, string Type, bool Nullable, string? Default)>();
        using (var cmd = new NpgsqlCommand(
            "SELECT column_name, data_type, is_nullable, column_default FROM information_schema.columns " +
            "WHERE table_schema='public' AND table_name=@t ORDER BY ordinal_position", conn))
        {
            cmd.Parameters.AddWithValue("@t", table);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                columns.Add((r.GetString(0), r.GetString(1), r.GetString(2) == "YES", r.IsDBNull(3) ? null : r.GetString(3)));
        }
        if (columns.Count == 0) return;

        var names = new List<string>();
        var values = new List<object?>();
        var marker = "DM_" + table + "_" + Guid.NewGuid().ToString("N").Substring(0, 6);

        foreach (var (name, type, nullable, def) in columns)
        {
            if (name.Equals("id", StringComparison.OrdinalIgnoreCase)) continue;
            if (def != null && def.StartsWith("nextval(")) continue;   // serial

            object? value;
            if (FkMap.TryGetValue(table, out var fks) && fks.TryGetValue(name, out var parent))
            {
                value = FirstId(conn, parent);
                if (value == null)
                {
                    if (nullable) continue;
                    throw new InvalidOperationException("لا يوجد صف في " + parent + " لعمود " + name);
                }
            }
            else
            {
                value = DefaultValue(name, type, marker);
                if (value == null && !nullable)
                    throw new InvalidOperationException("لا توجد قيمة لعمود إلزامي " + name);
                if (value == null) continue;
            }

            names.Add(name);
            values.Add(value);
        }

        if (names.Count == 0) return;

        var sql = $"INSERT INTO \"{table}\" (" +
                  string.Join(", ", names.Select(n => "\"" + n + "\"")) + ") VALUES (" +
                  string.Join(", ", names.Select((_, i) => "@p" + i)) + ")";
        using var ins = new NpgsqlCommand(sql, conn);
        for (var i = 0; i < names.Count; i++) ins.Parameters.AddWithValue("@p" + i, values[i]);
        ins.ExecuteNonQuery();
    }

    private static object? FirstId(NpgsqlConnection conn, string table)
    {
        try
        {
            using var cmd = new NpgsqlCommand($"SELECT id FROM \"{table}\" ORDER BY id LIMIT 1", conn);
            var v = cmd.ExecuteScalar();
            return v == null || v == DBNull.Value ? null : v;
        }
        catch { return null; }
    }

    private static object? DefaultValue(string name, string type, string marker)
    {
        if (type.Contains("timestamp", StringComparison.OrdinalIgnoreCase))
            return new DateTime(2024, 1, 15, 10, 0, 0);
        if (type.Contains("date", StringComparison.OrdinalIgnoreCase))
            return new DateTime(2024, 1, 15);
        if (type.Contains("numeric", StringComparison.OrdinalIgnoreCase) ||
            type.Contains("decimal", StringComparison.OrdinalIgnoreCase) ||
            type.Contains("double", StringComparison.OrdinalIgnoreCase) ||
            type.Contains("real", StringComparison.OrdinalIgnoreCase))
            return 10.0m;
        if (type.Contains("bool", StringComparison.OrdinalIgnoreCase))
            return true;
        if (type.Contains("json", StringComparison.OrdinalIgnoreCase))
            return "{}";
        if (type == "integer" || type == "bigint" || type == "smallint")
            return 1;
        if (type.Contains("char", StringComparison.OrdinalIgnoreCase) || type.Contains("text", StringComparison.OrdinalIgnoreCase))
            return marker + "_" + name;
        return null;
    }

    // ==================== مساعدات ====================

    private static int InsertParty(NpgsqlConnection conn, string name)
    {
        using var cmd = new NpgsqlCommand(
            "INSERT INTO parties (name, type, account_id, is_active) VALUES (@n, 'customer', @a, true) RETURNING id", conn);
        cmd.Parameters.AddWithValue("@n", name);
        cmd.Parameters.AddWithValue("@a", FirstId(conn, "accounts") ?? (object)1);
        return (int)cmd.ExecuteScalar();
    }

    private static int InsertInvoice(NpgsqlConnection conn, int partyId, string number, string date = "2024-06-01")
    {
        using var cmd = new NpgsqlCommand(
            "INSERT INTO invoices (invoice_number, date, due_date, party_id, type, currency_id, total_amount, status) " +
            "VALUES (@n, @d, @d2, @p, 'sale', @c, 100, 'open') RETURNING id", conn);
        cmd.Parameters.AddWithValue("@n", number);
        cmd.Parameters.AddWithValue("@d", DateTime.Parse(date));
        cmd.Parameters.AddWithValue("@d2", DateTime.Parse(date).AddDays(30));
        cmd.Parameters.AddWithValue("@p", partyId);
        cmd.Parameters.AddWithValue("@c", FirstId(conn, "currencies") ?? (object)1);
        var id = (int)cmd.ExecuteScalar();

        using var line = new NpgsqlCommand(
            "INSERT INTO invoice_lines (invoice_id, item_id, quantity, price, total) " +
            "VALUES (@i, @it, 1, 100, 100)", conn);
        line.Parameters.AddWithValue("@i", id);
        line.Parameters.AddWithValue("@it", FirstId(conn, "items") ?? (object)1);
        line.ExecuteNonQuery();
        return id;
    }

    private static void Execute(NpgsqlConnection conn, string sql)
    {
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.ExecuteNonQuery();
    }
}
