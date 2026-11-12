using System;
using System.IO;
using System.Text;
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

        // حماية حاسمة: لا يُسمح للاختبارات بالكتابة على قاعدة العمل إطلاقًا.
        // كل الاختبارات يجب أن تعمل على النسخة المنفصلة (techtoon_test_ci).
        if (!IsolationSucceeded)
            throw new InvalidOperationException(
                "قاعدة الاختبار المنفصلة (" + IsolatedDatabaseName + ") غير جاهزة — " +
                "يُمنع تشغيل الاختبارات على قاعدة العمل لحماية بيانات المستخدم.");

        var conn = new NpgsqlConnection(cs);
        conn.Open();
        return conn;
    }

    /// <summary>هل يتوفّر الاتصال المعزول؟ (تُتخطّى الاختبارات لو لم يتوفّر).</summary>
    public static bool Available => IsIsolatedTestDatabase() && IsolationSucceeded;

    /// <summary>هل نجحت قاعدة الاختبار المنفصلة؟ (null = لم تُحاول بعد).</summary>
    public static bool? IsolationResult => _isolationResult;

    /// <summary>هل نجحت العزل؟</summary>
    public static bool IsolationSucceeded => _isolationResult == true;

    private static bool? _isolationResult;

    /// <summary>
    /// يضمن وجود قاعدة اختبار منفصلة — ويشل بوضوح إن تعذّر.
    /// <para><b>مهم:</b> إن فشل العزل يجب أن تُوقف الاختبارات، لا أن تعمل على قاعدة العمل
    /// (فقد تمسح بيانات المستخدم).</para>
    /// </summary>
    public static void EnsureIsolatedDatabase()
    {
        if (_isolationResult != null) return;
        _isolationResult = SwitchToIsolatedDatabase();
    }

    /// <summary>
    /// اسم قاعدة الاختبار المنفصلة — نسخة من قاعدة العمل تُستخدم لكل الاختبارات.
    /// </summary>
    public const string IsolatedDatabaseName = "techtoon_test_ci";

    /// <summary>
    /// ينشئ قاعدة اختبار منفصلة (نسخة من قاعدة العمل) ويعيد توجيه الاختبارات إليها.
    ///
    /// <para><b>لماذا؟</b> الاختبارات تكتب وتحذف في قاعدة البيانات. لو اشتركت مع
    /// قاعدة العمل <c>techtoon_test</c> لأمكن لاختبار فاشل أن يمسح بيانات المستخدم.
    /// لذلك تعمل كل الاختبارات على نسخة مستقلة، وتبقى بيانات المستخدم كما هي.</para>
    ///
    /// <para><b>الطريقة:</b> لا يمكن استخدام <c>CREATE DATABASE ... TEMPLATE</c>
    /// عبر pgbouncer (المُجمِّع يبقي اتصالًا دائمًا) — لذا نأخذ نسخة SQL من قاعدة
    /// العمل (نفس مُنشئ النسخ الاحتياطي) ونستعيدها في القاعدة الجديدة.</para>
    /// </summary>
    /// <returns>true إذا نجح التحويل (وإلا تبقى الاختبارات على القاعدة الأصلية).</returns>
    public static bool SwitchToIsolatedDatabase()
    {
        var cs = ConnectionString;
        if (string.IsNullOrWhiteSpace(cs)) return false;

        string dumpDir = null;
        try
        {
            var srcDb = System.Text.RegularExpressions.Regex.Match(cs, @"(?i)Database=([^;]+)").Groups[1].Value.Trim();
            if (string.IsNullOrWhiteSpace(srcDb)) return false;

            var adminCs = System.Text.RegularExpressions.Regex.Replace(cs, @"(?i)(Database=)[^;]*", "$1postgres");
            var testCs = System.Text.RegularExpressions.Regex.Replace(cs, @"(?i)(Database=)[^;]*", "$1" + IsolatedDatabaseName);

            // 1) نسخة SQL من قاعدة العمل
            dumpDir = Path.Combine(Path.GetTempPath(), "techtoon_seed_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dumpDir);
            var dumpFile = Techtoon.UI.Services.BackupService.CreateBackup(dumpDir);
            if (string.IsNullOrEmpty(dumpFile) || !File.Exists(dumpFile))
                throw new InvalidOperationException("لم يُنشئ مُنشئ النسخة ملفًا.");

            // 2) القاعدة الجديدة
            using (var admin = new NpgsqlConnection(adminCs))
            {
                admin.Open();
                using (var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{IsolatedDatabaseName}\" WITH (FORCE)", admin))
                    drop.ExecuteNonQuery();
                using (var create = new NpgsqlCommand($"CREATE DATABASE \"{IsolatedDatabaseName}\"", admin))
                    create.ExecuteNonQuery();
            }

            // 3) نستعيد النسخة في القاعدة الجديدة
            var dumpText = File.ReadAllText(dumpFile, Encoding.UTF8);
            using (var conn = new NpgsqlConnection(testCs))
            {
                conn.Open();
                using var cmd = new NpgsqlCommand(dumpText, conn);
                cmd.CommandTimeout = 300;
                cmd.ExecuteNonQuery();
            }

            // 4) نُعيد توجيه الاختبارات داخل هذه العملية فقط
            Environment.SetEnvironmentVariable("TECHTOON_PG_CONNECTION", testCs);
            Console.WriteLine("[TestDb] الاختبارات تعمل الآن على قاعدة منفصلة: " + IsolatedDatabaseName);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("[TestDb] تعذّر إنشاء قاعدة الاختبار المنفصلة: " + ex.Message);
            return false;
        }
        finally
        {
            try { if (dumpDir != null && Directory.Exists(dumpDir)) Directory.Delete(dumpDir, true); } catch { }
        }
    }

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
