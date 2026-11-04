#nullable disable
using System;
using System.IO;
using System.Text;
using System.Windows;

namespace Techtoon.UI
{
    public static class ErrorGuard
    {
        private static int _count = 0;
        private static int _shown = 0;
        private static string _lastKey = "";

        public static void Attach()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Report("AppDomain", e.ExceptionObject as Exception);
            };
        }

        public static void Report(string source, Exception ex)
        {
            if (ex == null) { return; }
            _count = System.Threading.Interlocked.Increment(ref _count);
            try
            {
                string key = ex.GetType().FullName + " | " + ex.Message;
                string file = WriteLog(source, ex);
                if (_shown < 2 || key != _lastKey)
                {
                    _shown++;
                    _lastKey = key;
                    MessageBox.Show(ex.GetType().Name + "\n\n" + ex.Message + "\n\nالتقرير:\n" + file, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch { }
            if (_count >= 3) { Environment.Exit(1); }
        }

        private static string WriteLog(string source, Exception ex)
        {
            string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
            string body = BuildReport(source, ex);
            string saved = "";
            string[] dirs = new string[] { Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_logs"), @"D:\PROG\Accounting Prog\Techtoon Accounting C+\_logs" };
            foreach (string d in dirs)
            {
                try { Directory.CreateDirectory(d); string f = Path.Combine(d, "crash-" + stamp + ".txt"); File.WriteAllText(f, body, new UTF8Encoding(false)); if (saved == "") { saved = f; } } catch { }
            }
            return saved == "" ? "(فشل الحفظ)" : saved;
        }

        private static string BuildReport(string source, Exception ex)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Techtoon Crash Report ===");
            sb.AppendLine("Time   : " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("Source : " + source);
            Exception cur = ex; int depth = 0;
            while (cur != null && depth < 10)
            {
                sb.AppendLine();
                sb.AppendLine("[" + depth + "] " + cur.GetType().FullName);
                sb.AppendLine("Message: " + cur.Message);
                sb.AppendLine("Stack  :");
                sb.AppendLine(cur.StackTrace ?? "(empty)");
                cur = cur.InnerException; depth++;
            }
            sb.AppendLine();
            sb.AppendLine("=== Full ToString ===");
            sb.AppendLine(ex.ToString());
            return sb.ToString();
        }
    }
}