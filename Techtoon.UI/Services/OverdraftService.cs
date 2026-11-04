using System;
using System.Collections.Generic;
using Npgsql;

namespace Techtoon.UI.Services
{
    public class OverdraftTransaction
    {
        public int Id { get; set; }
        public int LoanId { get; set; }
        public DateTime TransactionDate { get; set; }
        public string TransactionType { get; set; } // withdrawal, repayment, interest, fees
        public double Amount { get; set; }
        public double BalanceAfter { get; set; }
        public double InterestAmount { get; set; }
        public string Notes { get; set; }
        public string ReferenceNumber { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        public string TypeDisplay { get { return TransactionType switch { "withdrawal" => "سحب", "repayment" => "سداد", "interest" => "فائدة", "fees" => "رسوم", _ => TransactionType }; } }
        public string AmountDisplay { get { return Amount.ToString("N2"); } }
        public string BalanceDisplay { get { return BalanceAfter.ToString("N2"); } }
        public string DateDisplay { get { return TransactionDate.ToString("yyyy-MM-dd"); } }
    }

    public static class OverdraftService
    {
        private static string GetCS() => SecureConnectionManager.GetConnectionString();

        public static int Withdraw(int loanId, double amount, DateTime date, string notes, int userId)
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                // 1. Get current balance and credit limit
                double currentBalance = GetUsedBalance(conn, tx, loanId);
                double creditLimit = 0;
                using (var cmd = new NpgsqlCommand("SELECT credit_limit FROM loans WHERE id=@id AND loan_type='overdraft'", conn, tx))
                {
                    cmd.Parameters.AddWithValue("id", loanId);
                    var result = cmd.ExecuteScalar();
                    if (result == null) throw new InvalidOperationException("Overdraft facility not found");
                    creditLimit = Convert.ToDouble(result);
                }

                // 2. Check if withdrawal exceeds limit
                if (currentBalance + amount > creditLimit)
                    throw new InvalidOperationException($"تجاوز الحد الائتماني! الحد الأقصى: {creditLimit}, الرصيد المستخدم: {currentBalance}, المطلوب سحبه: {amount}");

