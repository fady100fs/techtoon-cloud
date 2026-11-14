using System;
using System.IO;
using Techtoon.UI.Services;
using Xunit;

namespace Techtoon.Tests;

/// <summary>
/// مسارات التشغيل والنسخ الاحتياطي — يجب أن تكون قابلة للكتابة
/// حتى عند التثبيت في Program Files (مجلد للقراءة فقط).
/// </summary>
public class AppPathsTests : IDisposable
{
    private readonly string _temp;

    public AppPathsTests()
    {
        _temp = Path.Combine(Path.GetTempPath(), "apppaths_test_" + Guid.NewGuid().ToString("N").Substring(0, 8));
        Directory.CreateDirectory(_temp);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_temp)) Directory.Delete(_temp, true); } catch { }
    }

    [Fact]
    public void Writable_Folder_Is_Used_As_Is()
    {
        var preferred = Path.Combine(_temp, "Backups");
        var fallback = Path.Combine(_temp, "fallback");
        var resolved = AppPaths.ResolveWritableDir(preferred, fallback);
        Assert.Equal(preferred, resolved);
        Assert.True(Directory.Exists(resolved));
    }

    [Fact]
    public void ReadOnly_Location_Falls_Back_To_A_Writable_Folder()
    {
        // قرص غير موجود ⇒ لا يمكن الكتابة فيه
        var preferred = @"Z:\definitely-not-writable\Backups";
        var fallback = Path.Combine(_temp, "fallback");
        var resolved = AppPaths.ResolveWritableDir(preferred, fallback);
        Assert.Equal(fallback, resolved);
        Assert.True(Directory.Exists(resolved));
        // ويمكن الكتابة فيه فعلًا
        var probe = Path.Combine(resolved, ".probe.tmp");
        File.WriteAllText(probe, "x");
        File.Delete(probe);
    }

    [Fact]
    public void Log_And_Backup_Paths_Are_Writable()
    {
        // في بيئة الاختبار المجلد قابل للكتابة — نتحقق أن المسارات themselves صالحة
        var logFile = AppPaths.LogFile;
        Assert.False(string.IsNullOrWhiteSpace(logFile));
        Assert.True(Directory.Exists(Path.GetDirectoryName(logFile)!));

        var backupDir = AppPaths.BackupDir;
        Assert.False(string.IsNullOrWhiteSpace(backupDir));
        Directory.CreateDirectory(backupDir);
        var probe = Path.Combine(backupDir, ".probe.tmp");
        File.WriteAllText(probe, "x");
        File.Delete(probe);
    }
}
