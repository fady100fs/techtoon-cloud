using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Npgsql;
using Techtoon.Core.Data;

namespace Techtoon.UI.Services
{
    /// <summary>
    /// P5: السحب على البنك (تسهيل ائتماني) — القرض من نوع 'overdraft'.
    /// سحب: النقد يدخل الخزينة (receipt) + حركة سحب على خط الائتمان.
    /// سداد: النقد يخرج من الخزينة (payment) + حركة سداد.
    /// يُتحقق من حد الائتمان (loans.credit_limit) قبل السماح بالسحب.
    /// </summary>
    public static class OverdraftService
    {
        public class OverdraftTransaction
        {
            public int Id { get; set; }
            public int LoanId { get; set; }
            public DateTime Date { get; set; }
            public string Type { get; set; } = "";
            public decimal Amount { get; set; }
            public decimal BalanceAfter { get; set; }
            public string Notes { get; set; } = "";
            public string TypeDisplay => Type == "withdrawal" ? "سحب" : Type == "repayment" ? "سداد" : Type;
            public string DateDisplay => Date.ToString("yyyy-MM-dd");
            public string AmountDisplay => Amount.ToString("N2");
            public string BalanceDisplay => BalanceAfter.ToString("N2");
        }

        public static string GetCS() => SecureConnectionManager.GetConnectionString();

