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

                        // ترتيب حسب الاعتماديات: الجداول الأصل قبل التي تشير إليها
                        // (وإلا فشلت الاستعادة بمخالفة المفتاح الأجنبي)
                        tables = SortByDependencies(conn, tables);

                        foreach (var table in tables)
                        {
                            writer.WriteLine($"\n-- Table: {table}");
                            
                            // 1) CREATE TABLE
                            writer.WriteLine($"CREATE TABLE IF NOT EXISTS \"{table}\" (");
                            var columns = new System.Collections.Generic.List<(string name, string type)>();
                            var columnDefs = new System.Collections.Generic.List<string>();
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
                                        // نجمع التعريفات ثم نكتبها بفاصلة بين كل عمود وآخر — بلا فاصلة زائدة
                                        // (الفاصلة الزائدة كانت تُفشل الاستعادة: syntax error at or near ")")
                                        columnDefs.Add($"  \"{colName}\" {dataType} {nullable} {def}");
                                    }
                            }
                            foreach (var def in columnDefs) writer.WriteLine(def + ",");
                            if (columnDefs.Count > 0)
                            {
                                // إزالة الفاصلة من آخر عمود
                                var last = columnDefs.Count - 1;
                                writer.WriteLine(columnDefs[last]);
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
                                            // Npgsql 10 يُعيد عمود date كـ DateOnly و timestamp كـ DateTime.
                                            // يجب كتابة التواريخ بصيغة ISO مقبولة في PostgreSQL،
                                            // وإلا تحولت إلى قسمة أعداد (11/11/2026) فتعجز النسخة عن الاستعادة.
                                            if (val is DateTime dt) values.Add($"'{dt:yyyy-MM-dd HH:mm:ss}'");
                                            else if (val is DateOnly d) values.Add($"'{d:yyyy-MM-dd}'");
                                            else if (val is DateTimeOffset dto) values.Add($"'{dto.UtcDateTime:yyyy-MM-dd HH:mm:ss}'");
                                            else if (val is TimeOnly t) values.Add($"'{t:HH:mm:ss}'");
                                            else if (val is string s) values.Add($"'{s.Replace("'", "''")}'");
                                            else if (val is bool b) values.Add(b ? "TRUE" : "FALSE");
                                            else if (val is byte[] bytes) values.Add($"'\\x{Convert.ToHexString(bytes)}'");
                                            else if (val is Guid g) values.Add($"'{g}'");
                                            else if (val is decimal or double or float or int or long or short)
                                                values.Add(Convert.ToString(val, System.Globalization.CultureInfo.InvariantCulture) ?? "0");
                                            else values.Add($"'{Convert.ToString(val, System.Globalization.CultureInfo.InvariantCulture)?.Replace("'", "''")}'");
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

        /// <summary>
        /// يضبط كل sequence على أعلى id في جدوله — ضروري بعد استعادة نسخة
        /// (الصفوف تُدرج بأرقام صريحة فتتأخر الـ sequences ويشل الإدراج التالي).
        /// </summary>
        private static void ResyncSequences(NpgsqlConnection conn)
        {
            try
            {
                using var cmd = new NpgsqlCommand(@"
                    SELECT c.relname AS seq_name, split_part(c.relname, '_id_seq', 1) AS table_name
                    FROM pg_class c
                    JOIN pg_namespace n ON n.oid = c.relnamespace
                    WHERE c.relkind = 'S' AND n.nspname = 'public' AND c.relname LIKE '%_id_seq'", conn);
                var pairs = new List<(string seq, string table)>();
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        pairs.Add((r.GetString(0), r.GetString(1)));

                foreach (var (seq, table) in pairs)
                {
                    try
                    {
                        using var maxCmd = new NpgsqlCommand($"SELECT COALESCE(MAX(id),0) FROM \"{table}\"", conn);
                        var max = Convert.ToInt64(maxCmd.ExecuteScalar());
                        if (max <= 0) continue;
                        using var setCmd = new NpgsqlCommand($"SELECT setval('public.\"{seq}\"', @v, true)", conn);
                        setCmd.Parameters.AddWithValue("@v", max);
                        setCmd.ExecuteScalar();
                    }
                    catch { /* جدول غير موجود — نتخطاه */ }
                }
            }
            catch { }
        }

        /// <summary>
        /// يرتّب الجداول بحيث تأتي الجداول الأصل (المُشار إليها بمفتاح أجنبي) قبل الجداول التي تشير إليها.
        /// ضروري لأن ملف النسخة يُنفَّذ بالترتيب.
        /// </summary>
        private static List<string> SortByDependencies(NpgsqlConnection conn, List<string> tables)
        {
            var deps = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var t in tables) deps[t] = new List<string>();

            using (var cmd = new NpgsqlCommand(@"
                SELECT tc.table_name AS child, ccu.table_name AS parent
                FROM information_schema.table_constraints tc
                JOIN information_schema.constraint_column_usage ccu
                  ON ccu.constraint_name = tc.constraint_name
                 AND ccu.constraint_schema = tc.constraint_schema
                WHERE tc.constraint_type = 'FOREIGN KEY'
                  AND tc.table_schema = 'public'", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    var child = r.GetString(0);
                    var parent = r.GetString(1);
                    if (child == parent) continue;
                    if (!deps.ContainsKey(child) || !deps.ContainsKey(parent)) continue;
                    if (!deps[child].Contains(parent)) deps[child].Add(parent);
                }
            }

            var result = new List<string>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var inStack = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void Visit(string table)
            {
                if (visited.Contains(table)) return;
                if (!inStack.Add(table)) return;   // دورة — نتخطاها
                foreach (var parent in deps[table]) Visit(parent);
                inStack.Remove(table);
                visited.Add(table);
                result.Add(table);
            }

            foreach (var t in tables) Visit(t);
            return result;
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
                if (string.IsNullOrWhiteSpace(targetDatabase))
                    throw new ArgumentException("Target database is required.");

                // نستبدل اسم قاعدة البيانات في سلسلة الاتصال بأي اسم — لا يجب أن يكون "neondb"
                var current = GetCS();
                string targetCS = System.Text.RegularExpressions.Regex.Replace(
                    current, @"(?i)(Database=)[^;]*", "$1" + targetDatabase);
                if (targetCS == current && !current.Contains("Database=" + targetDatabase, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("تعذّر تحديد قاعدة البيانات المستهدفة في سلسلة الاتصال.");

                string sqlContent = File.ReadAllText(sqlFilePath, System.Text.Encoding.UTF8);

                using (var conn = new NpgsqlConnection(targetCS))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(sqlContent, conn))
                    {
                        cmd.CommandTimeout = 300; // 5 دقائق
                        cmd.ExecuteNonQuery();
                    }

                    // بعد الاستعادة يجب أن تُضبط الـ sequences على أعلى id،
                    // وإلا فشل أول إدراج بمفتاح مكرر (لأن الصفوف أُدرجت بأرقام صريحة).
                    ResyncSequences(conn);
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
