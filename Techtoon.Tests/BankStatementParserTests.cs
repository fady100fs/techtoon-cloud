using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Techtoon.Core.Banking;
using Xunit;

namespace Techtoon.Tests;

/// <summary>B4: تحليل كشوف البنك (PDF) — اختبارات على ملفات PDF حقيقية.</summary>
public class BankStatementParserTests : IDisposable
{
    private readonly string _sandbox;

    public BankStatementParserTests()
    {
        _sandbox = Path.Combine(Path.GetTempPath(), "techtoon_b4_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_sandbox);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_sandbox)) Directory.Delete(_sandbox, true); } catch { }
    }

    private string SandboxFile(string name) => Path.Combine(_sandbox, name);

    private static List<BankStatementPdfFactory.Row> SampleRows() => new()
    {
        new BankStatementPdfFactory.Row(new DateTime(2026, 3, 1), "Opening balance transfer", 0m, 5000m, 5000m),
        new BankStatementPdfFactory.Row(new DateTime(2026, 3, 5), "Payment to supplier", 1500m, 0m, 3500m),
        new BankStatementPdfFactory.Row(new DateTime(2026, 3, 9), "Receipt from customer", 0m, 2200m, 5700m),
        new BankStatementPdfFactory.Row(new DateTime(2026, 3, 15), "Bank charges", 75.5m, 0m, 5624.5m),
    };

    // ---------- قراءة PDF حقيقية ----------

    [Fact]
    public void PdfReader_Extracts_Text_From_Real_Pdf()
    {
        var path = SandboxFile("statement.pdf");
        BankStatementPdfFactory.Create(path, SampleRows());

        Assert.True(File.Exists(path));
        var text = BankStatementPdfReader.ExtractText(path);

        Assert.False(string.IsNullOrWhiteSpace(text));
        Assert.Contains("01/03/2026", text);
        Assert.Contains("Payment to supplier", text);
    }

    [Fact]
    public void PdfReader_Reads_And_Parses_Movements()
    {
        var path = SandboxFile("statement.pdf");
        BankStatementPdfFactory.Create(path, SampleRows());

        var lines = BankStatementPdfReader.ReadAndParse(path);

        Assert.Equal(4, lines.Count);
        Assert.All(lines, l => Assert.True(l.Debit > 0 || l.Credit > 0));
    }

    [Fact]
    public void PdfReader_Throws_On_Missing_File()
    {
        Assert.Throws<FileNotFoundException>(() => BankStatementPdfReader.ExtractText(SandboxFile("nope.pdf")));
    }

    // ---------- صحة الحركات ----------

    [Fact]
    public void Parsed_Movements_Have_Correct_Dates_And_Amounts()
    {
        var path = SandboxFile("statement.pdf");
        BankStatementPdfFactory.Create(path, SampleRows());
        var lines = BankStatementPdfReader.ReadAndParse(path);

        var supplier = lines.First(l => l.Description.Contains("Payment to supplier"));
        Assert.Equal(new DateTime(2026, 3, 5), supplier.TransactionDate);
        Assert.Equal(1500m, supplier.Debit);
        Assert.Equal(0m, supplier.Credit);
        Assert.True(supplier.IsDebit);
        Assert.Equal("payment", supplier.ExpectedPaymentType);
        Assert.Equal(1500m, supplier.Amount);

        var customer = lines.First(l => l.Description.Contains("Receipt from customer"));
        Assert.Equal(new DateTime(2026, 3, 9), customer.TransactionDate);
        Assert.Equal(2200m, customer.Credit);
        Assert.False(customer.IsDebit);
        Assert.Equal("receipt", customer.ExpectedPaymentType);
    }

    [Fact]
    public void Parsed_Movements_Carry_Balance()
    {
        var path = SandboxFile("statement.pdf");
        BankStatementPdfFactory.Create(path, SampleRows());
        var lines = BankStatementPdfReader.ReadAndParse(path);

        Assert.All(lines, l => Assert.True(l.Balance > 0));
        Assert.Equal(5624.5m, lines.Last().Balance);
    }

    [Fact]
    public void Parsed_Movements_Start_Unmatched()
    {
        var path = SandboxFile("statement.pdf");
        BankStatementPdfFactory.Create(path, SampleRows());
        var lines = BankStatementPdfReader.ReadAndParse(path);

        Assert.All(lines, l =>
        {
            Assert.Equal("unmatched", l.MatchStatus);
            Assert.Null(l.MatchedPaymentId);
        });
    }

    // ---------- التحليل النصي المباشر ----------

    [Fact]
    public void Parser_Ignores_Header_And_Non_Movement_Lines()
    {
        var text = @"Date        Description      Debit     Credit     Balance
Date        Description      Debit     Credit     Balance
                                             1000.00
Total                                              1000.00";
        var lines = BankStatementParser.Parse(text);
        // السطر الفارغ من الأرقام لا ينتج حركة
        Assert.DoesNotContain(lines, l => l.Amount == 0);
    }

    [Fact]
    public void Parser_Handles_Empty_And_Null_Input()
    {
        Assert.Empty(BankStatementParser.Parse(null));
        Assert.Empty(BankStatementParser.Parse(""));
        Assert.Empty(BankStatementParser.Parse("   \n  \n"));
    }

    [Fact]
    public void Parser_Supports_Multiple_Date_Formats()
    {
        var text = "2026-04-01  Sale A   100.00   0.00   100.00\n" +
                   "02/04/2026  Sale B   0.00     200.00 300.00\n" +
                   "03.04.2026  Sale C   50.00    0.00   250.00";
        var lines = BankStatementParser.Parse(text);

        Assert.Equal(3, lines.Count);
        Assert.Equal(new DateTime(2026, 4, 1), lines[0].TransactionDate);
        Assert.Equal(new DateTime(2026, 4, 2), lines[1].TransactionDate);
        Assert.Equal(new DateTime(2026, 4, 3), lines[2].TransactionDate);
    }

    [Fact]
    public void Parser_Skips_Lines_Without_Date()
    {
        var text = "Opening balance    1000.00\n03/04/2026  Sale  100.00  0.00  1100.00";
        var lines = BankStatementParser.Parse(text);
        Assert.Single(lines);
        Assert.Equal(new DateTime(2026, 4, 3), lines[0].TransactionDate);
    }

    [Fact]
    public void Parser_Handles_Thousands_Separators()
    {
        var text = "05/03/2026  Big payment  1,250.50  0.00  1,250.50";
        var lines = BankStatementParser.Parse(text);
        Assert.Single(lines);
        Assert.Equal(1250.50m, lines[0].Debit);
    }
}
