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
        private static readonly string BackupDir = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Backups");

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
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var backupFile = Path.Combine(BackupDir, "backup_" + timestamp + ".sql");

            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();

            using var writer = new StreamWriter(backupFile, false, System.Text.Encoding.UTF8);
            writer.WriteLine("-- Techtoon Accounting Backup");
            writer.WriteLine("-- Created: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            writer.WriteLine("-- Database: " + conn.Database);
            writer.WriteLine();

            // 1) استعلم كل الجداول
            var tables = new System.Collections.Generic.List<string>();
            using (var cmd = new NpgsqlCommand(
                "SELECT table_name FROM information_schema.tables WHERE table_schema='public' ORDER BY table_name", conn))
            using (var r = cmd.ExecuteReader())
                while (r.Read()) tables.Add(r.GetString(0));

            // 2) لكل جدول: صدّر CREATE TABLE + INSERT
            foreach (var table in tables)
            {
                writer.WriteLine("-- Table: " + table);
                writer.WriteLine("DELETE FROM " + table + ";");

                using var cmd = new NpgsqlCommand("SELECT * FROM " + table, conn);
                using var reader = cmd.ExecuteReader();
                var columns = new System.Collections.Generic.List<string>();
                for (int i = 0; i < reader.FieldCount; i++)
                    columns.Add(reader.GetName(i));

                while (reader.Read())
                {
                    var values = new System.Collections.Generic.List<string>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var val = reader.IsDBNull(i) ? "NULL" : FormatValue(reader.GetValue(i));
                        values.Add(val);
                    }
                    writer.WriteLine("INSERT INTO " + table + " (" + string.Join(", ", columns) + ") VALUES (" + string.Join(", ", values) + ");");
                }
                writer.WriteLine();
            }

            writer.WriteLine("-- End of backup");
        }

        private static string FormatValue(object value)
        {
            if (value == null) return "NULL";
            var type = value.GetType();
            if (type == typeof(string) || type == typeof(DateTime))
                return "'" + value.ToString().Replace("'", "''") + "'";
            if (type == typeof(bool))
                return (bool)value ? "TRUE" : "FALSE";
            return value.ToString().Replace("'", "''");
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