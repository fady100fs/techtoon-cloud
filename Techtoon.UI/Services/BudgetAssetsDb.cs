using System;
using System.Collections.Generic;
using System.Globalization;
using Npgsql;

namespace Techtoon.UI.Services;

/// <summary>
/// أدوات قاعدة بيانات مشتركة لصفحتي "الميزانيات" (B1) و"الأصول الثابتة" (B3):
/// تحويل الأرقام إلى decimal (المال لا يستخدم double)، قراءة الأعمدة الفعلية للجدول،
/// وحلّ قيم الأعمدة من نوع enum دون افتراض محتوى مسبق.
/// </summary>
public static class BudgetAssetsDb
{
    public static string GetCS() => SecureConnectionManager.GetConnectionString();

    /// <summary>معرّف المستخدم الحالي (لعمود created_by) أو null.</summary>
    public static int? CurrentUserId()
    {
        try
        {
            var u = AuthService.CurrentUser;
            return u != null && u.Id > 0 ? u.Id : (int?)null;
        }
        catch { return null; }
    }

    /// <summary>يحوّل أي قيمة رقمية قادمة من Npgsql إلى decimal (numeric أو double precision).</summary>
    public static decimal ToDecimal(object? value)
    {
        if (value == null || value == DBNull.Value) return 0m;
        if (value is decimal dec) return dec;
        if (value is double dbl) return Convert.ToDecimal(Math.Round(dbl, 2, MidpointRounding.AwayFromZero));
        if (value is float flt) return Convert.ToDecimal(Math.Round(flt, 2, MidpointRounding.AwayFromZero));
        if (value is int i) return i;
        if (value is long l) return l;
        if (value is short s) return s;
        return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
    }

    public static int ToInt(object? value)
    {
        if (value == null || value == DBNull.Value) return 0;
        return Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    public static string ToText(object? value)
    {
        if (value == null || value == DBNull.Value) return "";
        return value.ToString() ?? "";
    }

    /// <summary>قائمة أعمدة الجدول الموجودة فعليًا في القاعدة (لتفادي أعمدة غير موجودة).</summary>
    public static List<string> GetColumns(NpgsqlConnection conn, string table)
    {
        var list = new List<string>();
        using (var cmd = new NpgsqlCommand(
            "SELECT column_name FROM information_schema.columns WHERE table_schema='public' AND table_name=@t ORDER BY ordinal_position", conn))
        {
            cmd.Parameters.AddWithValue("t", table);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(r.GetString(0));
        }
        if (list.Count == 0)
        {
            // الجدول قد يكون في مخطط غير public
            using var cmd = new NpgsqlCommand(
                "SELECT column_name FROM information_schema.columns WHERE table_name=@t ORDER BY ordinal_position", conn);
            cmd.Parameters.AddWithValue("t", table);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(r.GetString(0));
        }
        return list;
    }

    public static bool TableExists(NpgsqlConnection conn, string table)
    {
        using var cmd = new NpgsqlCommand(
            "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema='public' AND table_name=@t)", conn);
        cmd.Parameters.AddWithValue("t", table);
        var res = cmd.ExecuteScalar();
        return res is bool b && b;
    }

    /// <summary>
    /// يحلّ القيمة التي يجب كتابتها في عمود من نوع enum (مثل depreciation_method):
    /// 1) قيمة موجودة فعليًا في الجدول، 2) أول تسمية في نوع الـ enum، 3) قيمة افتراضية.
    /// </summary>
    public static string ResolveEnumLike(NpgsqlConnection conn, string table, string column, string fallback)
    {
        try
        {
            using (var c = new NpgsqlCommand($"SELECT {column}::text FROM {table} WHERE {column} IS NOT NULL LIMIT 1", conn))
            {
                var v = c.ExecuteScalar() as string;
                if (!string.IsNullOrWhiteSpace(v)) return v;
            }
            using (var c = new NpgsqlCommand(@"
                SELECT e.enumlabel
                FROM information_schema.columns c
                JOIN pg_type t ON t.typname = c.udt_name
                JOIN pg_enum e ON e.enumtypid = t.oid
                WHERE c.table_name=@t AND c.column_name=@col
                ORDER BY e.enumsortorder LIMIT 1", conn))
            {
                c.Parameters.AddWithValue("t", table);
                c.Parameters.AddWithValue("col", column);
                var v = c.ExecuteScalar() as string;
                if (!string.IsNullOrWhiteSpace(v)) return v;
            }
        }
        catch { /* القيمة الافتراضية تكفي */ }
        return fallback;
    }

    /// <summary>
    /// يبني جملة INSERT مع الأعمدة الموجودة فعليًا في الجدول فقط
    /// (لتفادي أعمدة غير موجودة في القاعدة مثل depreciation_method).
    /// rawExpressions: أعمدة تُكتب بتعبير SQL مباشر (مثل NOW()).
    /// </summary>
    public static string BuildInsertSql(NpgsqlConnection conn, string table,
        List<KeyValuePair<string, object?>> values,
        Dictionary<string, string>? rawExpressions,
        out List<KeyValuePair<string, object?>> parameters)
    {
        var columns = GetColumns(conn, table);
        var names = new List<string>();
        var exprs = new List<string>();
        parameters = new List<KeyValuePair<string, object?>>();
        rawExpressions ??= new Dictionary<string, string>();

        foreach (var v in values)
        {
            if (!columns.Contains(v.Key)) continue;
            names.Add(v.Key);
            if (rawExpressions.TryGetValue(v.Key, out var raw)) exprs.Add(raw);
            else { exprs.Add("@" + v.Key); parameters.Add(v); }
        }
        return "INSERT INTO " + table + " (" + string.Join(", ", names) + ") VALUES (" + string.Join(", ", exprs) + ")";
    }

    /// <summary>يبني جملة UPDATE مع الأعمدة الموجودة فعليًا في الجدول فقط.</summary>
    public static string BuildUpdateSql(NpgsqlConnection conn, string table,
        List<KeyValuePair<string, object?>> values,
        Dictionary<string, string>? rawExpressions,
        string whereColumn, object whereValue,
        out List<KeyValuePair<string, object?>> parameters)
    {
        var columns = GetColumns(conn, table);
        var sets = new List<string>();
        parameters = new List<KeyValuePair<string, object?>>();
        rawExpressions ??= new Dictionary<string, string>();

        foreach (var v in values)
        {
            if (!columns.Contains(v.Key)) continue;
            if (rawExpressions.TryGetValue(v.Key, out var raw)) sets.Add(v.Key + " = " + raw);
            else { sets.Add(v.Key + " = @" + v.Key); parameters.Add(v); }
        }
        return "UPDATE " + table + " SET " + string.Join(", ", sets) + " WHERE " + whereColumn + " = @__where";
    }
}