        /// <summary>سحب نقدي من خط الائتمان (يدخل الخزينة).</summary>
        public static int Withdraw(int loanId, decimal amount, int cashBoxId, DateTime date, string notes, int userId)
        {
            if (amount <= 0) throw new InvalidOperationException("المبلغ يجب أن يكون أكبر من صفر.");
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                decimal currentBalance = GetUsedBalance(conn, tx, loanId);
                decimal creditLimit = 0;
                using (var cmd = new NpgsqlCommand("SELECT COALESCE(credit_limit,0) FROM loans WHERE id=@id AND loan_type='overdraft'", conn, tx))
                {
                    cmd.Parameters.AddWithValue("id", loanId);
                    var result = cmd.ExecuteScalar();
                    if (result == null || result == DBNull.Value)
                        throw new InvalidOperationException("هذا القرض ليس خط ائتمان (overdraft).");
                    creditLimit = Convert.ToDecimal(result);
                }
                if (currentBalance + amount > creditLimit)
                    throw new InvalidOperationException(
                        "تجاوز الحد الائتماني! الحد الأقصى: " + creditLimit.ToString("N2") +
                        " — المستخدم حاليًا: " + currentBalance.ToString("N2") +
                        " — المطلوب: " + amount.ToString("N2"));

                decimal newBalance = currentBalance + amount;
                int transId;
                using (var cmd = new NpgsqlCommand(
                    "INSERT INTO overdraft_transactions (loan_id, transaction_date, transaction_type, amount, balance_after, notes, created_by, created_at) " +
                    "VALUES (@loanId, @date, 'withdrawal', @amt, @balance, @notes, @user, NOW()) RETURNING id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("loanId", loanId);
                    cmd.Parameters.AddWithValue("date", date);
                    cmd.Parameters.AddWithValue("amt", amount);
                    cmd.Parameters.AddWithValue("balance", newBalance);
                    cmd.Parameters.AddWithValue("notes", (object?)notes ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("user", userId);
                    transId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // النقد يدخل الخزينة (سند قبض حقيقي)
                using (var cmd2 = new NpgsqlCommand(
                    "INSERT INTO payments (date, amount, payment_type, payment_method, reference_number, notes, cash_box_id, currency_id) " +
                    "VALUES (@date, @amt, 'receipt', 'bank_transfer', @ref, @notes, @cb, @cur)", conn, tx))
                {
                    cmd2.Parameters.AddWithValue("date", date);
                    cmd2.Parameters.AddWithValue("amt", amount);
                    cmd2.Parameters.AddWithValue("ref", "OD-WD-" + transId);
                    cmd2.Parameters.AddWithValue("notes", "سحب على خط ائتمان (سحب #" + transId + ")");
                    cmd2.Parameters.AddWithValue("cb", cashBoxId);
                    cmd2.Parameters.AddWithValue("cur", DbDefaults.GetDefaultCurrencyId(conn));
                    cmd2.ExecuteNonQuery();
                }

                AuditService.Log("OVERDRAFT_WITHDRAW", "Loan", loanId, "Withdrawal " + amount.ToString("N2") + " -> cash box " + cashBoxId);
                tx.Commit();
                return transId;
            }
            catch { try { tx.Rollback(); } catch { } throw; }
        }

        /// <summary>سداد نقدي لخط الائتمان (يخرج من الخزينة).</summary>
        public static int Repay(int loanId, decimal amount, int cashBoxId, DateTime date, string notes, int userId)
        {
            if (amount <= 0) throw new InvalidOperationException("المبلغ يجب أن يكون أكبر من صفر.");
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                decimal currentBalance = GetUsedBalance(conn, tx, loanId);
                if (amount > currentBalance)
                    throw new InvalidOperationException("مبلغ السداد أكبر من الرصيد المستخدم (" + currentBalance.ToString("N2") + ").");

                decimal newBalance = currentBalance - amount;
                int transId;
                using (var cmd = new NpgsqlCommand(
                    "INSERT INTO overdraft_transactions (loan_id, transaction_date, transaction_type, amount, balance_after, notes, created_by, created_at) " +
                    "VALUES (@loanId, @date, 'repayment', @amt, @balance, @notes, @user, NOW()) RETURNING id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("loanId", loanId);
                    cmd.Parameters.AddWithValue("date", date);
                    cmd.Parameters.AddWithValue("amt", amount);
                    cmd.Parameters.AddWithValue("balance", newBalance);
                    cmd.Parameters.AddWithValue("notes", (object?)notes ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("user", userId);
                    transId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                using (var cmd2 = new NpgsqlCommand(
                    "INSERT INTO payments (date, amount, payment_type, payment_method, reference_number, notes, cash_box_id, currency_id) " +
                    "VALUES (@date, @amt, 'payment', 'bank_transfer', @ref, @notes, @cb, @cur)", conn, tx))
                {
                    cmd2.Parameters.AddWithValue("date", date);
                    cmd2.Parameters.AddWithValue("amt", amount);
                    cmd2.Parameters.AddWithValue("ref", "OD-RP-" + transId);
                    cmd2.Parameters.AddWithValue("notes", "سداد خط ائتمان (سداد #" + transId + ")");
                    cmd2.Parameters.AddWithValue("cb", cashBoxId);
                    cmd2.Parameters.AddWithValue("cur", DbDefaults.GetDefaultCurrencyId(conn));
                    cmd2.ExecuteNonQuery();
                }

                AuditService.Log("OVERDRAFT_REPAY", "Loan", loanId, "Repayment " + amount.ToString("N2") + " <- cash box " + cashBoxId);
                tx.Commit();
                return transId;
            }
            catch { try { tx.Rollback(); } catch { } throw; }
        }

        /// <summary>الرصيد المستخدم من خط الائتمان.</summary>
        public static decimal GetUsedBalance(NpgsqlConnection conn, NpgsqlTransaction tx, int loanId)
        {
            using var cmd = new NpgsqlCommand(
                "SELECT COALESCE(SUM(CASE WHEN transaction_type='withdrawal' THEN amount ELSE -amount END),0) FROM overdraft_transactions WHERE loan_id=@id",
                conn, tx);
            cmd.Parameters.AddWithValue("id", loanId);
            return Convert.ToDecimal(cmd.ExecuteScalar());
        }

        public static decimal GetUsedBalance(int loanId)
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            return GetUsedBalance(conn, null, loanId);
        }

        public static List<OverdraftTransaction> GetTransactions(int loanId)
        {
            var list = new List<OverdraftTransaction>();
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var cmd = new NpgsqlCommand(
                "SELECT id, loan_id, transaction_date, transaction_type, amount, balance_after, COALESCE(notes,'') FROM overdraft_transactions WHERE loan_id=@id ORDER BY id DESC",
                conn);
            cmd.Parameters.AddWithValue("id", loanId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new OverdraftTransaction
                {
                    Id = r.GetInt32(0),
                    LoanId = r.GetInt32(1),
                    Date = DbValue.AsDateTime(r.GetValue(2)),
                    Type = r.GetString(3),
                    Amount = Convert.ToDecimal(r.GetValue(4)),
                    BalanceAfter = Convert.ToDecimal(r.GetValue(5)),
                    Notes = r.GetString(6)
                });
            return list;
        }
    }
}
