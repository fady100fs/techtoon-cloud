using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using Techtoon.UI.Services;
using Xunit;

namespace Techtoon.Tests;

/// <summary>
/// إغلاق فجوات التفاصيل والسجلات — اختبارات تكامل على قاعدة الاختبار المعزولة:
///  * warehouse_transfer_lines + warehouse_transfer_log
///  * kit_component_history
///  * bad_debt_attachments
///  * recurring_journal_executions
/// </summary>
[Collection("DatabaseIntegration")]
public class ClosedLoopDetailsIntegrationTests
{
    // ==================== تحويل المخازن ====================

    [Fact]
    public async Task CreateTransfer_Writes_Lines_And_Log()
    {
        if (!TestDb.Available) return;

        var svc = new ClosedLoopDetailsService(TestDb.ConnectionString!);

        int fromWh = await EnsureWarehouse("مخزن مصدر " + Guid.NewGuid().ToString("N").Substring(0, 6));
        int toWh = await EnsureWarehouse("مخزن هدف " + Guid.NewGuid().ToString("N").Substring(0, 6));
        int itemId = await EnsureItem("صنف تحويل " + Guid.NewGuid().ToString("N").Substring(0, 6));

        var lines = new List<ClosedLoopDetailsService.TransferLine>
        {
            new ClosedLoopDetailsService.TransferLine { ItemId = itemId, RequestedQuantity = 10, ShippedQuantity = 10, ReceivedQuantity = 9, MissingQuantity = 1, DifferenceReason = "تالف" },
            new ClosedLoopDetailsService.TransferLine { ItemId = itemId, RequestedQuantity = 5, ShippedQuantity = 5, ReceivedQuantity = 5 }
        };

        int transferId = await svc.CreateTransferAsync(fromWh, toWh, DateTime.Today, lines, "اختبار", "draft");

        // السطور
        var lineRows = Convert.ToInt32(TestDb.Scalar(
            "SELECT COUNT(*) FROM warehouse_transfer_lines WHERE transfer_id=@t", ("@t", transferId)));
        Assert.Equal(2, lineRows);

        var qty = Convert.ToDecimal(TestDb.Scalar(
            "SELECT SUM(requested_quantity) FROM warehouse_transfer_lines WHERE transfer_id=@t", ("@t", transferId)));
        Assert.Equal(15m, qty);

        // سجل الحركات
        var logRows = Convert.ToInt32(TestDb.Scalar(
            "SELECT COUNT(*) FROM warehouse_transfer_log WHERE transfer_id=@t", ("@t", transferId)));
        Assert.Equal(1, logRows);

        var action = TestDb.Scalar(
            "SELECT action FROM warehouse_transfer_log WHERE transfer_id=@t", ("@t", transferId));
        Assert.Equal("create", action?.ToString());

        // ---- تنظيف ----
        TestDb.Execute("DELETE FROM warehouse_transfer_lines WHERE transfer_id=@t", ("@t", transferId));
        TestDb.Execute("DELETE FROM warehouse_transfer_log WHERE transfer_id=@t", ("@t", transferId));
        TestDb.Execute("DELETE FROM warehouse_transfers WHERE id=@t", ("@t", transferId));
    }

    [Fact]
    public async Task CreateTransfer_Rejects_Same_Warehouse_Or_No_Lines()
    {
        if (!TestDb.Available) return;
        var svc = new ClosedLoopDetailsService(TestDb.ConnectionString!);
        int wh = await EnsureWarehouse("مخزن " + Guid.NewGuid().ToString("N").Substring(0, 6));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateTransferAsync(wh, wh, DateTime.Today,
                new List<ClosedLoopDetailsService.TransferLine> { new() { ItemId = 1, RequestedQuantity = 1 } }, "x", "draft"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateTransferAsync(wh, wh + 1000, DateTime.Today,
                new List<ClosedLoopDetailsService.TransferLine>(), "x", "draft"));
    }

    // ==================== تاريخ مكوّنات الكيت ====================

