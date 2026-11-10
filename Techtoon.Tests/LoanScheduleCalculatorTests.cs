using Techtoon.Core.Finance;

namespace Techtoon.Tests;

/// <summary>P4: اختبارات حاسبة جدول أقساط القروض (سداد ثابت/متغير/جارٍ مدين/بدون فائدة).</summary>
public class LoanScheduleCalculatorTests
{
    private static decimal Sum(IEnumerable<decimal> xs) => xs.Sum();

    [Fact]
    public void FixedRate_installments_principal_sums_to_principal()
    {
        var rows = LoanScheduleCalculator.Build("installment_fixed", 120000m, 12m, 12, "monthly", new DateTime(2026, 1, 1));
        Assert.Equal(12, rows.Count);
        // مجموع أقساط الأصل = رأس المال بالضبط
        Assert.Equal(120000m, Math.Round(Sum(rows.Select(r => r.Principal)), 2));
        // الفائدة موجبة、总 الإجمالي = أصل + فائدة
        Assert.True(Sum(rows.Select(r => r.Interest)) > 0);
        Assert.Equal(
            Math.Round(Sum(rows.Select(r => r.Total)), 2),
            Math.Round(Sum(rows.Select(r => r.Principal)) + Sum(rows.Select(r => r.Interest)), 2));
    }

    [Fact]
    public void FixedRate_annuity_installment_is_constant()
    {
        var rows = LoanScheduleCalculator.Build("installment_fixed", 100000m, 10m, 12, "monthly", new DateTime(2026, 1, 1));
        var totals = rows.Select(r => r.Total).ToList();
        // القسط الثابت: كل الأقساط متساوية (عدا التقريب في القسط الأخير)
        Assert.True(Math.Abs(totals[0] - totals[1]) < 0.02m);
        Assert.True(Math.Abs(totals[0] - totals[11]) < 0.05m);
    }

    [Fact]
    public void VariableRate_principal_equal_but_interest_declines()
    {
        var rows = LoanScheduleCalculator.Build("installment_variable", 120000m, 12m, 12, "monthly", new DateTime(2026, 1, 1));
        Assert.Equal(12, rows.Count);
        Assert.Equal(120000m, Math.Round(Sum(rows.Select(r => r.Principal)), 2));
        // الفائدة على الرصيد المتبقي تنخفض مع الزمن
        Assert.True(rows[0].Interest > rows[6].Interest);
        Assert.True(rows[6].Interest > rows[11].Interest);
    }

    [Fact]
    public void RunningInterest_only_interest_is_paid_principal_stays()
    {
        var rows = LoanScheduleCalculator.Build("running_interest", 200000m, 10m, 12, "monthly", new DateTime(2026, 1, 1));
        Assert.Equal(12, rows.Count);
        Assert.All(rows, r => Assert.Equal(0m, r.Principal));
        // فائدة ثابتة (الأساس لا يتغير) = 200000 * 10% / 12
        Assert.All(rows, r => Assert.Equal(Math.Round(200000m * 0.10m / 12m, 2), r.Interest));
        // رأس المال لم يُسدّد
        Assert.Equal(0m, Sum(rows.Select(r => r.Principal)));
    }

    [Fact]
    public void InterestFree_splits_principal_evenly()
    {
        var rows = LoanScheduleCalculator.Build("interest_free", 60000m, 0m, 6, "monthly", new DateTime(2026, 1, 1));
        Assert.Equal(6, rows.Count);
        Assert.All(rows, r => Assert.Equal(0m, r.Interest));
        Assert.Equal(60000m, Math.Round(Sum(rows.Select(r => r.Principal)), 2));
        Assert.All(rows, r => Assert.Equal(10000m, r.Principal));
    }

    [Fact]
    public void Grace_period_shifts_due_dates()
    {
        var first = new DateTime(2026, 1, 1);
        var rows = LoanScheduleCalculator.Build("installment_fixed", 120000m, 12m, 12, "monthly", first, graceMonths: 3);
        Assert.Equal(new DateTime(2026, 4, 1), rows[0].DueDate);
    }

    [Fact]
    public void Frequency_quarterly_moves_three_months()
    {
        var rows = LoanScheduleCalculator.Build("installment_fixed", 120000m, 12m, 4, "quarterly", new DateTime(2026, 1, 1));
        Assert.Equal(new DateTime(2026, 4, 1), rows[1].DueDate);
        Assert.Equal(new DateTime(2026, 10, 1), rows[3].DueDate);
    }

    [Fact]
    public void Invalid_input_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            LoanScheduleCalculator.Build("installment_fixed", 0m, 10m, 12, "monthly", DateTime.Today));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            LoanScheduleCalculator.Build("installment_fixed", 1000m, 10m, 0, "monthly", DateTime.Today));
    }
}
