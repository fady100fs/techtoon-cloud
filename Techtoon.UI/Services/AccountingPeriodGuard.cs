using System;
using Npgsql;

namespace Techtoon.UI.Services
{
    /// <summary>
    /// A4: حارس الفترات المحاسبية — يمنع الترحيل (posting) داخل فترة محاسبية مغلقة.
    ///
    /// الاستخدام:
    ///   if (AccountingPeriodGuard.IsPostingBlocked(date, out string periodName))
    ///   {
    ///       // امنع الحفظ واعرض: لا يمكن الحفظ: الفترة المحاسبية '{periodName}' مغلقة
    ///   }
    ///
    /// ملاحظة: إن لم تكن هناك أي فترات محاسبية مُعرَّفة في النظام فكل التواريخ مسموحة.
    /// </summary>
    public static class AccountingPeriodGuard
    {
        /// <summary>
        /// هل يوجد فترة محاسبية مغلقة تغطي التاريخ المُعطى؟
        /// </summary>
        public static bool IsPostingBlocked(DateTime date)
        {
            return TryGetBlockingPeriod(date, out _);
        }

        /// <summary>
        /// هل يوجد فترة محاسبية مغلقة تغطي التاريخ المُعطى؟ مع إرجاع اسم الفترة المانعة.
        /// </summary>
        public static bool IsPostingBlocked(DateTime date, out string? periodName)
        {
            return TryGetBlockingPeriod(date, out periodName);
        }

        /// <summary>
        /// يُرجع اسم أول فترة مغلقة تغطي التاريخ (أو null إن لم توجد).
        /// </summary>
        public static string? GetBlockingPeriodName(DateTime date)
        {
            TryGetBlockingPeriod(date, out string? name);
            return name;
        }

        /// <summary>
        /// رسالة الخطأ العربية الموحّدة عند منع الترحيل.
        /// </summary>
        public static string BuildBlockedMessage(string periodName)
        {
            return "لا يمكن الحفظ: الفترة المحاسبية '" + (periodName ?? "") + "' مغلقة";
        }

        /// <summary>
        /// البحث عن فترة مغلقة تغطي التاريخ. المقارنة تتم على مستوى اليوم (date)
        /// حتى تعمل مع أعمدة timestamp أو date على حد سواء.
        /// </summary>
        public static bool TryGetBlockingPeriod(DateTime date, out string? periodName)
        {
            periodName = null;
            try
            {
                using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
                conn.Open();

                using var cmd = new NpgsqlCommand(
                    @"SELECT period_name, status
                      FROM accounting_periods
                      WHERE start_date::date <= @d::date
                        AND end_date::date   >= @d::date
                      ORDER BY start_date DESC, id DESC
                      LIMIT 50", conn);
                // Npgsql يحوّل DateTime من النوع Local إلى timestamptz، ما قد يُزيح التاريخ.
                // نثبّت النوع على Unspecified ليُعامل كـ timestamp مجرد (نفس نوع العمود).
                cmd.Parameters.AddWithValue("d", NpgsqlTypes.NpgsqlDbType.Timestamp,
                    DateTime.SpecifyKind(date, DateTimeKind.Unspecified));

                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    string name = r.IsDBNull(0) ? "" : r.GetValue(0).ToString();
                    string status = r.IsDBNull(1) ? "" : r.GetValue(1).ToString();
                    if (AccountingPeriodStatus.IsClosed(status))
                    {
                        periodName = name;
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                // تعذّر الوصول للقاعدة → لا نمنع العمل (كما لو لم توجد فترات).
                return false;
            }
        }
    }
}
