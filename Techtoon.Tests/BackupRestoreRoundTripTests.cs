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
/// النسخ الاحتياطي والاستعادة — يجب أن تكون النسخة قابلة للاستعادة فعليًا.
/// <para><b>الخطأ الذي أصلحناه:</b> عمود <c>date</c> كان يُكتب بصيغة M/d/yyyy غير مقوَّسة،
/// فيقرأها PostgreSQL كقسمة أعداد (11/11/2026 → 11/11/2026 ≠ تاريخ) فتفشل الاستعادة،
/// كما كان عمود timestamp يحمل علامة AM/PM عربية لا يفهمها PostgreSQL.</para>
/// </summary>
[Collection("DatabaseIntegration")]
public class BackupRestoreRoundTripTests : IDisposable
{
    private readonly string _sandbox;

    public BackupRestoreRoundTripTests()
    {
        _sandbox = Path.Combine(Path.GetTempPath(), "techtoon_bk_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_sandbox);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_sandbox)) Directory.Delete(_sandbox, true); } catch { }
    }

    [Fact]
    public void Backup_File_Uses_Iso_Dates_Only()
    {
        if (!TestDb.Available) return;

        var file = BackupService.CreateBackup(_sandbox);
        Assert.True(File.Exists(file));

        var text = File.ReadAllText(file, Encoding.UTF8);

        // نتجاهل أسطر التعليقات (ترويسة النسخة تحمل تاريخًا نصيًا)
        var dataText = string.Join("\n", text.Split('\n').Where(l => !l.TrimStart().StartsWith("--")));

        // كل القيم القصورية يجب أن تكون بصيغة ISO — لا M/d/yyyy ولا علامة AM/PM
        var badQuoted = Regex.Matches(dataText, @"'(\d{1,2})/(\d{1,2})/(\d{4})[^']*'");
        Assert.True(badQuoted.Count == 0,
            "توجد تواريخ بصيغة غير ISO داخل النسخة: " + string.Join(", ", badQuoted.Select(m => m.Value).Take(5)));

        var badUnquoted = Regex.Matches(dataText, @"(?<![\w'])\b\d{1,2}/\d{1,2}/\d{4}\b(?!')");
        Assert.True(badUnquoted.Count == 0,
            "توجد تواريخ غير ISO غير مقوَّسة: " + string.Join(", ", badUnquoted.Select(m => m.Value).Take(5)));

        // القيم التي كُتبت يجب أن تكون قابلة للقراءة كـ ISO
        var isoDates = Regex.Matches(dataText, @"'(\d{4}-\d{2}-\d{2})'").Count;
        var isoStamps = Regex.Matches(dataText, @"'(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})'").Count;
        Assert.True(isoDates + isoStamps > 0, "لم يُكتب أي تاريخ بصيغة ISO في النسخة.");
    }

    [Fact]
    public void Backup_Create_Table_Has_No_Trailing_Comma()
    {
        if (!TestDb.Available) return;

        var file = BackupService.CreateBackup(_sandbox);
        var text = File.ReadAllText(file, Encoding.UTF8);
        var lines = text.Split('\n');

        // كل CREATE TABLE يجب أن ينتهي بعمود واحد لا تنتهي بفاصلة
        for (int i = 0; i < lines.Length; i++)
        {
            var t = lines[i].Trim();
            if (!t.StartsWith("CREATE TABLE", StringComparison.OrdinalIgnoreCase)) continue;
            // نبحث عن آخر تعريف عمود قبل ");"
            for (int j = i + 1; j < lines.Length && !lines[j].Trim().StartsWith(");"); j++)
            {
                var c = lines[j].TrimEnd();
                if (c.EndsWith(",")) continue;
                // آخر عمود بدون فاصلة — يجب ألا ينتهي بـ "),"
                Assert.False(c.EndsWith("),"), "CREATE TABLE ينتهي بفاصلة زائدة: " + c);
            }
        }
    }

    [Fact]
    public void Backup_Can_Be_Restored_Into_A_Fresh_Database()
    {
        if (!TestDb.Available) return;

        var cs = TestDb.ConnectionString!;
        var adminCs = System.Text.RegularExpressions.Regex.Replace(cs, @"(?i)(Database=)[^;]*", "$1postgres");
        var targetDb = "techtoon_restoretest_" + Guid.NewGuid().ToString("N").Substring(0, 8);

        // 1) لقطات ما قبل النسخ
        var tables = new List<string>();
        using (var conn = TestDb.Open())
        using (var cmd = new NpgsqlCommand(
            "SELECT table_name FROM information_schema.tables WHERE table_schema='public' AND table_type='BASE TABLE' ORDER BY table_name", conn))
        using (var r = cmd.ExecuteReader())
            while (r.Read()) tables.Add(r.GetString(0));

        var before = SnapshotCounts(tables);

        // 2) قاعدة جديدة + المخطط
        using (var admin = new NpgsqlConnection(adminCs))
        {
            admin.Open();
            using (var c = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{targetDb}\"", admin)) c.ExecuteNonQuery();
            using (var c = new NpgsqlCommand($"CREATE DATABASE \"{targetDb}\"", admin)) c.ExecuteNonQuery();
        }

        try
        {
            var schemaFile = Path.Combine(AppContext.BaseDirectory, "schema_full.sql");
            if (!File.Exists(schemaFile))
            {
                // نبحث عن السكربت في مجلد المستودع
                var dir = new DirectoryInfo(AppContext.BaseDirectory);
                while (dir != null && !File.Exists(schemaFile))
                {
                    var candidate = Path.Combine(dir.FullName, "deploy", "migrations", "schema_full.sql");
                    if (File.Exists(candidate)) { schemaFile = candidate; break; }
                    dir = dir.Parent;
                }
            }
            Assert.True(File.Exists(schemaFile), "schema_full.sql غير موجود");

            var targetCs = System.Text.RegularExpressions.Regex.Replace(cs, @"(?i)(Database=)[^;]*", "$1" + targetDb);
            using (var conn = new NpgsqlConnection(targetCs))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(File.ReadAllText(schemaFile, Encoding.UTF8), conn))
                {
                    cmd.CommandTimeout = 300;
                    cmd.ExecuteNonQuery();
                }
            }

