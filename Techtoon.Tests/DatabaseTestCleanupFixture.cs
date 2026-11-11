using System;
using System.Collections.Generic;
using Xunit;

namespace Techtoon.Tests;

/// <summary>
/// مُثبّت لمجموعة اختبارات قاعدة البيانات.
/// ينظّف آثار الاختبارات قبل تشغيل المجموعة وبعدها، حتى لا تتراكم بيانات وهمية
/// في قاعدة الاختبار وتُفسد نتايج اختبارات أخرى.
/// </summary>
public class DatabaseTestCleanupFixture : IDisposable
{
    /// <summary>أنماط الأكواد التي تنشئها أدوات الاختبار (حرف + أحرف سداسية عشرية).</summary>
    private static readonly string[] CleanupStatements =
    {
        // سطور الكيت والأرصدة أولًا (مفاتيح أجنبية)
        "DELETE FROM kit_components WHERE kit_item_id IN (SELECT id FROM items WHERE barcode ~ '^[KC][0-9a-f]{6,14}$') OR component_item_id IN (SELECT id FROM items WHERE barcode ~ '^[KC][0-9a-f]{6,14}$')",
        "DELETE FROM stock_levels WHERE item_id IN (SELECT id FROM items WHERE barcode ~ '^[KC][0-9a-f]{6,14}$')",
        "DELETE FROM invoice_lines WHERE item_id IN (SELECT id FROM items WHERE barcode ~ '^[KC][0-9a-f]{6,14}$')",
        "DELETE FROM inventory_movements WHERE item_id IN (SELECT id FROM items WHERE barcode ~ '^[KC][0-9a-f]{6,14}$')",
        // قوالب القيود الدورية وسجل تنفيذها
        "DELETE FROM recurring_journal_executions WHERE template_id IN (SELECT id FROM recurring_journal_templates WHERE name LIKE 'قالب دوري%' OR name LIKE '%اختبار%')",
        "DELETE FROM recurring_journal_lines WHERE template_id IN (SELECT id FROM recurring_journal_templates WHERE name LIKE 'قالب دوري%' OR name LIKE '%اختبار%')",
        "DELETE FROM recurring_journal_templates WHERE name LIKE 'قالب دوري%' OR name LIKE '%اختبار%'",
        "DELETE FROM recurring_invoice_lines WHERE template_id IN (SELECT id FROM recurring_invoice_templates WHERE name LIKE 'قالب%' OR name LIKE '%اختبار%')",
        "DELETE FROM recurring_invoice_templates WHERE name LIKE 'قالب%' OR name LIKE '%اختبار%'",
        // القيود المحاسبية المنشأة بالاختبار
        "DELETE FROM journal_lines WHERE entry_id IN (SELECT id FROM journal_entries WHERE description LIKE 'قيد دوري%' OR description LIKE '%اختبار%')",
        "DELETE FROM journal_entries WHERE description LIKE 'قيد دوري%' OR description LIKE '%اختبار%'",
        // حركات مراكز التكلفة
        "DELETE FROM cost_center_transactions WHERE cost_center_id IN (SELECT id FROM cost_centers WHERE name LIKE 'مركز تكلفة اختبار%' OR name LIKE '%اختبار%')",
        "DELETE FROM cost_centers WHERE name LIKE 'مركز تكلفة اختبار%' OR name LIKE '%اختبار%'",
        // خطوط الائتمان
        "DELETE FROM overdraft_transactions WHERE loan_id IN (SELECT id FROM loans WHERE loan_number LIKE 'OD-%')",
        "DELETE FROM loans WHERE loan_number LIKE 'OD-%'",
        // شطب الديون
        "DELETE FROM bad_debt_attachments WHERE writeoff_id IN (SELECT id FROM bad_debt_writeoffs WHERE writeoff_number LIKE 'WD-%')",
        "DELETE FROM bad_debt_writeoffs WHERE writeoff_number LIKE 'WD-%'",
        // الفترات المحاسبية للاختبار
        "DELETE FROM accounting_periods WHERE period_name LIKE 'فترة اختبار%' OR period_name LIKE 'فترة مفتوحة%'",
        // المخازن والأصناف والأطراف والحسابات
        "DELETE FROM warehouses WHERE name LIKE 'مخزن %'",
        "DELETE FROM items WHERE barcode ~ '^[KC][0-9a-f]{6,14}$' OR name LIKE '%اختبار%'",
        "DELETE FROM parties WHERE name LIKE '%اختبار%' AND name <> 'Test Customer'",
        "DELETE FROM accounts WHERE code ~ '^[TLC][0-9a-f]{6,14}$' OR name LIKE 'حساب%اختبار%'",
    };

    public DatabaseTestCleanupFixture()
    {
        Cleanup();
    }

    public void Dispose() => Cleanup();

    private static void Cleanup()
    {
        if (!TestDb.Available) return;
        foreach (var sql in CleanupStatements)
        {
            try { TestDb.Execute(sql); }
            catch { /* جدول غير موجود — نتخطاه */ }
        }
    }
}
