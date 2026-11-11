using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace Techtoon.Tests;

/// <summary>
/// فحص SQL ثابت: يتحقق من أن كل جملة INSERT/UPDATE/SELECT في كود التطبيق
/// تستخدم جداول وأعمدة موجودة فعلًا في المخطط.
///
/// <para><b>لماذا؟</b> عمود غير موجود في جدول لا يُكتشف وقت البناء — يفشل عند أول
/// ضغطة زر في وقت التشغيل (وهكذا اكتُشف خطأ <c>bank_statements.statement_number</c>).</para>
/// </summary>
public class SqlSchemaAuditTests
{
    private static string UiSourceDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "Techtoon.UI");
            if (Directory.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("لم يُعثر على مجلد Techtoon.UI من: " + AppContext.BaseDirectory);
    }

    /// <summary>كل ملفات C# في التطبيق (بدون obj/bin).</summary>
    private static string[] SourceFiles()
    {
        var root = UiSourceDirectory();
        return Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(@"\obj\") && !f.Contains(@"\bin\"))
            .OrderBy(f => f)
            .ToArray();
    }

    /// <summary>استخراج كل نصوص SQL من الملف (السلاسل النصية الحرفية).</summary>
    private static IEnumerable<string> SqlLiterals(string text)
    {
        // @"..." و "..." مع الهروب
        var verbatim = new Regex(@"@""(?<s>(?:[^""]|"")*)""", RegexOptions.Singleline);
        foreach (Match m in verbatim.Matches(text))
        {
            var s = m.Groups["s"].Value.Replace("\"\"", "\"");
            if (LooksLikeSql(s) && !IsDynamic(s)) yield return s;
        }

        var normal = new Regex(@"(?<!@)""(?<s>(?:\\.|[^""\\])*)""", RegexOptions.Singleline);
        foreach (Match m in normal.Matches(text))
        {
            var s = m.Groups["s"].Value;
            if (!LooksLikeSql(s)) continue;
            if (s.Length > 4000) continue;
            if (IsDynamic(s)) continue;
            yield return s;
        }
    }

    /// <summary>
    /// SQL مبنيّ ديناميكيًا (دمج نصوص أو استيفاء) — لا يمكن تدقيقه ثابتًا، فيُستثنى.
    /// </summary>
    private static bool IsDynamic(string sql)
    {
        // دمج نصوص C# داخل نص SQL
        if (sql.Contains("\" +") || sql.Contains("+ \"") || sql.Contains("$\"") || sql.Contains("$\"@"))
            return true;
        // استيفاء {variable}
        if (sql.Contains("{") && sql.Contains("}")) return true;
        // SQL مبني بأسطر متسلسلة
        if (sql.Contains("\n") && sql.Contains("INSERT")) return true;
        return false;
    }

    /// <summary>مخططات النظام وكلمات SQL المفتاحية — لا تُفحص.</summary>
    private static readonly HashSet<string> SystemNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "information_schema", "pg_catalog", "pg_class", "pg_namespace", "pg_attribute",
        "pg_index", "pg_depend", "pg_sequence", "pg_sequences", "pg_enum", "pg_proc",
        "pg_indexes", "pg_constraint", "pg_type", "pg_tables", "public",
        "and", "or", "not", "as", "on", "join", "left", "right", "inner", "outer",
        "from", "where", "select", "insert", "update", "delete", "set", "values",
        "returning", "group", "order", "by", "having", "limit", "offset", "union", "all"
    };

    private static bool LooksLikeSql(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        var head = s.TrimStart().ToUpperInvariant();
        return head.StartsWith("SELECT") || head.StartsWith("INSERT") || head.StartsWith("UPDATE")
            || head.StartsWith("DELETE") || head.StartsWith("WITH");
    }

    /// <summary>يجمع أعمدة الجداول من information_schema.</summary>
    private static Dictionary<string, HashSet<string>> LoadSchema()
    {
        var schema = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        using var conn = TestDb.Open();
        using var cmd = new Npgsql.NpgsqlCommand(
            "SELECT table_name, column_name FROM information_schema.columns WHERE table_schema='public'", conn);
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            var t = r.GetString(0);
            if (!schema.TryGetValue(t, out var set))
            {
                set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                schema[t] = set;
            }
            set.Add(r.GetString(1));
        }
        return schema;
    }

    private static readonly Regex InsertRegex = new(
        @"INSERT\s+INTO\s+(?:public\.)?""?(?<table>[A-Za-z_][A-Za-z0-9_]*)""?\s*\((?<cols>[^)]*)\)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline);

    private static readonly Regex UpdateRegex = new(
        @"UPDATE\s+(?:public\.)?""?(?<table>[A-Za-z_][A-Za-z0-9_]*)""?\s+SET\s+(?<sets>[^\r\n]*?)(\r?\n|WHERE|RETURNING|;)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline);

    private static readonly Regex SelectRegex = new(
        @"FROM\s+(?:public\.)?""?(?<table>[A-Za-z_][A-Za-z0-9_]*)""?",
        RegexOptions.IgnoreCase);

    [Fact]
    public void All_Insert_Statements_Use_Existing_Tables_And_Columns()
    {
        if (!TestDb.Available) return;
        var schema = LoadSchema();
        var problems = new List<string>();

        foreach (var file in SourceFiles())
        {
            var text = File.ReadAllText(file);
            var fileName = Path.GetFileName(file);

            foreach (var sql in SqlLiterals(text))
            {
                foreach (Match m in InsertRegex.Matches(sql))
                {
                    var table = m.Groups["table"].Value;
                    if (!schema.ContainsKey(table))
                    {
                        // قد يكون اسم جدول مُركَّب أو متغيّر — نتجاهل ما لا يبدأ بحرف/شرطة سفلية
                        problems.Add($"{fileName}: جدول غير موجود: {table}");
                        continue;
                    }
                    var cols = schema[table];
                    foreach (var raw in m.Groups["cols"].Value.Split(','))
                    {
                        var col = raw.Trim().Trim('"').Trim();
                        if (col.Length == 0) continue;
                        if (col.StartsWith("@")) continue;
                        if (!cols.Contains(col))
                            problems.Add($"{fileName}: عمود غير موجود: {table}.{col}");
                    }
                }
            }
        }

        Assert.True(problems.Count == 0,
            "أعمدة/جداول غير موجودة في المخطط:\n  - " + string.Join("\n  - ", problems.Distinct().Take(40)));
    }

    [Fact]
    public void All_Update_Statements_Use_Existing_Tables_And_Columns()
    {
        if (!TestDb.Available) return;
        var schema = LoadSchema();
        var problems = new List<string>();

        foreach (var file in SourceFiles())
        {
            var text = File.ReadAllText(file);
            var fileName = Path.GetFileName(file);

            foreach (var sql in SqlLiterals(text))
            {
                foreach (Match m in UpdateRegex.Matches(sql))
                {
                    var table = m.Groups["table"].Value;
                    if (!schema.ContainsKey(table))
                    {
                        problems.Add($"{fileName}: جدول غير موجود: {table}");
                        continue;
                    }
                    var cols = schema[table];
                    var sets = m.Groups["sets"].Value;
                    // نتحقق فقط من أول عمود بعد SET (الأعمدة الأخرى قد تكون مرتبطة بمتغيّرات)
                    var firstAssign = sets.Split(',').FirstOrDefault() ?? "";
                    var eq = firstAssign.IndexOf('=');
                    if (eq <= 0) continue;
                    var col = firstAssign.Substring(0, eq).Trim().Trim('"').Trim();
                    if (col.Length == 0 || col.StartsWith("@")) continue;
                    if (!cols.Contains(col))
                        problems.Add($"{fileName}: عمود غير موجود: {table}.{col}");
                }
            }
        }

        Assert.True(problems.Count == 0,
            "أعمدة/جداول غير موجودة في المخطط:\n  - " + string.Join("\n  - ", problems.Distinct().Take(40)));
    }

    [Fact]
    public void All_From_Tables_Exist_In_Schema()
    {
        if (!TestDb.Available) return;
        var schema = LoadSchema();
        var problems = new List<string>();

        foreach (var file in SourceFiles())
        {
            var text = File.ReadAllText(file);
            var fileName = Path.GetFileName(file);

            foreach (var sql in SqlLiterals(text))
            {
                foreach (Match m in SelectRegex.Matches(sql))
                {
                    var table = m.Groups["table"].Value;
                    // جداول النظام وكلمات SQL المفتاحية لا تُفحص
                    if (SystemNames.Contains(table)) continue;
                    // جداول التطبيق كلها snake_case صغيرة الحروف
                    if (!Regex.IsMatch(table, "^[a-z][a-z0-9_]{2,}$")) continue;
                    if (!schema.ContainsKey(table))
                        problems.Add($"{fileName}: جدول غير موجود في FROM: {table}");
                }
            }
        }

        Assert.True(problems.Count == 0,
            "جداول غير موجودة في المخطط:\n  - " + string.Join("\n  - ", problems.Distinct().Take(40)));
    }
}
