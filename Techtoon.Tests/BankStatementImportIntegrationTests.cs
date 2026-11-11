using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Npgsql;
using Techtoon.Core.Banking;
using Techtoon.UI.Services;
using Xunit;

namespace Techtoon.Tests;

/// <summary>
/// B4: استيراد كشوف البنك (PDF) والربط التلقائي بالسندات — اختبار تكامل حقيقي.
/// ينشئ ملف PDF حقيقيًا، يستورده، ثم verifies الربط بسند قبض مطابق.
/// </summary>
[Collection("DatabaseIntegration")]
public class BankStatementImportIntegrationTests
{
    private const string SkipReason = "يتطلب TECHTOON_PG_CONNECTION يشير إلى techtoon_test (قاعدة معزولة).";

    [Fact]
    public void Pdf_Import_Creates_Statement_And_Lines()
    {
        if (!TestDb.Available) return;

        var sandbox = Path.Combine(Path.GetTempPath(), "techtoon_b4int_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sandbox);
        try
        {
            using var conn = TestDb.Open();
            int cashBoxId = GetCashBox(conn);

            var rows = new List<BankStatementPdfFactory.Row>
            {
                new BankStatementPdfFactory.Row(new DateTime(2026, 5, 1), "Transfer in", 0m, 1000m, 1000m),
                new BankStatementPdfFactory.Row(new DateTime(2026, 5, 2), "Transfer out", 400m, 0m, 600m),
            };
            var pdf = Path.Combine(sandbox, "b4.pdf");
            BankStatementPdfFactory.Create(pdf, rows);

            // ---- استخراج وتحليل ----
            var lines = BankStatementPdfReader.ReadAndParse(pdf);
            Assert.Equal(2, lines.Count);

            // ---- استيراد في قاعدة البيانات ----
            var service = new BankStatementImportService(TestDb.ConnectionString!);
            var import = service.ImportAsync(lines, cashBoxId, 0m, 600m, pdf).GetAwaiter().GetResult();

            Assert.True(import.Success, import.Message);
            Assert.Equal(2, import.LinesImported);
            Assert.True(import.StatementId > 0);

            // التحقق من Rows في قاعدة البيانات
            using var check = new NpgsqlCommand(
                "SELECT COUNT(*), SUM(debit), SUM(credit) FROM bank_statement_lines WHERE statement_id=@s", conn);
            check.Parameters.AddWithValue("@s", import.StatementId);
            using var r = check.ExecuteReader();
            r.Read();
            Assert.Equal(2, Convert.ToInt32(r.GetValue(0)));
            Assert.Equal(400m, Convert.ToDecimal(r.GetValue(1)));
            Assert.Equal(1000m, Convert.ToDecimal(r.GetValue(2)));

            // سجل الاستيراد
            var logRows = Convert.ToInt32(TestDb.Scalar(
                "SELECT COUNT(*) FROM bank_statement_import_logs WHERE statement_id=@s AND status='success'",
                ("@s", import.StatementId)));
            Assert.Equal(1, logRows);

            // ---- تنظيف ----
            TestDb.Execute("DELETE FROM bank_statement_lines WHERE statement_id=@s", ("@s", import.StatementId));
            TestDb.Execute("DELETE FROM bank_statement_import_logs WHERE statement_id=@s", ("@s", import.StatementId));
            TestDb.Execute("DELETE FROM bank_statements WHERE id=@s", ("@s", import.StatementId));
        }
        finally
        {
            try { if (Directory.Exists(sandbox)) Directory.Delete(sandbox, true); } catch { }
        }
    }

    [Fact]
    public void AutoMatch_Links_Line_To_Existing_Payment()
    {
        if (!TestDb.Available) return;

        var sandbox = Path.Combine(Path.GetTempPath(), "techtoon_b4int_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sandbox);
        try
        {
            using var conn = TestDb.Open();
            int cashBoxId = GetCashBox(conn);

            // ---- ننشئ سند قبض مطابق (دائن) بتاريخ ومبلغ معروفين ----
            var txDate = new DateTime(2026, 6, 10);
            decimal amount = 750m;
            int paymentId;
            using (var cmd = new NpgsqlCommand(@"INSERT INTO payments
                (date, amount, payment_type, payment_method, reference_number, notes, cash_box_id, currency_id)
                VALUES (CAST(@d AS date), @a, 'receipt', 'cash', @r, 'B4 test', @cb, 1) RETURNING id", conn))
            {
                cmd.Parameters.AddWithValue("@d", txDate);
                cmd.Parameters.AddWithValue("@a", amount);
                cmd.Parameters.AddWithValue("@r", "B4-TEST-" + Guid.NewGuid().ToString("N").Substring(0, 8));
                cmd.Parameters.AddWithValue("@cb", cashBoxId);
                paymentId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // ---- كشف PDF بنفس التاريخ والمبلغ ----
            var rows = new List<BankStatementPdfFactory.Row>
            {
                new BankStatementPdfFactory.Row(txDate, "Customer receipt", 0m, amount, amount),
            };
            var pdf = Path.Combine(sandbox, "b4match.pdf");
            BankStatementPdfFactory.Create(pdf, rows);

            var lines = BankStatementPdfReader.ReadAndParse(pdf);
            Assert.Single(lines);

            var service = new BankStatementImportService(TestDb.ConnectionString!);
            var import = service.ImportAsync(lines, cashBoxId, 0m, amount, pdf).GetAwaiter().GetResult();
            Assert.True(import.Success, import.Message);

            // ---- الربط التلقائي ----
            var match = service.AutoMatchAsync(lines, cashBoxId).GetAwaiter().GetResult();
            Assert.True(match.Success, match.Message);
            Assert.Equal(1, match.Matched);
            Assert.Equal(0, match.Unmatched);

            // نتحقق من قاعدة البيانات: السطر مرتبط بالسند الصحيح
            var matchedId = TestDb.Scalar(
                "SELECT matched_payment_id FROM bank_statement_lines WHERE statement_id=@s",
                ("@s", import.StatementId));
            Assert.NotNull(matchedId);
            Assert.Equal(paymentId, Convert.ToInt32(matchedId));

            var status = TestDb.Scalar(
                "SELECT match_status FROM bank_statement_lines WHERE statement_id=@s",
                ("@s", import.StatementId));
            Assert.Equal("matched", status?.ToString());

            // ---- تنظيف ----
            TestDb.Execute("DELETE FROM bank_statement_lines WHERE statement_id=@s", ("@s", import.StatementId));
            TestDb.Execute("DELETE FROM bank_statement_import_logs WHERE statement_id=@s", ("@s", import.StatementId));
            TestDb.Execute("DELETE FROM bank_statements WHERE id=@s", ("@s", import.StatementId));
            TestDb.Execute("DELETE FROM payments WHERE id=@p", ("@p", paymentId));
        }
        finally
        {
            try { if (Directory.Exists(sandbox)) Directory.Delete(sandbox, true); } catch { }
        }
    }

    [Fact]
    public void AutoMatch_Does_Not_Link_Wrong_Amount_Or_Date()
    {
        if (!TestDb.Available) return;

        var sandbox = Path.Combine(Path.GetTempPath(), "techtoon_b4int_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sandbox);
        try
        {
            using var conn = TestDb.Open();
            int cashBoxId = GetCashBox(conn);

            // سند بمبلغ وتاريخ مختلفين عن الكشف
            int paymentId;
            using (var cmd = new NpgsqlCommand(@"INSERT INTO payments
                (date, amount, payment_type, payment_method, reference_number, notes, cash_box_id, currency_id)
                VALUES (CAST(@d AS date), 999.99, 'receipt', 'cash', @r, 'B4 negative', @cb, 1) RETURNING id", conn))
            {
                cmd.Parameters.AddWithValue("@d", new DateTime(2026, 7, 1));
                cmd.Parameters.AddWithValue("@r", "B4-NEG-" + Guid.NewGuid().ToString("N").Substring(0, 8));
                cmd.Parameters.AddWithValue("@cb", cashBoxId);
                paymentId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            var rows = new List<BankStatementPdfFactory.Row>
            {
                new BankStatementPdfFactory.Row(new DateTime(2026, 7, 20), "Different amount", 0m, 123.45m, 123.45m),
            };
            var pdf = Path.Combine(sandbox, "b4neg.pdf");
            BankStatementPdfFactory.Create(pdf, rows);

            var lines = BankStatementPdfReader.ReadAndParse(pdf);
            var service = new BankStatementImportService(TestDb.ConnectionString!);
            var import = service.ImportAsync(lines, cashBoxId, 0m, 123.45m, pdf).GetAwaiter().GetResult();
            Assert.True(import.Success, import.Message);

            var match = service.AutoMatchAsync(lines, cashBoxId).GetAwaiter().GetResult();
            Assert.True(match.Success, match.Message);
            Assert.Equal(0, match.Matched);   // لا ربط خاطئ

            var matchedId = TestDb.Scalar(
                "SELECT matched_payment_id FROM bank_statement_lines WHERE statement_id=@s",
                ("@s", import.StatementId));
            Assert.True(matchedId == null || matchedId == DBNull.Value || Convert.ToInt32(matchedId) == 0);

            // ---- تنظيف ----
            TestDb.Execute("DELETE FROM bank_statement_lines WHERE statement_id=@s", ("@s", import.StatementId));
            TestDb.Execute("DELETE FROM bank_statement_import_logs WHERE statement_id=@s", ("@s", import.StatementId));
            TestDb.Execute("DELETE FROM bank_statements WHERE id=@s", ("@s", import.StatementId));
            TestDb.Execute("DELETE FROM payments WHERE id=@p", ("@p", paymentId));
        }
        finally
        {
            try { if (Directory.Exists(sandbox)) Directory.Delete(sandbox, true); } catch { }
        }
    }

    private static int GetCashBox(NpgsqlConnection conn)
    {
        using var cmd = new NpgsqlCommand("SELECT id FROM cash_boxes WHERE COALESCE(is_active,TRUE)=TRUE ORDER BY id LIMIT 1", conn);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }
}