            // 3) نسخة احتياطية ثم استعادة
            var backupFile = BackupService.CreateBackup(_sandbox);
            Assert.True(File.Exists(backupFile));

            var restored = BackupService.RestoreBackup(backupFile, targetDb);
            Assert.True(restored, "فشلت الاستعادة — راجع سجل النسخ الاحتياطي.");

            // 4) تطابق الصفوف
            var after = SnapshotCounts(tables, targetCs);
            var mismatch = new List<string>();
            foreach (var kv in before)
            {
                if (kv.Value == 0) continue;              // الجداول الفارغة لا تُعدّ
                after.TryGetValue(kv.Key, out var n);
                if (n != kv.Value) mismatch.Add($"{kv.Key}: قبل={kv.Value} بعد={n}");
            }
            Assert.True(mismatch.Count == 0,
                "الاستعادة لم تُعد نفس البيانات:\n  - " + string.Join("\n  - ", mismatch.Take(15)));
        }
        finally
        {
            try
            {
                using var admin = new NpgsqlConnection(adminCs);
                admin.Open();
                using var c = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{targetDb}\" WITH (FORCE)", admin);
                c.ExecuteNonQuery();
            }
            catch { }
        }
    }

    private static Dictionary<string, long> SnapshotCounts(List<string> tables, string? cs = null)
    {
        var result = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        using var conn = cs == null ? TestDb.Open() : new NpgsqlConnection(cs);
        if (cs != null) conn.Open();
        foreach (var t in tables)
        {
            try
            {
                using var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM \"{t}\"", conn);
                result[t] = Convert.ToInt64(cmd.ExecuteScalar());
            }
            catch { result[t] = -1; }
        }
        return result;
    }
}
