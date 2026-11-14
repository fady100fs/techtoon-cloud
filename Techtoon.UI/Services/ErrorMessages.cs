using System;
using System.Linq;

namespace Techtoon.UI.Services;

/// <summary>
/// صياغة رسائل الخطأ للمستخدم.
/// <para><b>لماذا؟</b> EF Core يُخفي السبب الحقيقي داخل <c>InnerException</c>
/// فيظهر للمستخدم: «An error occurred while saving the entity changes» — بلا أي فائدة.
/// كما أن أخطاء PostgreSQL (المفاتيح الأجنبية) تأتي بالإنجليزية.</para>
/// </summary>
public static class ErrorMessages
{
    /// <summary>رسالة خطأ مفهومة بالعربية.</summary>
    public static string Describe(Exception ex, string? context = null)
    {
        if (ex == null) return "حدث خطأ غير معروف.";

        // أخطاء قاعدة البيانات المعروفة → شرح عربي
        var root = Root(ex);
        var msg = root?.Message ?? "";

        if (msg.Contains("violates foreign key constraint", StringComparison.OrdinalIgnoreCase))
        {
            var detail = ExtractConstraint(msg);
            var extra = string.IsNullOrEmpty(detail) ? "" : $"\n\nالسجل المرتبط: {detail}";
            return (context ?? "لا يمكن الحذف")
                 + ": توجد سجلات مرتبطة بهذا السجل ولا يمكن حذفه قبل حذفها." + extra;
        }
        if (msg.Contains("violates unique constraint", StringComparison.OrdinalIgnoreCase))
            return (context ?? "لا يمكن الحفظ") + ": القيمة موجودة مسبقًا (تكرار).";
        if (msg.Contains("duplicate key value", StringComparison.OrdinalIgnoreCase))
            return (context ?? "لا يمكن الحفظ") + ": القيمة موجودة مسبقًا (تكرار).";
        if (msg.Contains("null value in column", StringComparison.OrdinalIgnoreCase))
            return (context ?? "لا يمكن الحفظ") + ": أحد الحقول الإلزامية فارغ.";
        if (msg.Contains("cannot access the file", StringComparison.OrdinalIgnoreCase))
            return "الملف مفتوح في برنامج آخر — أغلقه ثم أعد المحاولة.";
        if (msg.Contains("connection", StringComparison.OrdinalIgnoreCase) &&
            (msg.Contains("refused") || msg.Contains("timed out") || msg.Contains("terminated")))
            return "تعذّر الاتصال بقاعدة البيانات — تحقق من الشبكة.";

        // لا نكرر نفس الرسالة مرتين
        if (ex.InnerException != null && ex.Message != root?.Message)
            return ex.Message + "\n\n" + root.Message;

        return ex.Message;
    }

    /// <summary>أعمق استثناء في السلسلة.</summary>
    public static Exception Root(Exception ex)
    {
        var cur = ex;
        int guard = 0;
        while (cur.InnerException != null && guard++ < 10) cur = cur.InnerException;
        return cur;
    }

    /// <summary>اسم القيد المُخالف من رسالة PostgreSQL.</summary>
    private static string ExtractConstraint(string message)
    {
        var m = System.Text.RegularExpressions.Regex.Match(message,
            @"on constraint ""(?<c>[^""]+)""");
        if (!m.Success) return "";
        var c = m.Groups["c"].Value;
        // "parties_account_id_fkey" -> "parties"
        var table = c.Split('_')[0];
        var friendly = table switch
        {
            "invoices" => "فاتورة",
            "payments" => "سند قبض/صرف",
            "subscriptions" => "اشتراك",
            "invoice_payments" => "دفعة فاتورة",
            "invoice_lines" => "سطر فاتورة",
            "inventory_movements" => "حركة مخزون",
            "journal_entries" => "قيد محاسبي",
            "parties" => "عميل/مورد",
            "accounts" => "حساب",
            "items" => "صنف",
            _ => table
        };
        return $"{friendly} ({c})";
    }
}
