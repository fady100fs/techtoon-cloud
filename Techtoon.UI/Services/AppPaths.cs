using System;
using System.IO;

namespace Techtoon.UI.Services;

/// <summary>
/// مسارات قابلة للكتابة لبيانات المستخدم.
/// <para><b>لماذا؟</b> عند التثبيت في <c>Program Files</c> يكون المجلد للقراءة فقط
/// (لا صلاحيات مدير) ⇒ كان سجل التشغيل والنسخ الاحتياطي يفشلان بـ «Access denied».
/// لذلك نستخدم مجلد بيانات المستخدم، ونُبقي بجانب البرنامج إن كان قابلًا للكتابة
/// (تثبيت portable / نسخة على فلاشة).</para>
/// </summary>
public static class AppPaths
{
    /// <summary>مجلد بيانات التطبيق (قابل للكتابة دائمًا).</summary>
    public static string DataDir
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TechtoonAccounting");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    /// <summary>
    /// مجلد السجل: بجانب البرنامج إن كان قابلًا للكتابة، وإلا في بيانات المستخدم.
    /// <para>السجل يبقى باسم <c>techtoon.log</c> في نفس المجلد الذي اعتاد عليه المستخدم.</para>
    /// </summary>
    public static string LogDir => ResolveWritableDir(AppContext.BaseDirectory, DataDir);

    /// <summary>مجلد النسخ الاحتياطية: بجانب البرنامج إن كان قابلًا للكتابة، وإلا في بيانات المستخدم.</summary>
    public static string BackupDir => ResolveWritableDir(Path.Combine(AppContext.BaseDirectory, "Backups"),
                                                       Path.Combine(DataDir, "Backups"));

    /// <summary>مسار ملف السجل.</summary>
    public static string LogFile => Path.Combine(LogDir, "techtoon.log");

    private static string WritableOrFallback(string preferred)
        => ResolveWritableDir(preferred, Path.Combine(DataDir, Path.GetFileName(preferred)));

    /// <summary>
    /// يعيد المجلد إن كان قابلًا للكتابة، وإلا المجلد البديل.
    /// <para>مُتاح للاختبار: مرّر مسارًا غير قابل للكتابة للتأكد من التحوّل.</para>
    /// </summary>
    public static string ResolveWritableDir(string preferred, string fallback)
    {
        try
        {
            Directory.CreateDirectory(preferred);
            var probe = Path.Combine(preferred, ".write-probe.tmp");
            File.WriteAllText(probe, "probe");
            File.Delete(probe);
            return preferred;
        }
        catch
        {
            Directory.CreateDirectory(fallback);
            return fallback;
        }
    }
}
