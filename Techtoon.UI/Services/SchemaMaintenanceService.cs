using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace Techtoon.UI.Services;

/// <summary>
/// صيانة المخطط: مزامنة sequences PostgreSQL مع أعلى id موجود في كل جدول.
///
/// <para><b>لماذا؟</b> عند نقل/استيراد بيانات (أو زرع يدوي بمعرّفات صريحة) تبقى الـ sequences
/// متأخرة عن البيانات، فيفشل أي INSERT جديد بخطأ <c>duplicate key value violates unique constraint</c>.
/// هذا فحص إصلاحي غير مدمّر: <c>setval</c> فقط — لا يحذف ولا يعدّل بيانات.</para>
///
/// <para><b>الأداء:</b> يُنفَّذ في استعلامين فقط (لا استعلام لكل جدول) — لا يُبطئ إقلاع البرنامج.</para>
/// </summary>
public static class SchemaMaintenanceService
{
    /// <summary>نتيجة المزامنة (تُستخدم في السجل).</summary>
    public class SyncResult
    {
        public int Fixed { get; set; }
        public List<string> Details { get; } = new();
        public string Message => Fixed == 0
            ? "All sequences are in sync."
            : $"Synced {Fixed} sequence(s).";
    }

    /// <summary>
    /// يبحث عن كل الـ sequences المتأخرة ويضبطها على MAX(id).
    /// آمن للتشغيل المتكرر (idempotent).
    /// </summary>
    public static async Task<SyncResult> SyncSequencesAsync(string connectionString)
    {
        var result = new SyncResult();
        try
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();

            // 1) كل الـ sequences مع آخر قيمة والجدول المالك (استعلام واحد)
            var sequences = new List<(string seq, string table, long last)>();
            const string seqQuery = @"
                SELECT s.sequencename,
                       t.relname AS table_name,
                       s.last_value
                FROM pg_sequences s
                JOIN pg_depend d
                  ON d.objid = (SELECT c.oid FROM pg_class c
                                JOIN pg_namespace n ON n.oid = c.relnamespace
                                WHERE c.relname = s.sequencename AND n.nspname = s.schemaname)
                 AND d.classid = 'pg_class'::regclass
                 AND d.refclassid = 'pg_class'::regclass
                JOIN pg_class t ON t.oid = d.refobjid AND t.relkind = 'r'
                JOIN pg_namespace tn ON tn.oid = t.relnamespace
                WHERE s.schemaname = 'public'
                  AND tn.nspname = 'public'
                ORDER BY s.sequencename";

            await using (var cmd = new NpgsqlCommand(seqQuery, conn))
            await using (var r = await cmd.ExecuteReaderAsync())
            {
                while (await r.ReadAsync())
                {
                    var seq = r.GetString(0);
                    var tbl = r.GetString(1);
                    var last = r.IsDBNull(2) ? 0L : Convert.ToInt64(r.GetValue(2));
                    sequences.Add((seq, tbl, last));
                }
            }

            if (sequences.Count == 0) return result;

            // 2) أعلى id لكل جدول — استعلام واحد مركّب (لا استعلام لكل جدول)
            var maxIds = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            var sb = new StringBuilder();
            for (int i = 0; i < sequences.Count; i++)
            {
                if (i > 0) sb.Append(" UNION ALL ");
                sb.Append("SELECT ").Append(i).Append(" AS k, COALESCE(MAX(id),0) AS m FROM public.\"")
                  .Append(sequences[i].table.Replace("\"", "\"\"")).Append('"');
            }

            await using (var cmd = new NpgsqlCommand(sb.ToString(), conn))
            await using (var r = await cmd.ExecuteReaderAsync())
            {
                while (await r.ReadAsync())
                {
                    var k = Convert.ToInt32(r.GetValue(0));
                    maxIds[sequences[k].table] = Convert.ToInt64(r.GetValue(1));
                }
            }

            // 3) إصلاح المتأخر فقط
            var fixes = new List<(string seq, long max)>();
            foreach (var (seq, table, last) in sequences)
            {
                if (!maxIds.TryGetValue(table, out var max)) continue;
                if (max > last) fixes.Add((seq, max));
            }

            if (fixes.Count == 0) return result;

            await using (var tx = await conn.BeginTransactionAsync())
            {
                foreach (var (seq, max) in fixes)
                {
                    await using var fix = new NpgsqlCommand(
                        $"SELECT setval('public.\"{seq}\"', @v, true)", conn, tx);
                    fix.Parameters.AddWithValue("@v", max);
                    await fix.ExecuteScalarAsync();
                }
                await tx.CommitAsync();
            }

            foreach (var (seq, max) in fixes)
            {
                result.Fixed++;
                result.Details.Add($"{seq} -> {max}");
            }
            App.Log("SCHEMA: synced " + result.Fixed + " sequence(s) - " + string.Join(", ", result.Details));
        }
        catch (Exception ex)
        {
            App.Log("SCHEMA sync failed: " + ex.Message);
        }
        return result;
    }
}
