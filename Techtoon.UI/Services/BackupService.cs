using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Npgsql;

namespace Techtoon.UI.Services
{
    public static class BackupService
    {
        public static string GetCS() { return Techtoon.UI.Services.SecureConnectionManager.GetConnectionString(); }

        // ============================================================
        //  إنشاء نسخة احتياطية كاملة (Schema + Data) كملف SQL
        // ============================================================
        public static string CreateBackup(string backupDir)
        {
            try
            {
                if (!Directory.Exists(backupDir))
                    Directory.CreateDirectory(backupDir);

                string fileName = $"techtoon_backup_{DateTime.Now:yyyyMMdd_HHmmss}.sql";
                string filePath = Path.Combine(backupDir, fileName);

                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
                    {
                        writer.WriteLine($"-- Techtoon Backup: {DateTime.Now}");
                        writer.WriteLine("-- Host: Neon PostgreSQL");
                        writer.WriteLine("-- ======================================\n");

                        // احصل على كل الجداول
                        var tables = new System.Collections.Generic.List<string>();
                        using (var cmd = new NpgsqlCommand("SELECT table_name FROM information_schema.tables WHERE table_schema='public' AND table_type='BASE TABLE' ORDER BY table_name", conn))
                        using (var r = cmd.ExecuteReader())
                            while (r.Read()) tables.Add(r.GetString(0));

                        foreach (var table in tables)
                        {
                            writer.WriteLine($"\n-- Table: {table}");
                            
                            // 1) CREATE TABLE
                            writer.WriteLine($"CREATE TABLE IF NOT EXISTS \"{table}\" (");
                            var columns = new System.Collections.Generic.List<(string name, string type)>();
                            using (var cmd = new NpgsqlCommand($"SELECT column_name, data_type, is_nullable, column_default FROM information_schema.columns WHERE table_schema='public' AND table_name=@t ORDER BY ordinal_position", conn))
                            {
                                cmd.Parameters.AddWithValue("t", table);
                                using (var r = cmd.ExecuteReader())
                                    while (r.Read())
                                    {
                                        string colName = r.GetString(0);
                                        string dataType = r.GetString(1);
                                        string nullable = r.GetString(2) == "YES" ? "NULL" : "NOT NULL";
                                        string def = r.IsDBNull(3) ? "" : $"DEFAULT {r.GetString(3)}";
                                        columns.Add((colName, dataType));
                                        writer.WriteLine($"  \"{colName}\" {dataType} {nullable} {def},");
                                    }
                            }
                            writer.WriteLine(");");

                            // 2) COPY data
                            var colNames = string.Join(", ", columns.Select(c => $"\"{c.name}\""));
                            using (var cmd = new NpgsqlCommand($"SELECT {colNames} FROM \"{table}\"", conn))
                            using (var r = cmd.ExecuteReader())
                            {
                                while (r.Read())
                                {
                                    var values = new System.Collections.Generic.List<string>();
                                    for (int i = 0; i < columns.Count; i++)
                                    {
                                        if (r.IsDBNull(i)) values.Add("NULL");
                                        else
                                        {
                                            object val = r.GetValue(i);
                                            if (val is string s) values.Add($"'{s.Replace("'", "''")}'");
                                            else if (val is DateTime dt) values.Add($"'{dt:yyyy-MM-dd HH:mm:ss}'");
                                            else if (val is bool b) values.Add(b ? "TRUE" : "FALSE");
                                            else if (val is byte[] bytes) values.Add($"'\\x{Convert.ToHexString(bytes)}'");
                                            else values.Add(val.ToString());
                                        }
                                    }
                                    writer.WriteLine($"INSERT INTO \"{table}\" ({colNames}) VALUES ({string.Join(", ", values)});");
                                }
                            }
                            writer.WriteLine($"-- {table}: data exported");
                        }
                    }
                }

                // تسجيل نجاح العملية
                LogBackupResult(filePath, true, "Backup created successfully");
                return filePath;
            }
            catch (Exception ex)
            {
                LogBackupResult("", false, ex.Message);
                throw;
            }
        }

        // ============================================================
        //  الاستعادة من ملف SQL (على قاعدة بيانات منفصلة)
        // ============================================================
        public static bool RestoreBackup(string sqlFilePath, string targetDatabase)
        {
            try
            {
                if (!File.Exists(sqlFilePath))
                    throw new FileNotFoundException("Backup file not found", sqlFilePath);

                string targetCS = GetCS().Replace("Database=neondb", $"Database={targetDatabase}");
                string sqlContent = File.ReadAllText(sqlFilePath, System.Text.Encoding.UTF8);

                using (var conn = new NpgsqlConnection(targetCS))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(sqlContent, conn))
                    {
                        cmd.CommandTimeout = 300; // 5 دقائق
                        cmd.ExecuteNonQuery();
                    }
                }

                LogBackupResult(sqlFilePath, true, $"Restored to {targetDatabase}");
                return true;
            }
            catch (Exception ex)
            {
                LogBackupResult(sqlFilePath, false, $"Restore failed: {ex.Message}");
                return false;
            }
        }

        // ============================================================
        //  التحقق من سلامة ملف النسخة
        // ============================================================
        public static bool VerifyBackup(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return false;
                var content = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                if (!content.Contains("CREATE TABLE")) return false;
                if (!content.Contains("INSERT INTO")) return false;
                return content.Length > 100; // حجم أدنى
            }
            catch { return false; }
        }

        // ============================================================
        //  تسجيل نتائج النسخ
        // ============================================================
        private static void LogBackupResult(string filePath, bool success, string message)
        {
            try
            {
                string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TechtoonBackupLogs");
                Directory.CreateDirectory(logDir);
                string logFile = Path.Combine(logDir, "backup_log.txt");
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {(success ? "SUCCESS" : "FAILED")} | File: {Path.GetFileName(filePath)} | {message}\n";
                File.AppendAllText(logFile, logEntry, System.Text.Encoding.UTF8);
            }
            catch { }
        }

        // ============================================================
        //  سياسة الاحتفاظ (Retention) - حذف النسخ الأقدم من N أيام
        // ============================================================
        public static int CleanOldBackups(string backupDir, int retentionDays)
        {
            int deleted = 0;
            try
            {
                var cutoff = DateTime.Now.AddDays(-retentionDays);
                foreach (var file in Directory.GetFiles(backupDir, "techtoon_backup_*.sql"))
                {
                    if (File.GetCreationTime(file) < cutoff)
                    {
                        File.Delete(file);
                        deleted++;
                    }
                }
            }
            catch { }
            return deleted;
        }
    }
}
