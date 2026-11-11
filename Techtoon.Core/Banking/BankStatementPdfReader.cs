using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Techtoon.Core.Banking;

/// <summary>
/// B4: استخراج كشوف الحساب البنكي من ملفات PDF.
///
/// <para><b>لماذا لا نستخدم page.Text مباشرة؟</b> لأن PdfPig يدمج أسطر الصفحة في نص واحد
/// بلا فواصل أسطر (كل سطر في PDF موضَع بإحداثيات مستقلة)، فتختلط الحركات.
/// لذلك نُعيد بناء الأسطر من مواضع الكلمات (X/Y) ونكتشف أعمدة الكشف (مدين/دائن/رصيد)
/// من صفّ العنوان، ثم نُنتج نصًا منظّمًا يفصل بين الأعمدة بعلامة TAB.</para>
/// </summary>
public static class BankStatementPdfReader
{
    /// <summary>كلمات تدل على عمود المدين (المسحوبات).</summary>
    private static readonly string[] DebitKeywords = { "debit", "withdrawal", "withdrawals", "paid out", "money out", "مدين", "سحب", "مسحوبات", "دفع" };

    /// <summary>كلمات تدل على عمود الدائن (التحصيلات).</summary>
    private static readonly string[] CreditKeywords = { "credit", "deposit", "deposits", "paid in", "money in", "دائن", "ايداع", "إيداع", "تحصيل", "وارد" };

    /// <summary>كلمات تدل على عمود الرصيد.</summary>
    private static readonly string[] BalanceKeywords = { "balance", "running balance", "رصيد", "الرصيد" };

    /// <summary>كلمات تدل على عمود التاريخ.</summary>
    private static readonly string[] DateKeywords = { "date", "transaction date", "value date", "تاريخ", "التاريخ" };

    /// <summary>كلمات تدل على عمود البيان.</summary>
    private static readonly string[] DescriptionKeywords = { "description", "details", "narration", "particulars", "remarks", "statement", "بيان", "البيان", "وصف" };

    /// <summary>فاصل الأعمدة في النص المنظّم.</summary>
    public const char ColumnSeparator = '\t';

    /// <summary>الحد الأدنى للفجوة الأفقية (بالنقاط) التي تُعتبر فاصل أعمدة.</summary>
    private const double ColumnGapPoints = 12d;

    /// <summary>يستخرج النص المجمّع لكل صفحات ملف PDF — بأسطر مُعاد بناؤها.</summary>
    public static string ExtractText(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            throw new FileNotFoundException("ملف PDF غير موجود: " + path, path);

        var sb = new StringBuilder();
        using var doc = PdfDocument.Open(path);
        foreach (var page in doc.GetPages())
            sb.Append(ReadPage(page));
        return sb.ToString();
    }

    /// <summary>يستخرج النص ثم يحلله إلى حركات — العملية الكاملة.</summary>
    public static List<BankStatementLine> ReadAndParse(string path) =>
        BankStatementParser.Parse(ExtractText(path));

    /// <summary>يعيد بناء نص صفحة واحدة.</summary>
    private static string ReadPage(Page page)
    {
        var words = page.GetWords().ToList();
        if (words.Count == 0) return "";

        // 1) تجميع الكلمات في أسطر حسب الإحداثي الرأسي
        var lines = words
            .GroupBy(w => Math.Round(w.BoundingBox.Bottom, 0))
            .OrderByDescending(g => g.Key)
            .Select(g => g.OrderBy(w => w.BoundingBox.Left).ToList())
            .ToList();

        // 2) اكتشاف صفّ العنوان وأعمدة الكشف
        var columns = DetectColumns(lines);
        var sb = new StringBuilder();

        foreach (var line in lines)
        {
            if (IsHeaderLine(line))
            {
                // نتخطى العنوان (هو نفسه يحدد الأعمدة)
                continue;
            }

            if (columns != null)
                sb.AppendLine(BuildSegmentedLine(line, columns));
            else
                sb.AppendLine(BuildPlainLine(line));
        }
        return sb.ToString();
    }

    /// <summary>وصف عمود: الكلمة الدالة + موضع بداية العمود.</summary>
    private sealed class ColumnDef
    {
        public string Kind = "";      // date | description | debit | credit | balance
        public double StartX;
    }

