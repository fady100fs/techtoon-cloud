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

        public static string GetConnectionString()
        {
            try
            {
                if (!Directory.Exists(AppDataDir))
                    Directory.CreateDirectory(AppDataDir);

                // أول تشغيل: يجب توفير connection string عبر إعدادات المشغل
                // (env var TECHTOON_PG_CONNECTION) أو SaveConnectionString قبل أول تشغيل.
                if (!File.Exists(EncryptedFilePath))
                {
                    var envCs = Environment.GetEnvironmentVariable("TECHTOON_PG_CONNECTION");
                    if (!string.IsNullOrWhiteSpace(envCs))
                        SaveConnectionString(envCs);
                    else
                        throw new InvalidOperationException(
                            "No connection string configured. Set TECHTOON_PG_CONNECTION or call SaveConnectionString() before first run.");
                }

                return LoadConnectionString();
            }
            catch (Exception ex) when (!(ex is InvalidOperationException && ex.Message.Contains("No connection string")))
            {
                // لا fallback مكشوف — رمي الخطأ بوضوح
                throw;
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