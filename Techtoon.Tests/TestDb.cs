using System;
using System.IO;
using Npgsql;
using Xunit;

namespace Techtoon.Tests;

/// <summary>
/// قاعدة بيانات اختبار معزولة — لا تُلمس بيانات الإنتاج إطلاقًا.
/// تُستخدم فقط عندما تكون TECHTOON_PG_CONNECTION مربوطة بقاعدة باسم techtoon_test.
/// </summary>
public static class TestDb
{
    /// <summary>سلسلة الاتصال من متغير البيئة، أو null إذا لم تُضبط.</summary>
    public static string? ConnectionString =>
        Environment.GetEnvironmentVariable("TECHTOON_PG_CONNECTION");

    /// <summary>هل الاتصال يشير إلى قاعدة اختبار معزولة؟</summary>
    public static bool IsIsolatedTestDatabase()
    {
        var cs = ConnectionString;
        if (string.IsNullOrWhiteSpace(cs)) return false;
        return cs.Contains("techtoon_test", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>ينشئ اتصالًا — يرمي استثناءً إذا لم تكن قاعدة الاختبار معزولة.</summary>
    public static NpgsqlConnection Open()
    {
        var cs = ConnectionString;
        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException("TECHTOON_PG_CONNECTION غير مضبوط — تُتخطّى اختبارات قاعدة البيانات.");
        if (!IsIsolatedTestDatabase())
            throw new InvalidOperationException("الاتصال لا يشير إلى techtoon_test — حماية من الكتابة على الإنتاج.");

        var conn = new NpgsqlConnection(cs);
        conn.Open();
        return conn;
    }

    /// <summary>هل يتوفّر الاتصال المعزول؟ (تُتخطّى الاختبارات لو لم يتوفّر).</summary>
    public static bool Available => IsIsolatedTestDatabase();

    /// <summary>ينفّذ استعلامًا ويُعيد أول قيمة.</summary>
    public static object? Scalar(string sql, params (string, object)[] parameters)
    {
        using var conn = Open();
        using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value);
        return cmd.ExecuteScalar();
    }

    /// <summary>ينفّذ أمرًا ويُعيد عدد الصفوف المتأثرة.</summary>
    public static int Execute(string sql, params (string, object)[] parameters)
    {
        using var conn = Open();
        using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value);
        return cmd.ExecuteNonQuery();
    }

    /// <summary>ينفّذ أمرًا داخل معاملة (تُرجَع بالمعاملة).</summary>
    public static int ExecuteInTransaction(NpgsqlConnection conn, NpgsqlTransaction tx, string sql, params (string, object)[] parameters)
    {
        using var cmd = new NpgsqlCommand(sql, conn, tx);
        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value);
        return cmd.ExecuteNonQuery();
    }
}
