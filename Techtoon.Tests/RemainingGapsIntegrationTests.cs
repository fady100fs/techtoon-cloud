using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;
using Techtoon.UI.Services;
using Xunit;

namespace Techtoon.Tests;

/// <summary>
/// الفجوات المتبقية التي كانت بلا اختبار:
///  * سداد خط الائتمان (overdraft repayment)
///  * حركات مراكز التكلفة (cost_center_transactions)
///  * رفض الفاتورة داخل فترة محاسبية مغلقة (AccountingPeriodGuard)
/// </summary>
[Collection("DatabaseIntegration")]
public class RemainingGapsIntegrationTests
{
    // ==================== سداد خط الائتمان ====================

    [Fact]
    public void Overdraft_Withdrawal_Then_Repayment_Closes_The_Loop()
    {
        if (!TestDb.Available) return;

        int loanId = 0;
        int cashBoxId = 0;
        try
        {
            cashBoxId = GetCashBox();
            loanId = EnsureOverdraftLoan();

            // رصيد الخزينة قبل أي حركة
            var cashBefore = GetCashBoxBalance(cashBoxId);

            // 1) سحب (withdrawal) — النقد يدخل الخزينة
            int transId = OverdraftService.Withdraw(loanId, 1000m, cashBoxId, DateTime.Today, "سحب اختبار", 1);
            Assert.True(transId > 0);

            var usedAfterWithdraw = OverdraftService.GetUsedBalance(loanId);
            Assert.Equal(1000m, usedAfterWithdraw);

            // بعد السحب: الخزينة +1000
            Assert.Equal(cashBefore + 1000m, GetCashBoxBalance(cashBoxId));

            // 2) سداد (repayment) — النقد يخرج من الخزينة
            int repayId = OverdraftService.Repay(loanId, 400m, cashBoxId, DateTime.Today, "سداد اختبار", 1);
            Assert.True(repayId > 0);

            // الرصيد المستخدم نقص
            var usedAfterRepay = OverdraftService.GetUsedBalance(loanId);
            Assert.Equal(600m, usedAfterRepay);

            // سطر السداد مسجَّل بنوع 'repayment'
            var txType = TestDb.Scalar(
                "SELECT transaction_type FROM overdraft_transactions WHERE id=@i", ("@i", repayId));
            Assert.Equal("repayment", txType?.ToString());

            // سند الدفع موجود (الخزينة نقصت 400)
            var payRef = TestDb.Scalar(
                "SELECT reference_number FROM payments WHERE reference_number=@r", ("@r", "OD-RP-" + repayId));
            Assert.NotNull(payRef);

            // بعد السداد: الخزينة = قبل + 1000 − 400
            Assert.Equal(cashBefore + 600m, GetCashBoxBalance(cashBoxId));

            // 3) لا يمكن السداد أكثر من الرصيد المستخدم
            var ex = Assert.Throws<InvalidOperationException>(() =>
                OverdraftService.Repay(loanId, 99999m, cashBoxId, DateTime.Today, "سداد زائد", 1));
            Assert.Contains("أكبر من الرصيد", ex.Message);
        }
        finally
        {
            if (loanId > 0)
            {
                TestDb.Execute("DELETE FROM payments WHERE reference_number LIKE 'OD-%'");
                TestDb.Execute("DELETE FROM overdraft_transactions WHERE loan_id=@l", ("@l", loanId));
                TestDb.Execute("DELETE FROM loans WHERE id=@l", ("@l", loanId));
            }
        }
    }

    // ==================== حركات مراكز التكلفة ====================

    [Fact]
    public void CostCenter_Transaction_Is_Recorded_With_Balance()
    {
        if (!TestDb.Available) return;

        int costCenterId = 0;
        try
        {
            costCenterId = EnsureCostCenter();

            var svc = CostCenterService.Create();
            var before = svc.GetBalance(costCenterId);

            var txId = svc.AddTransaction(costCenterId, DateTime.Today, "اختبار حركة", 250m,
                "expense", "invoice", 1);
            Assert.True(txId > 0);

            // الحركة مسجَّلة
            var rows = Convert.ToInt32(TestDb.Scalar(
                "SELECT COUNT(*) FROM cost_center_transactions WHERE cost_center_id=@c", ("@c", costCenterId)));
            Assert.Equal(1, rows);

            var amount = Convert.ToDecimal(TestDb.Scalar(
                "SELECT amount FROM cost_center_transactions WHERE cost_center_id=@c", ("@c", costCenterId)));
            Assert.Equal(250m, amount);

            // الرصيد تحدّث
            var after = svc.GetBalance(costCenterId);
            Assert.Equal(before + 250m, after);

            // حذف
            Assert.True(svc.DeleteTransaction(txId));
            var rowsAfter = Convert.ToInt32(TestDb.Scalar(
                "SELECT COUNT(*) FROM cost_center_transactions WHERE cost_center_id=@c", ("@c", costCenterId)));
            Assert.Equal(0, rowsAfter);
        }
        finally
        {
            if (costCenterId > 0)
            {
                TestDb.Execute("DELETE FROM cost_center_transactions WHERE cost_center_id=@c", ("@c", costCenterId));
                TestDb.Execute("DELETE FROM cost_centers WHERE id=@c", ("@c", costCenterId));
            }
        }
    }

    // ==================== رفض الفاتورة في فترة مغلقة ====================

