using System;
using Npgsql;

namespace Techtoon.UI.Services
{
    /// <summary>
    /// P4: قيم افتراضية موحّدة من قاعدة البيانات (مصدر واحد للحقيقة).
    /// currency_id في جدول payments إلزامي (NOT NULL) — كل عملية إدخال تمرّ من هنا.
    /// </summary>
    public static class DbDefaults
    {
        public static int GetDefaultCurrencyId(NpgsqlConnection conn)
        {
            try
            {
                using var cmd = new NpgsqlCommand("SELECT id FROM currencies ORDER BY id LIMIT 1", conn);
                var v = cmd.ExecuteScalar();
                return (v == null || v == DBNull.Value) ? 1 : Convert.ToInt32(v);
            }
            catch { return 1; }
        }
    }
}
