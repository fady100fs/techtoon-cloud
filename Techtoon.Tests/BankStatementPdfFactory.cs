using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Techtoon.Tests;

/// <summary>
/// مولّد كشوف بنك PDF حقيقية للاختبار (B4).
/// ينتج ملف PDF صالحًا يُقرأ بواسطة PdfPig — لا ملفات وهمية.
/// </summary>
public static class BankStatementPdfFactory
{
    /// <summary>سطر حركة في الكشف.</summary>
    public record Row(DateTime Date, string Description, decimal Debit, decimal Credit, decimal Balance);

    /// <summary>
    /// يبني ملف PDF حقيقيًا يحتوي كشف حساب بنكي.
    /// يستخدم_lines محتوى PDF مباشر ( operators نصية) — يُقرأ عبر PdfPig.
    /// </summary>
    public static string Create(string path, IEnumerable<Row> rows, string title = "Bank Statement")
    {
        var content = new StringBuilder();
        content.AppendLine("BT /F1 12 Tf 50 780 Td (" + Escape(title) + ") Tj ET");
        content.AppendLine("BT /F1 10 Tf 50 760 Td (Date        Description                     Debit        Credit      Balance) Tj ET");

        var y = 740;
        foreach (var r in rows)
        {
            var date = r.Date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            var desc = r.Description.Length > 30 ? r.Description[..30] : r.Description;
            var debit = r.Debit > 0 ? r.Debit.ToString("0.00", CultureInfo.InvariantCulture) : "";
            var credit = r.Credit > 0 ? r.Credit.ToString("0.00", CultureInfo.InvariantCulture) : "";
            var bal = r.Balance.ToString("0.00", CultureInfo.InvariantCulture);

            var line = $"{date}   {desc,-30} {debit,12} {credit,12} {bal,14}";
            content.AppendLine("BT /F1 10 Tf 50 " + y + " Td (" + Escape(line) + ") Tj ET");
            y -= 16;
            if (y < 60) break;
        }

        var pdf = BuildPdf(content.ToString());
        File.WriteAllText(path, pdf, new UTF8Encoding(false));
        return path;
    }

    private static string Escape(string s) =>
        s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

    /// <summary>يبني مستند PDF صالحًا (بنية كلاسيكية: Catalog / Pages / Page / Contents / Font).</summary>
    private static string BuildPdf(string contentStream)
    {
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(contentStream)} >>\nstream\n{contentStream}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"
        };

        var sb = new StringBuilder();
        sb.Append("%PDF-1.4\n");
        var offsets = new List<int>();
        foreach (var obj in objects)
        {
            offsets.Add(sb.Length);
            sb.Append($"{offsets.Count} 0 obj\n{obj}\nendobj\n");
        }

        var xrefPos = sb.Length;
        sb.Append($"xref\n0 {objects.Count + 1}\n");
        sb.Append("0000000000 65535 f \n");
        foreach (var off in offsets) sb.Append(off.ToString("D10") + " 00000 n \n");
        sb.Append($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefPos}\n%%EOF\n");
        return sb.ToString();
    }
}