    [Fact]
    public async Task KitComponentHistory_Is_Recorded()
    {
        if (!TestDb.Available) return;
        var svc = new ClosedLoopDetailsService(TestDb.ConnectionString!);

        int kitId = await EnsureItem("كيت " + Guid.NewGuid().ToString("N").Substring(0, 6));
        int compId = await EnsureItem("مكوّن " + Guid.NewGuid().ToString("N").Substring(0, 6));

        await svc.RecordKitComponentHistoryAsync(kitId, compId, 3, null, "added", null, 3, null, null, "إضافة");

        var rows = Convert.ToInt32(TestDb.Scalar(
            "SELECT COUNT(*) FROM kit_component_history WHERE kit_item_id=@k", ("@k", kitId)));
        Assert.Equal(1, rows);

        var changeType = TestDb.Scalar(
            "SELECT change_type FROM kit_component_history WHERE kit_item_id=@k", ("@k", kitId));
        Assert.Equal("added", changeType?.ToString());

        var newQty = Convert.ToDecimal(TestDb.Scalar(
            "SELECT new_quantity FROM kit_component_history WHERE kit_item_id=@k", ("@k", kitId)));
        Assert.Equal(3m, newQty);

        TestDb.Execute("DELETE FROM kit_component_history WHERE kit_item_id=@k", ("@k", kitId));
    }

    // ==================== مرفقات الديون المعدومة ====================

    [Fact]
    public async Task BadDebtAttachment_Is_Saved_And_Recorded()
    {
        if (!TestDb.Available) return;
        var svc = new ClosedLoopDetailsService(TestDb.ConnectionString!);

        // شطب ديون حقيقي (لأن writeoff_id NOT NULL + FK)
        int writeoffId = await EnsureBadDebtWriteoff();

        // ملف مرفق حقيقي
        var sandbox = Path.Combine(Path.GetTempPath(), "techtoon_att_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sandbox);
        try
        {
            var file = Path.Combine(sandbox, "evidence.txt");
            File.WriteAllText(file, "proof of debt evidence", new UTF8Encoding(false));

            var att = await svc.AddBadDebtAttachmentAsync(writeoffId, file, "إثبات دين");

            Assert.True(att.Id > 0);
            Assert.Equal("evidence.txt", att.FileName);
            Assert.True(File.Exists(att.FilePath));
            Assert.True(att.FileSize > 0);
            Assert.False(string.IsNullOrEmpty(att.FileHash));

            // محفوظ في القاعدة
            var rows = Convert.ToInt32(TestDb.Scalar(
                "SELECT COUNT(*) FROM bad_debt_attachments WHERE writeoff_id=@w", ("@w", writeoffId)));
            Assert.Equal(1, rows);

            // يُقرأ عبر الخدمة
            var list = await svc.GetBadDebtAttachmentsAsync(writeoffId);
            Assert.Contains(list, a => a.FileName == "evidence.txt");

            TestDb.Execute("DELETE FROM bad_debt_attachments WHERE writeoff_id=@w", ("@w", writeoffId));
        }
        finally
        {
            try { if (Directory.Exists(sandbox)) Directory.Delete(sandbox, true); } catch { }
        }
    }

    [Fact]
    public async Task BadDebtAttachment_Rejects_Missing_File()
    {
        if (!TestDb.Available) return;
        var svc = new ClosedLoopDetailsService(TestDb.ConnectionString!);
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            svc.AddBadDebtAttachmentAsync(1, Path.Combine(Path.GetTempPath(), "does_not_exist_xyz.pdf"), null));
    }

    // ==================== تنفيذ القيود الدورية ====================

    [Fact]
    public async Task ExecuteRecurringJournal_Creats_Entry_And_Execution_Record()
    {
        if (!TestDb.Available) return;
        var svc = new RecurringJournalService(TestDb.ConnectionString!);

        int debitAcc = await EnsureAccount("مصروف اختبار " + Guid.NewGuid().ToString("N").Substring(0, 6), "EXPENSE");
        int creditAcc = await EnsureAccount("إيراد اختبار " + Guid.NewGuid().ToString("N").Substring(0, 6), "REVENUE");
        int templateId = await EnsureRecurringTemplate(debitAcc, creditAcc, 100m, 100m);

        var res = await svc.ExecuteAsync(templateId, DateTime.Today);

        Assert.True(res.Success, res.Message);
        Assert.True(res.JournalEntryId.HasValue);
        Assert.Equal(100m, res.TotalDebit);
        Assert.Equal(100m, res.TotalCredit);

        // سطر التنفيذ موجود (كان الفجوة)
        var execRows = Convert.ToInt32(TestDb.Scalar(
            "SELECT COUNT(*) FROM recurring_journal_executions WHERE template_id=@t", ("@t", templateId)));
        Assert.Equal(1, execRows);

        var status = TestDb.Scalar(
            "SELECT status FROM recurring_journal_executions WHERE template_id=@t", ("@t", templateId));
        Assert.Equal("success", status?.ToString());

        // القيد متوازن
        var entryId = res.JournalEntryId!.Value;
        var dr = Convert.ToDecimal(TestDb.Scalar(
            "SELECT COALESCE(SUM(debit),0) FROM journal_lines WHERE entry_id=@e", ("@e", entryId)));
        var cr = Convert.ToDecimal(TestDb.Scalar(
            "SELECT COALESCE(SUM(credit),0) FROM journal_lines WHERE entry_id=@e", ("@e", entryId)));
        Assert.Equal(dr, cr);
        Assert.Equal(100m, dr);

        // القالب تحدّث
        var lastStatus = TestDb.Scalar(
            "SELECT last_run_status FROM recurring_journal_templates WHERE id=@t", ("@t", templateId));
        Assert.Equal("success", lastStatus?.ToString());

        // ---- تنظيف ----
        TestDb.Execute("DELETE FROM recurring_journal_executions WHERE template_id=@t", ("@t", templateId));
        TestDb.Execute("DELETE FROM journal_lines WHERE entry_id=@e", ("@e", entryId));
        TestDb.Execute("DELETE FROM journal_entries WHERE id=@e", ("@e", entryId));
        TestDb.Execute("DELETE FROM recurring_journal_lines WHERE template_id=@t", ("@t", templateId));
        TestDb.Execute("DELETE FROM recurring_journal_templates WHERE id=@t", ("@t", templateId));
    }