    [Fact]
    public void Closed_Accounting_Period_Blocks_Posting()
    {
        if (!TestDb.Available) return;

        var periodName = "فترة اختبار " + Guid.NewGuid().ToString("N").Substring(0, 6);
        var start = new DateTime(2020, 1, 1);
        var end = new DateTime(2020, 12, 31);
        int periodId = 0;

        try
        {
            // نفترض أن TECHTOON_PG_CONNECTION يشير لقاعدة معزولة
            periodId = CreatePeriod(periodName, start, end, "closed");

            // تاريخ داخل الفترة المغلقة → ممنوع
            Assert.True(AccountingPeriodGuard.IsPostingBlocked(start.AddMonths(2), out var blocked));
            Assert.Equal(periodName, blocked);
            Assert.Contains("مغلقة", AccountingPeriodGuard.BuildBlockedMessage(blocked!));

            // تاريخ خارج الفترة → مسموح
            Assert.False(AccountingPeriodGuard.IsPostingBlocked(start.AddYears(-1), out _));
            Assert.False(AccountingPeriodGuard.IsPostingBlocked(end.AddDays(1), out _));
        }
        finally
        {
            if (periodId > 0) TestDb.Execute("DELETE FROM accounting_periods WHERE id=@i", ("@i", periodId));
        }
    }

    [Fact]
    public void Open_Accounting_Period_Does_Not_Block()
    {
        if (!TestDb.Available) return;

        var periodName = "فترة مفتوحة " + Guid.NewGuid().ToString("N").Substring(0, 6);
        var start = new DateTime(2021, 1, 1);
        var end = new DateTime(2021, 12, 31);
        int periodId = 0;
        try
        {
            periodId = CreatePeriod(periodName, start, end, "open");
            Assert.False(AccountingPeriodGuard.IsPostingBlocked(start.AddMonths(2), out _));
        }
        finally
        {
            if (periodId > 0) TestDb.Execute("DELETE FROM accounting_periods WHERE id=@i", ("@i", periodId));
        }
    }

    // ==================== مساعدات ====================

    private static int CreatePeriod(string name, DateTime start, DateTime end, string status)
    {
        using var conn = TestDb.Open();
        using var cmd = new NpgsqlCommand(@"INSERT INTO accounting_periods
            (period_name, start_date, end_date, is_fiscal_year, created_by, created_at, status)
            VALUES (@n, @s, @e, FALSE, 1, NOW(), @st) RETURNING id", conn);
        cmd.Parameters.AddWithValue("@n", name);
        cmd.Parameters.AddWithValue("@s", start);
        cmd.Parameters.AddWithValue("@e", end);
        cmd.Parameters.AddWithValue("@st", status);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static int GetCashBox()
    {
        using var conn = TestDb.Open();
        using var cmd = new NpgsqlCommand("SELECT id FROM cash_boxes WHERE COALESCE(is_active,TRUE)=TRUE ORDER BY id LIMIT 1", conn);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static decimal GetCashBoxBalance(int cashBoxId)
    {
        return Convert.ToDecimal(TestDb.Scalar(@"
            SELECT COALESCE((SELECT opening_balance FROM cash_boxes WHERE id=@id),0)
                 + COALESCE((SELECT SUM(amount) FROM payments WHERE cash_box_id=@id AND payment_type='receipt'),0)
                 - COALESCE((SELECT SUM(amount) FROM payments WHERE cash_box_id=@id AND payment_type='payment'),0)
                 - COALESCE((SELECT SUM(amount) FROM expenses WHERE cash_box_id=@id),0)", ("@id", cashBoxId)));
    }

    private static int EnsureOverdraftLoan()
    {
        using var conn = TestDb.Open();
        // حساب خط ائتمان (سيولة)
        int accountId;
        using (var cmd = new NpgsqlCommand(
            "INSERT INTO accounts (code, name, parent_id, type) VALUES (@c, @n, NULL, 'LIABILITY') RETURNING id", conn))
        {
            cmd.Parameters.AddWithValue("@c", "L" + Guid.NewGuid().ToString("N").Substring(0, 10));
            cmd.Parameters.AddWithValue("@n", "خط ائتمان اختبار " + Guid.NewGuid().ToString("N").Substring(0, 6));
            accountId = Convert.ToInt32(cmd.ExecuteScalar());
        }
        int loanId;
        using (var cmd = new NpgsqlCommand(@"INSERT INTO loans
            (loan_number, borrower_name, borrower_type, principal_amount, interest_rate, term_months,
             loan_date, status, loan_type, liability_account_id, credit_limit, created_by, created_at)
            VALUES (@ln, 'خط ائتمان اختبار', 'company', 100000, 0, 12, CURRENT_DATE, 'active', 'overdraft', @acc, 50000, 1, NOW())
            RETURNING id", conn))
        {
            cmd.Parameters.AddWithValue("@ln", "OD-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            cmd.Parameters.AddWithValue("@acc", accountId);
            loanId = Convert.ToInt32(cmd.ExecuteScalar());
        }
        return loanId;
    }

    private static int EnsureCostCenter()
    {
        using var conn = TestDb.Open();
        using var cmd2 = new NpgsqlCommand(@"INSERT INTO cost_centers
            (code, name, is_active, created_by, created_at)
            VALUES (@c, @n, TRUE, 1, NOW()) RETURNING id", conn);
        cmd2.Parameters.AddWithValue("@c", "CC" + Guid.NewGuid().ToString("N").Substring(0, 8));
        cmd2.Parameters.AddWithValue("@n", "مركز تكلفة اختبار " + Guid.NewGuid().ToString("N").Substring(0, 6));
        return Convert.ToInt32(cmd2.ExecuteScalar());
    }
}
