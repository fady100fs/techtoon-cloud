using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using Techtoon.Core.Data;

namespace Techtoon.UI.Services;

/// <summary>
/// محرّك تنفيذ القيود الدورية: ينشئ القيد المحاسبي فعليًا ويسجّل سطر التنفيذ
/// في <c>recurring_journal_executions</c> (سجل التنفيذ + حالة + مبالغ + أخطاء).
/// </summary>
public class RecurringJournalService
{
    private readonly string _cs;

    public RecurringJournalService(string connectionString) { _cs = connectionString; }

    public static RecurringJournalService Create() =>
        new RecurringJournalService(SecureConnectionManager.GetConnectionString());

    /// <summary>سطر قالب.</summary>
    public class TemplateLine
    {
        public int LineNumber { get; set; }
        public int AccountId { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string? Description { get; set; }
        public int? CostCenterId { get; set; }
    }

    /// <summary>نتيجة تنفيذ قالب.</summary>
    public class ExecutionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int TemplateId { get; set; }
        public int ExecutionId { get; set; }
        public int? JournalEntryId { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public DateTime ExecutionDate { get; set; }
    }

    /// <summary>يقرأ سطور قالب.</summary>
    public async Task<List<TemplateLine>> GetTemplateLinesAsync(int templateId)
    {
        var lines = new List<TemplateLine>();
        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(@"SELECT line_number, account_id, COALESCE(debit,0), COALESCE(credit,0),
            description, cost_center_id FROM recurring_journal_lines WHERE template_id=@t ORDER BY line_number", conn);
        cmd.Parameters.AddWithValue("@t", templateId);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            lines.Add(new TemplateLine
            {
                LineNumber = r.GetInt32(0),
                AccountId = r.GetInt32(1),
                Debit = Convert.ToDecimal(r.GetValue(2)),
                Credit = Convert.ToDecimal(r.GetValue(3)),
                Description = r.IsDBNull(4) ? null : r.GetString(4),
                CostCenterId = r.IsDBNull(5) ? null : r.GetInt32(5)
            });
        }
        return lines;
    }

    /// <summary>
    /// ينفّذ القالب الدوري: ينشئ القيد + سطوره + سطر التنفيذ + يحدّث حالة القالب — في معاملة واحدة.
    /// إن فشل شيء: يُسجَّل سطر تنفيذ بحالة 'failed' مع نص الخطأ (ولا يُنشأ قيد ناقص).
    /// </summary>
    public async Task<ExecutionResult> ExecuteAsync(int templateId, DateTime executionDate, int executedBy = 1)
    {
        var result = new ExecutionResult { TemplateId = templateId, ExecutionDate = executionDate.Date };

        // القالب
        string name = "";
        string frequency = "";
        DateTime? nextRun = null;
        await using (var conn = new NpgsqlConnection(_cs))
        {
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(
                "SELECT name, frequency, next_run_date, is_active FROM recurring_journal_templates WHERE id=@t", conn);
            cmd.Parameters.AddWithValue("@t", templateId);
            await using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync())
            {
                result.Success = false;
                result.Message = "القالب غير موجود.";
                return result;
            }
            name = r.GetString(0);
            frequency = r.GetString(1);
            nextRun = r.IsDBNull(2) ? (DateTime?)null : DbValue.AsDateTime(r.GetValue(2));
            if (r.GetBoolean(3) == false)
            {
                result.Success = false;
                result.Message = "القالب غير مُفعّل.";
                return result;
            }
        }

        var lines = await GetTemplateLinesAsync(templateId);
        if (lines.Count == 0)
        {
            await RecordFailureAsync(templateId, executionDate, nextRun, "القالب بلا سطور — لا يوجد ما يُنفَّذ.");
            result.Success = false;
            result.Message = "القالب بلا سطور — لا يوجد ما يُنفَّذ.";
            return result;
        }

