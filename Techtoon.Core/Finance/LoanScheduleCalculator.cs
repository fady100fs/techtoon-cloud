namespace Techtoon.Core.Finance;

/// <summary>
/// P4: حاسبة جدول أقساط القروض (منطق خالص — قابل للاختبار).
/// أنواع القروض المدعومة:
///   installment_fixed    → سداد بأقساط بفائدة ثابتة (قسط ثابت - annuity)
///   installment_variable → سداد بأقساط بفائدة متغيرة (أصل متساوي + فائدة على الرصيد المتبقي)
///   running_interest     → جارٍ مدين: تسديد فائدة فقط، يبقى الأصل للبنك
///   interest_free        → بدون فائدة
/// </summary>
public static class LoanScheduleCalculator
{
    public class ScheduleRow
    {
        public int Number { get; set; }
        public DateTime DueDate { get; set; }
        public decimal Principal { get; set; }
        public decimal Interest { get; set; }
        public decimal Total { get; set; }
    }

    /// <summary>عدد الدفعات في السنة حسب التكرار.</summary>
    public static int PeriodsPerYear(string frequency) => frequency switch
    {
        "quarterly" => 4,
        "yearly" => 1,
        "weekly" => 52,
        _ => 12
    };

    /// <summary>يبني جدول الأقساط. rateAnnual بصيغة نسبة مئوية (مثال 12 = 12%).</summary>
    public static List<ScheduleRow> Build(
        string loanType,
        decimal principal,
        decimal rateAnnual,
        int count,
        string frequency,
        DateTime firstDue,
        int graceMonths = 0)
    {
        if (principal <= 0) throw new ArgumentOutOfRangeException(nameof(principal));
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));

        int ppy = PeriodsPerYear(frequency);
        decimal r = rateAnnual / 100m / ppy;
        var due = graceMonths > 0 ? firstDue.AddMonths(graceMonths) : firstDue;
        var rows = new List<ScheduleRow>(count);
        decimal remaining = principal;
        decimal linear = principal / count;
        decimal annuity = 0m;

        if (loanType == "installment_fixed" && r > 0)
        {
            double a = 1d;
            for (int k = 0; k < count; k++) a *= (double)(1m + r);
            annuity = principal * r / (1m - (decimal)(1d / a));
        }

        for (int i = 1; i <= count; i++)
        {
            decimal pRaw, interest;
            switch (loanType)
            {
                case "running_interest":
                    interest = Math.Round(principal * r, 2); pRaw = 0m; break;
                case "interest_free":
                    interest = 0m; pRaw = linear; break;
                case "installment_variable":
                    pRaw = linear;
                    interest = Math.Round(remaining * r, 2); break;
                default: // installment_fixed
                    if (r == 0m) { interest = 0m; pRaw = linear; }
                    else { interest = Math.Round(remaining * r, 2); pRaw = annuity - interest; }
                    break;
            }
            decimal p = Math.Round(pRaw, 2);
            // القسط الأخير يمتص فرق التقريب حتى يساوي مجموع الأصل رأس المال بالضبط
            // (لا ينطبق على الجارٍ مدين: الأصل يبقى للبنك ولا يُسدَّد ضمن الأقساط)
            if (i == count && loanType != "running_interest") p = remaining;
            if (p < 0) p = 0;
            rows.Add(new ScheduleRow
            {
                Number = i,
                DueDate = due,
                Principal = p,
                Interest = interest,
                Total = Math.Round(p + interest, 2)
            });
            remaining -= p;
            if (frequency == "weekly") due = due.AddDays(7);
            else due = due.AddMonths(ppy == 4 ? 3 : ppy == 1 ? 12 : 1);
        }
        return rows;
    }
}