                // 3. Record transaction
                double newBalance = currentBalance + amount;
                int transId;
                using (var cmd = new NpgsqlCommand(
                    "INSERT INTO overdraft_transactions (loan_id, transaction_date, transaction_type, amount, balance_after, notes, created_by, created_at) " +
                    "VALUES (@loanId, @date, 'withdrawal', @amt, @balance, @notes, @user, NOW()) RETURNING id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("loanId", loanId);
                    cmd.Parameters.AddWithValue("date", date);
                    cmd.Parameters.AddWithValue("amt", amount);
                    cmd.Parameters.AddWithValue("balance", newBalance);
                    cmd.Parameters.AddWithValue("notes", notes ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("user", userId);
                    transId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // 4. Log in payments table
                using (var cmd2 = new NpgsqlCommand(
                    "INSERT INTO payments (date, amount, payment_type, payment_method, reference_number, notes, currency_id, account_id) " +
                    "VALUES (@date, @amt, 'receipt', 'تحويل بنكي', @ref, @notes, 1, (SELECT bank_account_id FROM loans WHERE id=@loanId))", conn, tx))
                {
                    cmd2.Parameters.AddWithValue("date", date);
                    cmd2.Parameters.AddWithValue("amt", amount);
                    cmd2.Parameters.AddWithValue("ref", "OD-WD-" + transId);
                    cmd2.Parameters.AddWithValue("notes", "Overdraft withdrawal");
                    cmd2.Parameters.AddWithValue("loanId", loanId);
                    cmd2.ExecuteNonQuery();
                }

                AuthService.LogAudit("OVERDRAFT_WITHDRAW", "loan", loanId, "Withdrawal: " + amount);
                tx.Commit();
                return transId;
            }
            catch { tx.Rollback(); throw; }
        }

        public static int Repay(int loanId, double amount, DateTime date, string notes, int userId)
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                double currentBalance = GetUsedBalance(conn, tx, loanId);
                if (amount > currentBalance)
                    throw new InvalidOperationException("مبلغ السداد أكبر من الرصيد المستخدم");

                double newBalance = currentBalance - amount;
                int transId;
                using (var cmd = new NpgsqlCommand(
                    "INSERT INTO overdraft_transactions (loan_id, transaction_date, transaction_type, amount, balance_after, notes, created_by, created_at) " +
                    "VALUES (@loanId, @date, 'repayment', @amt, @balance, @notes, @user, NOW()) RETURNING id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("loanId", loanId);
                    cmd.Parameters.AddWithValue("date", date);
                    cmd.Parameters.AddWithValue("amt", amount);
                    cmd.Parameters.AddWithValue("balance", newBalance);
                    cmd.Parameters.AddWithValue("notes", notes ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("user", userId);
                    transId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // Log in payments table
                using (var cmd2 = new NpgsqlCommand(
                    "INSERT INTO payments (date, amount, payment_type, payment_method, reference_number, notes, currency_id, account_id) " +
                    "VALUES (@date, @amt, 'payment', 'تحويل بنكي', @ref, @notes, 1, (SELECT bank_account_id FROM loans WHERE id=@loanId))", conn, tx))
                {
                    cmd2.Parameters.AddWithValue("date", date);
                    cmd2.Parameters.AddWithValue("amt", amount);
                    cmd2.Parameters.AddWithValue("ref", "OD-REP-" + transId);
                    cmd2.Parameters.AddWithValue("notes", "Overdraft repayment");
                    cmd2.Parameters.AddWithValue("loanId", loanId);
                    cmd2.ExecuteNonQuery();
                }

                AuthService.LogAudit("OVERDRAFT_REPAY", "loan", loanId, "Repayment: " + amount);
                tx.Commit();
                return transId;
            }
            catch { tx.Rollback(); throw; }
        }

        public static double GetUsedBalance(NpgsqlConnection conn, NpgsqlTransaction tx, int loanId)
        {
            using var cmd = new NpgsqlCommand(
                "SELECT COALESCE(SUM(CASE WHEN transaction_type IN ('withdrawal', 'interest', 'fees') THEN amount ELSE -amount END), 0) " +
                "FROM overdraft_transactions WHERE loan_id=@id", conn, tx);
            cmd.Parameters.AddWithValue("id", loanId);
            return Convert.ToDouble(cmd.ExecuteScalar());
        }

        public static double GetAvailableBalance(int loanId)
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            double used = GetUsedBalance(conn, null, loanId);
            double limit = 0;
            using (var cmd = new NpgsqlCommand("SELECT credit_limit FROM loans WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("id", loanId);
                limit = Convert.ToDouble(cmd.ExecuteScalar());
            }
            return limit - used;
        }

        public static List<OverdraftTransaction> GetTransactions(int loanId)
        {
            var list = new List<OverdraftTransaction>();
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var cmd = new NpgsqlCommand(
                "SELECT id, loan_id, transaction_date, transaction_type, amount, balance_after, interest_amount, notes, reference_number, created_by, created_at " +
                "FROM overdraft_transactions WHERE loan_id=@id ORDER BY transaction_date DESC", conn);
            cmd.Parameters.AddWithValue("id", loanId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new OverdraftTransaction
                {
                    Id = r.GetInt32(0),
                    LoanId = r.GetInt32(1),
                    TransactionDate = r.GetDateTime(2),
                    TransactionType = r.GetString(3),
                    Amount = r.GetDouble(4),
                    BalanceAfter = r.GetDouble(5),
                    InterestAmount = r.IsDBNull(6) ? 0 : r.GetDouble(6),
                    Notes = r.IsDBNull(7) ? "" : r.GetString(7),
                    ReferenceNumber = r.IsDBNull(8) ? "" : r.GetString(8),
                    CreatedBy = r.IsDBNull(9) ? (int?)null : r.GetInt32(9),
                    CreatedAt = r.GetDateTime(10)
                });
            }
            return list;
        }
    }
}