    private static List<ColumnDef>? DetectColumns(List<List<Word>> lines)
    {
        foreach (var line in lines)
        {
            if (!IsHeaderLine(line)) continue;

            var cols = new List<ColumnDef>();
            foreach (var w in line)
            {
                var text = w.Text.Trim().ToLowerInvariant();
                if (text.Length == 0) continue;

                var kind = ClassifyColumn(text);
                if (kind == null) continue;

                // أول كلمة من نفس النوع تحدّد بداية العمود
                if (cols.Any(c => c.Kind == kind)) continue;
                cols.Add(new ColumnDef { Kind = kind, StartX = w.BoundingBox.Left });
            }

            // نحتاج على الأقل: تاريخ + مدين/دائن لنعتبر الكشف مُفهومًا
            bool hasDate = cols.Any(c => c.Kind == "date");
            bool hasAmount = cols.Any(c => c.Kind == "debit" || c.Kind == "credit");
            if (hasDate && hasAmount)
            {
                cols.Sort((a, b) => a.StartX.CompareTo(b.StartX));
                return cols;
            }
        }
        return null;
    }

    private static string? ClassifyColumn(string text)
    {
        if (DebitKeywords.Any(k => text.Contains(k))) return "debit";
        if (CreditKeywords.Any(k => text.Contains(k))) return "credit";
        if (BalanceKeywords.Any(k => text.Contains(k))) return "balance";
        if (DateKeywords.Any(k => text.Contains(k))) return "date";
        if (DescriptionKeywords.Any(k => text.Contains(k))) return "description";
        return null;
    }

    private static bool IsHeaderLine(List<Word> line)
    {
        int hits = 0;
        foreach (var w in line)
        {
            var t = w.Text.Trim().ToLowerInvariant();
            if (t.Length == 0) continue;
            if (ClassifyColumn(t) != null) hits++;
        }
        return hits >= 2;
    }

    /// <summary>يبني سطرًا منظّمًا: تاريخ | بيان | مدين | دائن | رصيد.</summary>
    private static string BuildSegmentedLine(List<Word> line, List<ColumnDef> columns)
    {
        var dateText = "";
        var descWords = new List<string>();
        var debit = "";
        var credit = "";
        var balance = "";

        double descStartX = columns.Where(c => c.Kind == "date").Select(c => c.StartX).DefaultIfEmpty(0).Max();
        double firstAmountX = columns
            .Where(c => c.Kind is "debit" or "credit" or "balance")
            .Select(c => c.StartX)
            .DefaultIfEmpty(double.MaxValue)
            .Min();

        foreach (var w in line)
        {
            var text = w.Text.Trim();
            if (text.Length == 0) continue;
            var x = w.BoundingBox.Left;

            var col = FindColumn(columns, x);
            switch (col)
            {
                case "date":
                    if (dateText.Length == 0) dateText = text;
                    break;
                case "debit":
                    debit = text;
                    break;
                case "credit":
                    credit = text;
                    break;
                case "balance":
                    balance = text;
                    break;
                default:
                    // ما بعد التاريخ وقبل أعمدة المبالغ = البيان
                    if (x > descStartX && x < firstAmountX) descWords.Add(text);
                    break;
            }
        }

        return string.Join(ColumnSeparator, new[] { dateText, string.Join(" ", descWords), debit, credit, balance });
    }

    private static string? FindColumn(List<ColumnDef> columns, double x)
    {
        ColumnDef? found = null;
        foreach (var c in columns)
        {
            if (c.StartX <= x + 0.5) found = c;
            else break;
        }
        return found?.Kind;
    }

    /// <summary>يبني سطرًا نصيًا عاديًا (بدون كشف أعمدة) مع الحفاظ على المسافات.</summary>
    private static string BuildPlainLine(List<Word> line)
    {
        var sb = new StringBuilder();
        double? prevRight = null;
        foreach (var w in line)
        {
            var left = w.BoundingBox.Left;
            if (prevRight.HasValue)
            {
                var gap = left - prevRight.Value;
                var spaces = gap <= 1 ? 1 : (int)Math.Round(gap / 4.0);
                if (spaces < 1) spaces = 1;
                if (spaces > 60) spaces = 60;
                sb.Append(new string(' ', spaces));
            }
            sb.Append(w.Text);
            prevRight = w.BoundingBox.Right;
        }
        return sb.ToString();
    }
}
