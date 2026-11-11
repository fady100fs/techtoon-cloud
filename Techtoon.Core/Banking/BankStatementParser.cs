using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Techtoon.Core.Banking;

/// <summary>حركة واحدة مستخرجة من كشف حساب بنكي.</summary>
public class BankStatementLine
{
    public int LineNumber { get; set; }
    public DateTime TransactionDate { get; set; }
    public string Description { get; set; } = "";
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
    public string MatchStatus { get; set; } = "unmatched";
    public int? MatchedPaymentId { get; set; }

    /// <summary>المبلغ الكلي للحركة (مدين أو دائن).</summary>
    public decimal Amount => Debit > 0 ? Debit : Credit;

    /// <summary>اتجاه الحركة: مدين = صرف، دائن = تحصيل.</summary>
    public bool IsDebit => Debit > 0;

    /// <summary>نوع السند المقابل: receipt للدائن، payment للمدين.</summary>
    public string ExpectedPaymentType => IsDebit ? "payment" : "receipt";

    public string DateDisplay => TransactionDate.ToString("yyyy-MM-dd");
    public string DebitDisplay => Debit.ToString("N2");
    public string CreditDisplay => Credit.ToString("N2");
    public string BalanceDisplay => Balance.ToString("N2");

    public string MatchDisplay => MatchStatus switch
    {
        "matched" => "مُربط",
        "partial" => "مرشّح",
        _ => "غير مُربط"
    };
}

/// <summary>
/// B4: تحليل كشوف الحساب البنكي إلى حركات قابلة للاستيراد.
/// يدعم صيغتين:
///  * نص مُقسّم أعمدة (TAB) — المُخرَج من قارئ PDF بعد اكتشاف الأعمدة.
///  * نص حرّ — يُحلَّل heuristically (آخر الأرقام = المدين/الدائن/الرصيد).
/// </summary>
public static class BankStatementParser
{
    /// <summary>صيغ التاريخ المدعومة في كشوف البنوك.</summary>
    public static readonly string[] DatePatterns =
    {
        "dd/MM/yyyy", "dd-MM-yyyy", "yyyy/MM/dd", "yyyy-MM-dd",
        "dd.MM.yyyy", "dd/MM/yy", "dd-MM-yy", "MM/dd/yyyy"
    };

    private static readonly Regex DateRegex = new(@"\d{1,4}[./-]\d{1,2}[./-]\d{1,4}", RegexOptions.Compiled);
    private static readonly Regex NumberRegex = new(@"-?\d{1,3}(?:[,\s]\d{3})*(?:\.\d{1,2})?", RegexOptions.Compiled);

    /// <summary>يحلل نص الكشف إلى قائمة حركات.</summary>
    public static List<BankStatementLine> Parse(string? text)
    {
        var list = new List<BankStatementLine>();
        if (string.IsNullOrWhiteSpace(text)) return list;

        int lineNo = 0;
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (line.Trim().Length == 0) continue;

            BankStatementLine? parsed = line.Contains('\t')
                ? ParseSegmented(line)
                : ParsePlain(line);

            if (parsed == null) continue;
            parsed.LineNumber = ++lineNo;
            list.Add(parsed);
        }
        return list;
    }

    /// <summary>يحلّل سطرًا مقسّمًا بالأعمدة: تاريخ | بيان | مدين | دائن | رصيد.</summary>
    private static BankStatementLine? ParseSegmented(string line)
    {
        var parts = line.Split('\t');
        var dateText = parts.Length > 0 ? parts[0].Trim() : "";
        var desc = parts.Length > 1 ? parts[1].Trim() : "";
        var debitText = parts.Length > 2 ? parts[2].Trim() : "";
        var creditText = parts.Length > 3 ? parts[3].Trim() : "";
        var balanceText = parts.Length > 4 ? parts[4].Trim() : "";

        var date = FindDate(dateText);
        if (date == null) return null;

        var debit = ParseAmount(debitText);
        var credit = ParseAmount(creditText);
        var balance = ParseAmount(balanceText);

        if (debit == 0 && credit == 0) return null;

        return new BankStatementLine
        {
            TransactionDate = date.Value,
            Description = TrimDescription(desc),
            Debit = debit,
            Credit = credit,
            Balance = balance
        };
    }

    /// <summary>يحلّل سطرًا نصيًا حرًّا.</summary>
    private static BankStatementLine? ParsePlain(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.Length < 8) return null;

        var date = FindDate(line);
        if (date == null) return null;

        // نزيل التاريخ من السطر قبل استخراج المبالغ — وإلا حُسبت أرقام التاريخ مبالغ.
        var withoutDate = DateRegex.Replace(line, " ");
        var numbers = FindNumbers(withoutDate);
        if (numbers.Count == 0) return null;

        decimal debit = 0, credit = 0, balance = 0;
        if (numbers.Count == 1)
        {
            // حركة بمبلغ واحد: السالب = مدين، الموجب = دائن
            if (numbers[0] < 0) debit = Math.Abs(numbers[0]);
            else credit = numbers[0];
        }
        else if (numbers.Count == 2)
        {
            debit = Math.Abs(numbers[0]);
            credit = Math.Abs(numbers[1]);
        }
        else
        {
            // الأعمدة الثلاثة الأخيرة: مدين / دائن / رصيد
            debit = numbers[numbers.Count - 3];
            credit = numbers[numbers.Count - 2];
            balance = numbers[numbers.Count - 1];
            if (debit < 0) debit = 0;
            if (credit < 0) credit = 0;
        }

        if (debit == 0 && credit == 0) return null;

        return new BankStatementLine
        {
            TransactionDate = date.Value,
            Description = TrimDescription(RemoveDateAndNumbers(line)),
            Debit = debit,
            Credit = credit,
            Balance = balance
        };
    }

    /// <summary>يبحث عن أول تاريخ صالح في السطر (يتجاهل أرقامًا شبيهة بالتاريخ).</summary>
    public static DateTime? FindDate(string line)
    {
        foreach (Match m in DateRegex.Matches(line ?? ""))
        {
            var s = m.Value;
            foreach (var p in DatePatterns)
            {
                if (DateTime.TryParseExact(s, p, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    return dt;
            }
        }
        return null;
    }

    /// <summary>يستخرج كل المبالغ من السطر (يتجاهل القيم الصفرية).</summary>
    public static List<decimal> FindNumbers(string line)
    {
        var list = new List<decimal>();
        foreach (Match m in NumberRegex.Matches(line ?? ""))
        {
            var s = m.Value.Replace(" ", "").Replace(",", "");
            if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                if (Math.Abs(v) >= 0.01m) list.Add(v);
        }
        return list;
    }

    /// <summary>يحوّل نص مبلغ (قد يحوي رموز عملة أو مسافات) إلى رقم.</summary>
    public static decimal ParseAmount(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0m;
        var s = text.Trim().Replace(",", "").Replace(" ", "");
        if (s.Length == 0) return 0m;
        if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
            return Math.Abs(v);
        return 0m;
    }

    private static string RemoveDateAndNumbers(string line)
    {
        var s = DateRegex.Replace(line, " ");
        s = NumberRegex.Replace(s, " ");
        s = Regex.Replace(s, @"\s+", " ").Trim();
        return s;
    }

    private static string TrimDescription(string desc)
    {
        desc = Regex.Replace(desc ?? "", @"^[\s\-–—|:;,]+", "").Trim();
        return desc.Length > 180 ? desc[..180] : desc;
    }
}
