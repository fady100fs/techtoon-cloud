using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Npgsql;
using Techtoon.Core.Banking;
using Techtoon.UI.Services;

namespace Techtoon.UI.Services;

/// <summary>
/// B4: استيراد كشوف الحساب البنكي (PDF) وربط حركاتها بالسندات القائمة.
///-policy الربط: التاريخ + المبلغ + الاتجاه (مدين/دائن) + الخزينة المحدَّدة.
/// </summary>
public class BankStatementImportService
{
    /// <summary>نتيجة استيراد كشف.</summary>
    public class ImportResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int StatementId { get; set; }
        public int LinesImported { get; set; }
        public string SavedFilePath { get; set; } = "";
    }

    /// <summary>نتيجة الربط التلقائي.</summary>
    public class MatchResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int Matched { get; set; }
        public int Unmatched { get; set; }
    }

    private readonly string _connectionString;

    public BankStatementImportService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public static BankStatementImportService Create() =>
        new BankStatementImportService(SecureConnectionManager.GetConnectionString());

    /// <summary>يحفظ ملف الـ PDF داخل مجلد بيانات المستخدم ويعيد مساره.</summary>
    public static string SaveStatementFile(string sourcePath)
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TechtoonAccounting", "BankStatements");
        Directory.CreateDirectory(dir);
        var saved = Path.Combine(dir, Path.GetFileName(sourcePath));
        File.Copy(sourcePath, saved, true);
        return saved;
    }

    /// <summary>
    /// يستورد كشفًا: ينشئ سجل bank_statements ثم سطور bank_statement_lines — داخل معاملة واحدة.
    /// </summary>
    public async Task<ImportResult> ImportAsync(List<BankStatementLine> lines, int cashBoxId,
        decimal openingBalance, decimal closingBalance, string sourcePdfPath)
    {
        var result = new ImportResult();
        if (lines == null || lines.Count == 0)
        {
            result.Success = false;
            result.Message = "لا توجد حركات للاستيراد.";
            return result;
        }
        if (cashBoxId <= 0)
        {
            result.Success = false;
            result.Message = "اختر الخزينة/الحساب البنكي (إلزامي).";
            return result;
        }

        var saved = string.Empty;
        try
        {
            saved = SaveStatementFile(sourcePdfPath);
            var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(saved))).ToLowerInvariant();
            var fileSize = new FileInfo(saved).Length;

            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();
            try
            {
                int statementId;
                // مخطط bank_statements الفعلي: لا يوجد عمود statement_number — المرجع يُحفظ في notes.
                await using (var ins = new NpgsqlCommand(@"
                    INSERT INTO bank_statements
                        (cash_box_id, statement_date, period_start, period_end, opening_balance, closing_balance,
                         currency, source_file_path, source_file_format, source_file_size, file_hash,
                         imported_by, imported_at, status, notes)
                    VALUES (@cb, CAST(@sd AS date), CAST(@ps AS date), CAST(@pe AS date), @ob, @cl,
                            'EGP', @path, 'pdf', @size, @hash, 1, NOW(), 'imported', @notes)
                    RETURNING id", conn, tx))
                {
                    ins.Parameters.AddWithValue("@cb", cashBoxId);
                    ins.Parameters.AddWithValue("@sd", lines.Max(l => l.TransactionDate));
                    ins.Parameters.AddWithValue("@ps", lines.Min(l => l.TransactionDate));
                    ins.Parameters.AddWithValue("@pe", lines.Max(l => l.TransactionDate));
                    ins.Parameters.AddWithValue("@ob", openingBalance);
                    ins.Parameters.AddWithValue("@cl", closingBalance);
                    ins.Parameters.AddWithValue("@path", saved);
                    ins.Parameters.AddWithValue("@size", fileSize);
                    ins.Parameters.AddWithValue("@hash", hash);
                    ins.Parameters.AddWithValue("@notes", "PDF: " + Path.GetFileName(sourcePdfPath));
                    statementId = Convert.ToInt32(await ins.ExecuteScalarAsync());
                }

                int n = 0;
                foreach (var l in lines)
                {
                    await using var ins = new NpgsqlCommand(@"
                        INSERT INTO bank_statement_lines
                            (statement_id, line_number, transaction_date, value_date, description, debit, credit, balance, match_status)
                        VALUES (@sid, @ln, CAST(@d AS date), CAST(@d AS date), @desc, @dr, @cr, @bal, 'unmatched')", conn, tx);
                    ins.Parameters.AddWithValue("@sid", statementId);
                    ins.Parameters.AddWithValue("@ln", l.LineNumber);
                    ins.Parameters.AddWithValue("@d", l.TransactionDate);
                    ins.Parameters.AddWithValue("@desc", l.Description);
                    ins.Parameters.AddWithValue("@dr", l.Debit);
                    ins.Parameters.AddWithValue("@cr", l.Credit);
                    ins.Parameters.AddWithValue("@bal", l.Balance);
                    await ins.ExecuteNonQueryAsync();
                    n++;
                }

                // سجل الاستيراد (bank_statement_import_logs)
                await using (var log = new NpgsqlCommand(@"
                    INSERT INTO bank_statement_import_logs
                        (statement_id, template_id, started_at, completed_at, status,
                         total_rows, imported_rows, skipped_rows, error_rows, duplicate_rows, performed_by)
                    VALUES (@sid, NULL, NOW(), NOW(), 'success', @tot, @imp, 0, 0, 0, 1)", conn, tx))
                {
                    log.Parameters.AddWithValue("@sid", statementId);
                    log.Parameters.AddWithValue("@tot", lines.Count);
                    log.Parameters.AddWithValue("@imp", n);
                    await log.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();

                result.Success = true;
                result.StatementId = statementId;
                result.LinesImported = n;
                result.SavedFilePath = saved;
                result.Message = "تم استيراد " + n + " حركة.";
                AuditService.Log("IMPORT", "BankStatement", statementId, $"Imported {n} lines from {Path.GetFileName(sourcePdfPath)}");
            }
            catch
            {
                try { await tx.RollbackAsync(); } catch { }
                throw;
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = "فشل الاستيراد: " + ex.Message;
        }
        return result;
    }

    /// <summary>
    /// يربط الحركات بالسندات القائمة داخل معاملة واحدة.
    /// المطابقة: نفس الخزينة + نفس التاريخ + نفس المبلغ + نفس الاتجاه (مدين↔payment / دائن↔receipt).
    /// </summary>
    public async Task<MatchResult> AutoMatchAsync(List<BankStatementLine> lines, int cashBoxId)
    {
        var result = new MatchResult();
        if (lines == null || lines.Count == 0)
        {
            result.Success = false;
            result.Message = "لا توجد حركات للربط.";
            return result;
        }
        if (cashBoxId <= 0)
        {
            result.Success = false;
            result.Message = "اختر الخزينة/الحساب البنكي (إلزامي).";
            return result;
        }

        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();
            try
            {
                int matched = 0;
                foreach (var l in lines)
                {
                    var amount = l.Amount;
                    var paymentType = l.ExpectedPaymentType;

                    // ابحث عن سند مطابق غير مُربط بعد
                    int? paymentId = null;
                    await using (var find = new NpgsqlCommand(@"
                        SELECT p.id
                        FROM payments p
                        WHERE p.cash_box_id = @cb
                          AND p.amount = @amt
                          AND p.payment_type = @pt
                          AND CAST(p.date AS date) = CAST(@d AS date)
                          AND NOT EXISTS (
                                SELECT 1 FROM bank_statement_lines bl
                                WHERE bl.matched_payment_id = p.id)
                        ORDER BY p.id DESC
                        LIMIT 1", conn, tx))
                    {
                        find.Parameters.AddWithValue("@cb", cashBoxId);
                        find.Parameters.AddWithValue("@amt", amount);
                        find.Parameters.AddWithValue("@pt", paymentType);
                        find.Parameters.AddWithValue("@d", l.TransactionDate);
                        var v = await find.ExecuteScalarAsync();
                        if (v != null && v != DBNull.Value) paymentId = Convert.ToInt32(v);
                    }

                    if (!paymentId.HasValue) continue;

                    // اربط سطر الكشف بالسند
                    await using (var upd = new NpgsqlCommand(@"
                        UPDATE bank_statement_lines bl
                        SET match_status = 'matched',
                            matched_payment_id = @pid,
                            matched_at = NOW(),
                            match_confidence = 90
                        FROM bank_statements bs
                        WHERE bl.statement_id = bs.id
                          AND bs.cash_box_id = @cb
                          AND bl.transaction_date = CAST(@d AS date)
                          AND bl.debit = @dr AND bl.credit = @cr
                          AND bl.match_status = 'unmatched'
                          AND bl.line_number = @ln", conn, tx))
                    {
                        upd.Parameters.AddWithValue("@pid", paymentId.Value);
                        upd.Parameters.AddWithValue("@cb", cashBoxId);
                        upd.Parameters.AddWithValue("@d", l.TransactionDate);
                        upd.Parameters.AddWithValue("@dr", l.Debit);
                        upd.Parameters.AddWithValue("@cr", l.Credit);
                        upd.Parameters.AddWithValue("@ln", l.LineNumber);
                        var n = await upd.ExecuteNonQueryAsync();
                        if (n > 0)
                        {
                            matched++;
                            l.MatchStatus = "matched";
                            l.MatchedPaymentId = paymentId.Value;
                        }
                    }
                }

                await tx.CommitAsync();
                result.Success = true;
                result.Matched = matched;
                result.Unmatched = lines.Count - matched;
                result.Message = $"تم ربط {matched} حركة، و{result.Unmatched} حركة بلا سند مطابق.";
                if (matched > 0)
                    AuditService.Log("MATCH", "BankStatement", cashBoxId, $"Auto-matched {matched} statement lines to payments");
            }
            catch
            {
                try { await tx.RollbackAsync(); } catch { }
                throw;
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = "فشل الربط: " + ex.Message;
        }
        return result;
    }
}
