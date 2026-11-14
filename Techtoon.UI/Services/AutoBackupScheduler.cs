using System;
using System.IO;
using System.Linq;
using Npgsql;

namespace Techtoon.UI.Services
{
    /// <summary>
    /// يفحص تاريخ آخر نسخة احتياطية، وينشئ نسخة جديدة تلقائيًا كل 24 ساعة.
    /// يحتفظ بآخر 7 نسخ فقط.
    /// </summary>
    public static class AutoBackupScheduler
    {
        // مجلد النسخ: بجانب البرنامج إن كان قابلًا للكتابة، وإلا في بيانات المستخدم
        // (تثبيت Program Files = مجلد للقراءة فقط ⇒ كانت النسخة تفشل بـ Access denied).
        private static readonly string BackupDir = AppPaths.BackupDir;

        private const int MaxBackupsToKeep = 7;
        private const int BackupIntervalHours = 24;

        /// <summary>
        /// يفحص الحاجة لنسخة احتياطية جديدة. يُستدعى عند بدء التطبيق.
        /// </summary>
        /// <returns>true لو تم إنشاء نسخة جديدة</returns>
        public static bool CheckAndBackupIfNeeded(string connectionString)
        {
            try
            {
                if (!Directory.Exists(BackupDir))
                    Directory.CreateDirectory(BackupDir);

                var lastBackup = GetLastBackupTime();
                var now = DateTime.Now;

                if (lastBackup.HasValue && (now - lastBackup.Value).TotalHours < BackupIntervalHours)
                    return false; // لا حاجة لنسخة جديدة

                // إنشاء نسخة جديدة
                CreateBackup(connectionString);
                CleanOldBackups();
                App.Log("Auto-backup created at " + now.ToString("yyyy-MM-dd HH:mm"));
                return true;
            }
            catch (Exception ex)
            {
                App.Log("Auto-backup failed: " + ex.Message);
                return false;
            }
        }

        private static DateTime? GetLastBackupTime()
        {
            var files = Directory.GetFiles(BackupDir, "backup_*.sql");
            if (files.Length == 0) return null;

            var latest = files
                .Select(f => ParseBackupFilename(Path.GetFileName(f)))
                .Where(d => d.HasValue)
                .Max();
            return latest;
        }

        private static DateTime? ParseBackupFilename(string filename)
        {
            // backup_2026-10-27_14-30-00.sql
            try
            {
                var parts = filename.Replace("backup_", "").Replace(".sql", "").Split('_');
                if (parts.Length != 2) return null;
                var date = DateTime.ParseExact(parts[0] + " " + parts[1].Replace("-", ":"),
                    "yyyy-MM-dd HH:mm:ss", null);
                return date;
            }
            catch { return null; }
        }

        public static void CreateBackup(string connectionString)
        {
            // نستخدم نفس مُنشئ النسخة الموثوق (BackupService) الذي يكتب:
            //  - CREATE TABLE صحيح (بلا عمود مكرر وبلا فاصلة زائدة)
            //  - تواريخ بصيغة ISO (القاعدة تقرأ 11/10/2026 كقسمة أعداد في تفشل)
            //  - بلا عبارات DELETE (الاستعادة تُعيد البيانات كاملة أو لا شيء)
            // النسخة القديمة (بصيغة M/d/yyyy مع علامة AM/PM) كانت غير قابلة للاستعادة إطلاقًا.
            var backupDir = Path.GetDirectoryName(BackupDir);
            if (string.IsNullOrEmpty(backupDir)) backupDir = BackupDir;

            // نحتفظ باسم الملف المتوقع للنسخ التلقائية: backup_yyyy-MM-dd_HH-mm-ss.sql
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string backupFile = Path.Combine(BackupDir, "backup_" + timestamp + ".sql");

            try
            {
                if (!Directory.Exists(BackupDir)) Directory.CreateDirectory(BackupDir);

                var created = BackupService.CreateBackup(BackupDir);
                if (!string.IsNullOrEmpty(created) && File.Exists(created))
                {
                    // نُعيد تسمية الملف بالشكل الذي يتوقّعه تنظيف النسخ التلقائي
                    try
                    {
                        if (File.Exists(backupFile)) File.Delete(backupFile);
                        File.Move(created, backupFile);
                    }
                    catch { /* نترك الاسم الأصلي إن فشل النقل */ }
                }
            }
            catch (Exception ex)
            {
                App.Log("Auto-backup failed: " + ex.Message);
            }
        }

        private static void CleanOldBackups()
        {
            var files = Directory.GetFiles(BackupDir, "backup_*.sql")
                .OrderByDescending(f => f)
                .Skip(MaxBackupsToKeep);
            foreach (var f in files)
            {
                try { File.Delete(f); } catch { }
            }
        }
    }
}