    [Fact]
    public async Task ExecuteRecurringJournal_Unbalanced_Records_Failure_And_No_Entry()
    {
        if (!TestDb.Available) return;
        var svc = new RecurringJournalService(TestDb.ConnectionString!);

        int debitAcc = await EnsureAccount("مدين غير متوازن " + Guid.NewGuid().ToString("N").Substring(0, 6), "EXPENSE");
        int creditAcc = await EnsureAccount("دائن غير متوازن " + Guid.NewGuid().ToString("N").Substring(0, 6), "REVENUE");
        int templateId = await EnsureRecurringTemplate(debitAcc, creditAcc, 100m, 70m); // غير متوازن

        var res = await svc.ExecuteAsync(templateId, DateTime.Today);

        Assert.False(res.Success);
        Assert.Contains("غير متوازن", res.Message);
        Assert.Null(res.JournalEntryId);

        // سطر فشل مسجَّل
        var execRows = Convert.ToInt32(TestDb.Scalar(
            "SELECT COUNT(*) FROM recurring_journal_executions WHERE template_id=@t AND status='failed'", ("@t", templateId)));
        Assert.Equal(1, execRows);

        // لا قيد ناقص
        var entries = Convert.ToInt32(TestDb.Scalar(
            "SELECT COUNT(*) FROM journal_entries WHERE reference_type='recurring_journal' AND reference_id=@t", ("@t", templateId)));
        Assert.Equal(0, entries);

        TestDb.Execute("DELETE FROM recurring_journal_executions WHERE template_id=@t", ("@t", templateId));
        TestDb.Execute("DELETE FROM recurring_journal_lines WHERE template_id=@t", ("@t", templateId));
        TestDb.Execute("DELETE FROM recurring_journal_templates WHERE id=@t", ("@t", templateId));
    }

    // ==================== مساعدات ====================

    private static async Task<int> EnsureWarehouse(string name)
    {
        return await Task.Run(() =>
        {
            using var conn = TestDb.Open();
            using var cmd = new NpgsqlCommand(
                "INSERT INTO warehouses (name, code, is_active) VALUES (@n, @c, TRUE) RETURNING id", conn);
            cmd.Parameters.AddWithValue("@n", name);
            cmd.Parameters.AddWithValue("@c", "W" + Guid.NewGuid().ToString("N").Substring(0, 8));
            return Convert.ToInt32(cmd.ExecuteScalar());
        });
    }

    private static async Task<int> EnsureItem(string name)
    {
        return await Task.Run(() =>
        {
            using var conn = TestDb.Open();
            using var cmd = new NpgsqlCommand(
                "INSERT INTO items (name, sell_price, cost_price, avg_cost_price, min_stock, opening_quantity, is_active) " +
                "VALUES (@n, 0, 0, 0, 0, 0, TRUE) RETURNING id", conn);
            cmd.Parameters.AddWithValue("@n", name);
            return Convert.ToInt32(cmd.ExecuteScalar());
        });
    }

