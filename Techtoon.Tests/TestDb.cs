using System;
using System.IO;
using Npgsql;
using Xunit;

namespace Techtoon.Tests;

/// <summary>
/// قاعدة بيانات اختبار معزولة — لا تُلمس بيانات الإنتاج إطلاقًا.
/// تُستخدم فقط عندما تكون TECHTOON_PG_CONNECTION مربوطة بقاعدة باسم techtoon_test.
/// </summary>
public static class TestDb
{
    /// <summary>سلسلة الاتصال من متغير البيئة، أو null إذا لم تُضبط.</summary>
    public static string? ConnectionString =>
        Environment.GetEnvironmentVariable("TECHTOON_PG_CONNECTION");

    /// <summary>هل الاتصال يشير إلى قاعدة اختبار معزولة؟</summary>
    public static bool IsIsolatedTestDatabase()
    {
        var cs = ConnectionString;
        if (string.IsNullOrWhiteSpace(cs)) return false;
        return cs.Contains("techtoon_test", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>ينشئ اتصالًا — يرمي استثناءً إذا لم تكن قاعدة الاختبار معزولة.</summary>
    public static NpgsqlConnection Open()
    {
        var cs = ConnectionString;
        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException("TECHTOON_PG_CONNECTION غير مضبوط — تُتخطّى اختبارات قاعدة البيانات.");
        if (!IsIsolatedTestDatabase())
            throw new InvalidOperationException("الاتصال لا يشير إلى techtoon_test — حماية من الكتابة على الإنتاج.");

        var conn = new NpgsqlConnection(cs);
        conn.Open();
        return conn;
    }

    /// <summary>هل يتوفّر الاتصال المعزول؟ (تُتخطّى الاختبارات لو لم يتوفّر).</summary>
    public static bool Available => IsIsolatedTestDatabase();

    /// <summary>ينفّذ استعلامًا ويُعيد أول قيمة.</summary>
    public static object? Scalar(string sql, params (string, object)[] parameters)
    {
        using var conn = Open();
        using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value);
        return cmd.ExecuteScalar();
    }

    /// <summary>
    /// يحذف كل ما أنشأه الاختبار لهذا المعرّف الفريد (بالترتيب لاحترام المفاتيح الأجنبية).
    /// يمنع تراكم بيانات الاختبار في قاعدة الاختبار.
    /// </summary>
    public static void CleanupTestArtifacts(string unique)
    {
        var like = "%" + unique;
        var steps = new[]
        {
            "DELETE FROM kit_components WHERE kit_item_id IN (SELECT id FROM items WHERE barcode LIKE @u OR name LIKE @u) OR component_item_id IN (SELECT id FROM items WHERE barcode LIKE @u OR name LIKE @u)",
            "DELETE FROM stock_levels WHERE item_id IN (SELECT id FROM items WHERE barcode LIKE @u OR name LIKE @u)",
            "DELETE FROM recurring_invoice_lines WHERE template_id IN (SELECT id FROM recurring_invoice_templates WHERE name LIKE @u)",
            "DELETE FROM recurring_journal_executions WHERE template_id IN (SELECT id FROM recurring_journal_templates WHERE name LIKE @u)",
            "DELETE FROM recurring_journal_lines WHERE template_id IN (SELECT id FROM recurring_journal_templates WHERE name LIKE @u)",
            "DELETE FROM recurring_journal_templates WHERE name LIKE @u",
            "DELETE FROM recurring_invoice_templates WHERE name LIKE @u",
            "DELETE FROM journal_lines WHERE entry_id IN (SELECT id FROM journal_entries WHERE description LIKE @u)",
            "DELETE FROM journal_entries WHERE description LIKE @u",
            "DELETE FROM cost_center_transactions WHERE cost_center_id IN (SELECT id FROM cost_centers WHERE name LIKE @u)",
            "DELETE FROM cost_centers WHERE name LIKE @u",
            "DELETE FROM overdraft_transactions WHERE loan_id IN (SELECT id FROM loans WHERE loan_number LIKE @u)",
            "DELETE FROM loans WHERE loan_number LIKE @u",
            "DELETE FROM bad_debt_attachments WHERE writeoff_id IN (SELECT id FROM bad_debt_writeoffs WHERE writeoff_number LIKE @u)",
            "DELETE FROM bad_debt_writeoffs WHERE writeoff_number LIKE @u",
            "DELETE FROM inventory_movements WHERE item_id IN (SELECT id FROM items WHERE barcode LIKE @u OR name LIKE @u)",
            "DELETE FROM invoice_lines WHERE item_id IN (SELECT id FROM items WHERE barcode LIKE @u OR name LIKE @u)",
            "DELETE FROM items WHERE barcode LIKE @u OR name LIKE @u",
            "DELETE FROM parties WHERE name LIKE @u",
            "DELETE FROM warehouses WHERE code LIKE @u OR name LIKE @u",
            "DELETE FROM accounting_periods WHERE period_name LIKE @u",
            "DELETE FROM accounts WHERE code LIKE @u OR name LIKE @u",
        };

        foreach (var sql in steps)
        {
            try { Execute(sql, ("@u", like)); }
            catch { /* جدول غير موجود أو مرجع — نتخطاه */ }
        }
    }

    /// <summary>ينفّذ أمرًا ويُعيد عدد الصفوف المتأثرة.</summary>
    public static int Execute(string sql, params (string, object)[] parameters)
    {
        using var conn = Open();
        using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value);
        return cmd.ExecuteNonQuery();
    }

    /// <summary>ينفّذ أمرًا داخل معاملة (تُرجَع بالمعاملة).</summary>
    public static int ExecuteInTransaction(NpgsqlConnection conn, NpgsqlTransaction tx, string sql, params (string, object)[] parameters)
    {
        using var cmd = new NpgsqlCommand(sql, conn, tx);
        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value);
        return cmd.ExecuteNonQuery();
    }
}