        var totalDebit = lines.Sum(l => l.Debit);
        var totalCredit = lines.Sum(l => l.Credit);
        if (Math.Abs(totalDebit - totalCredit) > 0.005m)
        {
            var msg = $"القيد غير متوازن: مدين {totalDebit:N2} ≠ دائن {totalCredit:N2}.";
            await RecordFailureAsync(templateId, executionDate, nextRun, msg);
            result.Success = false;
            result.Message = msg;
            return result;
        }

        await using var conn2 = new NpgsqlConnection(_cs);
        await conn2.OpenAsync();
        await using var tx = await conn2.BeginTransactionAsync();
        try
        {
            int entryId;
            await using (var ins = new NpgsqlCommand(@"INSERT INTO journal_entries
                (date, description, reference_type, reference_id, created_by, period_id, cash_basis_posted)
                VALUES (CAST(@d AS timestamp), @desc, 'recurring_journal', @tid, @by, NULL, FALSE) RETURNING id", conn2, tx))
            {
                ins.Parameters.AddWithValue("@d", executionDate);
                ins.Parameters.AddWithValue("@desc", "قيد دوري: " + name);
                ins.Parameters.AddWithValue("@tid", templateId);
                ins.Parameters.AddWithValue("@by", executedBy);
                entryId = Convert.ToInt32(await ins.ExecuteScalarAsync());
            }

            foreach (var l in lines)
            {
                await using var ins = new NpgsqlCommand(@"INSERT INTO journal_lines
                    (entry_id, account_id, debit, credit) VALUES (@e, @a, @d, @c)", conn2, tx);
                ins.Parameters.AddWithValue("@e", entryId);
                ins.Parameters.AddWithValue("@a", l.AccountId);
                ins.Parameters.AddWithValue("@d", l.Debit);
                ins.Parameters.AddWithValue("@c", l.Credit);
                await ins.ExecuteNonQueryAsync();
            }

            // سطر التنفيذ (السجل الذي كان فارغًا)
            int executionId;
            await using (var ins = new NpgsqlCommand(@"INSERT INTO recurring_journal_executions
                (template_id, execution_date, planned_run_date, journal_entry_id, status,
                 total_debit, total_credit, error_message, executed_at, executed_by)
                VALUES (@tid, CAST(@ed AS date), CAST(@pd AS date), @jid, 'success',
                        @td, @tc, NULL, NOW(), @by) RETURNING id", conn2, tx))
            {
                ins.Parameters.AddWithValue("@tid", templateId);
                ins.Parameters.AddWithValue("@ed", executionDate);
                ins.Parameters.AddWithValue("@pd", (object?)nextRun ?? executionDate);
                ins.Parameters.AddWithValue("@jid", entryId);
                ins.Parameters.AddWithValue("@td", totalDebit);
                ins.Parameters.AddWithValue("@tc", totalCredit);
                ins.Parameters.AddWithValue("@by", executedBy);
                executionId = Convert.ToInt32(await ins.ExecuteScalarAsync());
            }

            // تحديث حالة القالب + تاريخ التشغيل التالي
            var next = ComputeNextRun(executionDate, frequency);
            await using (var upd = new NpgsqlCommand(@"UPDATE recurring_journal_templates
                SET last_run_date=NOW(), last_run_status='success', last_run_error=NULL,
                    next_run_date=@next, updated_at=NOW(), updated_by=@by
                WHERE id=@id", conn2, tx))
            {
                upd.Parameters.AddWithValue("@next", next);
                upd.Parameters.AddWithValue("@by", executedBy);
                upd.Parameters.AddWithValue("@id", templateId);
                await upd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();

            result.Success = true;
            result.ExecutionId = executionId;
            result.JournalEntryId = entryId;
            result.TotalDebit = totalDebit;
            result.TotalCredit = totalCredit;
            result.Message = "تم تنفيذ القيد الدوري بنجاح — القيد رقم " + entryId + ".";
            AuditService.Log("EXECUTE", "RecurringJournal", templateId,
                "Recurring journal executed (entry " + entryId + ", debit " + totalDebit.ToString("N2") + ")");
        }
        catch (Exception ex)
        {
            try { await tx.RollbackAsync(); } catch { }
            await RecordFailureAsync(templateId, executionDate, nextRun, ex.Message);
            result.Success = false;
            result.Message = "فشل تنفيذ القيد: " + ex.Message;
        }
        return result;
    }

    /// <summary>يسجّل فشل التنفيذ في السجل (بلا قيد ناقص).</summary>
    private async Task RecordFailureAsync(int templateId, DateTime executionDate, DateTime? plannedRun, string error)
    {
        try
        {
            await using var conn = new NpgsqlConnection(_cs);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();
            await using (var ins = new NpgsqlCommand(@"INSERT INTO recurring_journal_executions
                (template_id, execution_date, planned_run_date, journal_entry_id, status,
                 total_debit, total_credit, error_message, executed_at, executed_by)
                VALUES (@tid, CAST(@ed AS date), CAST(@pd AS date), NULL, 'failed',
                        0, 0, @err, NOW(), 1)", conn, tx))
            {
                ins.Parameters.AddWithValue("@tid", templateId);
                ins.Parameters.AddWithValue("@ed", executionDate);
                ins.Parameters.AddWithValue("@pd", (object?)plannedRun ?? executionDate);
                ins.Parameters.AddWithValue("@err", error);
                await ins.ExecuteNonQueryAsync();
            }
            await using (var upd = new NpgsqlCommand(@"UPDATE recurring_journal_templates
                SET last_run_date=NOW(), last_run_status='failed', last_run_error=@err, updated_at=NOW()
                WHERE id=@id", conn, tx))
            {
                upd.Parameters.AddWithValue("@err", error);
                upd.Parameters.AddWithValue("@id", templateId);
                await upd.ExecuteNonQueryAsync();
            }
            await tx.CommitAsync();
        }
        catch { /* لا نُسقط نتيجة التنفيذ بسبب فشل تسجيل الخطأ */ }
    }

    /// <summary>يحسب تاريخ التشغيل التالي حسب تكرار القالب.</summary>
    public static DateTime ComputeNextRun(DateTime from, string frequency) => frequency.ToLowerInvariant() switch
    {
        "daily" => from.Date.AddDays(1),
        "weekly" => from.Date.AddDays(7),
        "monthly" => from.Date.AddMonths(1),
        "quarterly" => from.Date.AddMonths(3),
        "yearly" => from.Date.AddYears(1),
        _ => from.Date.AddDays(1)
    };

    /// <summary>سجل تنفيذ قالب.</summary>
    public async Task<List<Dictionary<string, object?>>> GetExecutionsAsync(int templateId)
    {
        var list = new List<Dictionary<string, object?>>();
        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(@"SELECT id, execution_date, planned_run_date, journal_entry_id,
            status, COALESCE(total_debit,0), COALESCE(total_credit,0), error_message, executed_at
            FROM recurring_journal_executions WHERE template_id=@t ORDER BY executed_at DESC LIMIT 100", conn);
        cmd.Parameters.AddWithValue("@t", templateId);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new Dictionary<string, object?>
            {
                ["id"] = r.GetInt32(0),
                ["execution_date"] = DbValue.AsDateTime(r.GetValue(1)),
                ["planned_run_date"] = DbValue.AsDateTime(r.GetValue(2)),
                ["journal_entry_id"] = r.IsDBNull(3) ? null : r.GetInt32(3),
                ["status"] = r.GetString(4),
                ["total_debit"] = Convert.ToDecimal(r.GetValue(5)),
                ["total_credit"] = Convert.ToDecimal(r.GetValue(6)),
                ["error_message"] = r.IsDBNull(7) ? null : r.GetString(7),
                ["executed_at"] = DbValue.AsDateTime(r.GetValue(8))
            });
        }
        return list;
    }
}