    private static async Task<int> EnsureAccount(string name, string type)
    {
        return await Task.Run(() =>
        {
            using var conn = TestDb.Open();
            using var cmd = new NpgsqlCommand(
                "INSERT INTO accounts (code, name, parent_id, type) VALUES (@c, @n, NULL, @t) RETURNING id", conn);
            cmd.Parameters.AddWithValue("@c", "T" + Guid.NewGuid().ToString("N").Substring(0, 10));
            cmd.Parameters.AddWithValue("@n", name);
            cmd.Parameters.AddWithValue("@t", type);
            return Convert.ToInt32(cmd.ExecuteScalar());
        });
    }

    private static async Task<int> EnsureBadDebtWriteoff()
    {
        return await Task.Run(() =>
        {
            using var conn = TestDb.Open();
            int accountId;
            using (var cmd = new NpgsqlCommand(
                "INSERT INTO accounts (code, name, parent_id, type) VALUES (@c, @n, NULL, 'ASSET') RETURNING id", conn))
            {
                cmd.Parameters.AddWithValue("@c", "T" + Guid.NewGuid().ToString("N").Substring(0, 10));
                cmd.Parameters.AddWithValue("@n", "حساب ديون " + Guid.NewGuid().ToString("N").Substring(0, 6));
                accountId = Convert.ToInt32(cmd.ExecuteScalar());
            }
            int partyId;
            using (var cmd = new NpgsqlCommand(
                "INSERT INTO parties (name, type, account_id) VALUES (@n, 'customer', @a) RETURNING id", conn))
            {
                cmd.Parameters.AddWithValue("@n", "مدين " + Guid.NewGuid().ToString("N").Substring(0, 6));
                cmd.Parameters.AddWithValue("@a", accountId);
                partyId = Convert.ToInt32(cmd.ExecuteScalar());
            }
            using var cmd2 = new NpgsqlCommand(@"INSERT INTO bad_debt_writeoffs
                (writeoff_number, party_id, original_amount, remaining_amount, writeoff_amount,
                 writeoff_type, reason, writeoff_date, bad_debt_account_id, ar_account_id,
                 status, requested_by, requested_at)
                VALUES (@wn, @p, 100, 100, 100, 'full', 'اختبار', CURRENT_DATE, @a, @a, 'pending', 1, NOW())
                RETURNING id", conn);
            cmd2.Parameters.AddWithValue("@wn", "WD-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            cmd2.Parameters.AddWithValue("@p", partyId);
            cmd2.Parameters.AddWithValue("@a", accountId);
            var id = Convert.ToInt32(cmd2.ExecuteScalar());
            TestDb.Execute("DELETE FROM bad_debt_writeoffs WHERE id=@i", ("@i", id));
            TestDb.Execute("DELETE FROM parties WHERE id=@i", ("@i", partyId));
            return id;
        });
    }

    private static async Task<int> EnsureRecurringTemplate(int debitAcc, int creditAcc, decimal debit, decimal credit)
    {
        return await Task.Run(() =>
        {
            using var conn = TestDb.Open();
            using var tx = conn.BeginTransaction();
            int templateId;
            using (var cmd = new NpgsqlCommand(@"INSERT INTO recurring_journal_templates
                (template_number, name, description, frequency, execution_day, start_date,
                 is_active, auto_post, next_run_date, created_by, created_at)
                VALUES (@tn, @n, 'اختبار', 'monthly', 1, CURRENT_DATE, TRUE, FALSE, CURRENT_DATE, 1, NOW())
                RETURNING id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@tn", "RJ-" + Guid.NewGuid().ToString("N").Substring(0, 8));
                cmd.Parameters.AddWithValue("@n", "قالب دوري " + Guid.NewGuid().ToString("N").Substring(0, 6));
                templateId = Convert.ToInt32(cmd.ExecuteScalar());
            }
            using (var cmd = new NpgsqlCommand(@"INSERT INTO recurring_journal_lines
                (template_id, line_number, account_id, debit, credit, description, amount_rule)
                VALUES (@t, 1, @a, @d, 0, 'مدين', 'fixed')", conn, tx))
            {
                cmd.Parameters.AddWithValue("@t", templateId);
                cmd.Parameters.AddWithValue("@a", debitAcc);
                cmd.Parameters.AddWithValue("@d", debit);
                cmd.ExecuteNonQuery();
            }
            using (var cmd = new NpgsqlCommand(@"INSERT INTO recurring_journal_lines
                (template_id, line_number, account_id, debit, credit, description, amount_rule)
                VALUES (@t, 2, @a, 0, @c, 'دائن', 'fixed')", conn, tx))
            {
                cmd.Parameters.AddWithValue("@t", templateId);
                cmd.Parameters.AddWithValue("@a", creditAcc);
                cmd.Parameters.AddWithValue("@c", credit);
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
            return templateId;
        });
    }
}
