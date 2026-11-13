using Npgsql;

namespace Techtoon.Api.Services;

/// <summary>
/// أدوات مساعدة لمعاملات Npgsql.
/// </summary>
internal static class NpgsqlExtensions
{
    /// <summary>
    /// يضيف نسخًا جديدة من المعاملات إلى أمر.
    /// <para><b>لماذا؟</b> المعامل الواحد لا يمكن أن ينتمي إلى أمرين:
    /// <c>cmd.Parameters.Add(p)</c> لنفس <c>p</c> في استعلام COUNT ثم استعلام SELECT
    /// يرمي "The parameter already belongs to a collection" — فتفشل كل نقاط
    /// الاستعلام المُصفّح (تصفية بالنوع أو الطرف أو التاريخ) بخطأ 400.</para>
    /// </summary>
    public static void AddClones(this NpgsqlParameterCollection target, IEnumerable<NpgsqlParameter> source)
    {
        foreach (var p in source)
            target.Add(new NpgsqlParameter(p.ParameterName, p.Value));
    }
}
