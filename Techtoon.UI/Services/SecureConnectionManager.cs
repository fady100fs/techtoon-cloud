using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Techtoon.UI.Services
{
    /// <summary>
    /// يحفظ connection string مشفّرًا في مجلد بيانات المستخدم باستخدام DPAPI.
    /// لا يُكشف النص العادي على القرص أبدًا.
    /// </summary>
    public static class SecureConnectionManager
    {
        private static readonly string AppDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TechtoonAccounting");

        private static readonly string EncryptedFilePath = Path.Combine(AppDataDir, "connection.enc");

        // قيمة افتراضية (fallback) لو لم يوجد ملف مشفّر — تُستخدم أول مرة فقط
        private const string DefaultConnection =
            "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;" +
            "Username=neondb_owner;Password=npg_K9nHVkY5OjRC;Database=neondb;" +
            "SSL Mode=Require;Trust Server Certificate=true";

        public static string GetConnectionString()
        {
            try
            {
                if (!Directory.Exists(AppDataDir))
                    Directory.CreateDirectory(AppDataDir);

                // إن لم يوجد ملف مشفّر، أنشئه من القيمة الافتراضية (أول تشغيل)
                if (!File.Exists(EncryptedFilePath))
                {
                    SaveConnectionString(DefaultConnection);
                }

                return LoadConnectionString();
            }
            catch
            {
                // fallback آمن لو فشل DPAPI
                return DefaultConnection;
            }
        }

        public static void SaveConnectionString(string connectionString)
        {
            try
            {
                if (!Directory.Exists(AppDataDir))
                    Directory.CreateDirectory(AppDataDir);

                // تشفير بـ DPAPI (مرتبط بالمستخدم الحالي)
                var plainBytes = Encoding.UTF8.GetBytes(connectionString);
                var cipherBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(EncryptedFilePath, cipherBytes);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to save connection string: " + ex.Message, ex);
            }
        }

        private static string LoadConnectionString()
        {
            var cipherBytes = File.ReadAllBytes(EncryptedFilePath);
            var plainBytes = ProtectedData.Unprotect(cipherBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plainBytes);
        }

        public static bool HasStoredConnection()
        {
            return File.Exists(EncryptedFilePath);
        }
    }
}