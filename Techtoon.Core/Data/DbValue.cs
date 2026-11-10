namespace Techtoon.Core.Data;

/// <summary>
/// P5: مساعد قراءة قيم التاريخ من Npgsql.
///Npgsql 10 يعيد أعمدة النوع <c>date</c> بصيغة <see cref="DateOnly"/> (و<see cref="timestamp"/> بصيغة DateTime)،
/// فأي تحويل مباشر إلى DateTime يفشل. هذا المساعد يوحّد القراءة.
/// </summary>
public static class DbValue
{
    /// <summary>يحوّل القيمة إلى DateTime (يدعم DateTime / DateOnly / نص).</summary>
    public static DateTime AsDateTime(object? value) => value switch
    {
        DateTime dt => dt,
        DateOnly d => d.ToDateTime(TimeOnly.MinValue),
        string s when DateTime.TryParse(s, out var parsed) => parsed,
        _ => DateTime.MinValue
    };

    /// <summary>يحوّل القيمة إلى DateTime؟ (null عند NULL).</summary>
    public static DateTime? AsNullableDateTime(object? value)
    {
        if (value == null || value == DBNull.Value) return null;
        var dt = AsDateTime(value);
        return dt == DateTime.MinValue ? (DateTime?)null : dt;
    }
}
