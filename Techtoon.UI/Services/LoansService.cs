using System;
using System.Collections.Generic;
using Npgsql;

namespace Techtoon.UI.Services
{
    public class Loan
    {
        public int Id { get; set; }
        public string LoanName { get; set; }
        public string ContractNumber { get; set; }
        public string LoanType { get; set; }
        public int? BankAccountId { get; set; }
        public string BankName { get; set; }
        public double PrincipalAmount { get; set; }
        public DateTime DisbursementDate { get; set; }
        public DateTime? FirstPaymentDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int NumInstallments { get; set; }
        public string PaymentFrequency { get; set; }
        public double InterestRate { get; set; }
        public string InterestType { get; set; }
        public string InterestCalcMethod { get; set; }
        public int GracePeriod { get; set; }
        public double AdminFees { get; set; }
        public double InsuranceAmount { get; set; }
        public double CreditLimit { get; set; }
        public string DayCountBasis { get; set; }
        public int? LiabilityAccountId { get; set; }
        public int? InterestExpenseAccountId { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        public string StatusDisplay { get { return Status switch { "draft" => "مسودة", "active" => "ساري", "closed" => "مغلق", "defaulted" => "متعثر", _ => Status }; } }
        public string LoanTypeDisplay { get { return LoanType == "overdraft" ? "جاري مدين" : "قرض أقساط"; } }
        public string PrincipalDisplay { get { return PrincipalAmount.ToString("N2"); } }
    }

    public class LoanInstallment
    {
        public int Id { get; set; }
        public int LoanId { get; set; }
        public int InstallmentNumber { get; set; }
        public DateTime DueDate { get; set; }
        public double PrincipalAmount { get; set; }
        public double InterestAmount { get; set; }
        public double FeesAmount { get; set; }
        public double TotalAmount { get; set; }
        public double RemainingBalance { get; set; }
        public double PaidAmount { get; set; }
        public DateTime? PaidDate { get; set; }
        public double PenaltyAmount { get; set; }
        public string Status { get; set; }

        public string StatusDisplay { get { return Status switch { "upcoming" => "قادم", "due" => "مستحق", "partially_paid" => "مدفوع جزئياً", "paid" => "مدفوع", "late" => "متأخر", _ => Status }; } }
        public string DueDateDisplay { get { return DueDate.ToString("yyyy-MM-dd"); } }
        public string PrincipalDisplay { get { return PrincipalAmount.ToString("N2"); } }
        public string InterestDisplay { get { return InterestAmount.ToString("N2"); } }
        public string TotalDisplay { get { return TotalAmount.ToString("N2"); } }
        public string RemainingDisplay { get { return RemainingBalance.ToString("N2"); } }
        public string PaidDisplay { get { return PaidAmount.ToString("N2"); } }
    }

    public static class LoansService
    {
        private static string GetCS() => SecureConnectionManager.GetConnectionString();

        public static int CreateLoan(Loan loan, int userId)
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                int loanId;
                using (var cmd = new NpgsqlCommand(
                    "INSERT INTO loans (borrower_name, contract_number, loan_number, loan_type, bank_account_id, principal_amount, " +
                    "loan_date, first_payment_date, end_date, num_installments, payment_frequency, " +
                    "interest_rate, interest_type, interest_calc_method, grace_period, admin_fees, insurance_amount, " +
                    "credit_limit, day_count_basis, liability_account_id, interest_expense_account_id, status, notes, created_by, created_at, " +
                    "borrower_type, maturity_date, term_months, installment_amount, total_interest, total_amount, paid_amount, remaining_amount, cash_box_id) " +
                    "VALUES (@name, @contract, @loanNum, @type, @bank, @principal, @disburse, @firstPay, @end, @num, @freq, " +
                    "@rate, @intType, @intMethod, @grace, @admin, @ins, @limit, @dcb, @lia, @intExp, @status, @notes, @user, NOW(), " +
                    "@borrowerType, @maturity, @termMonths, @instAmt, @totalInt, @totalAmt, @paidAmt, @remainingAmt, @cashBoxId) RETURNING id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("name", loan.LoanName ?? "");
                    cmd.Parameters.AddWithValue("contract", loan.ContractNumber ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("loanNum", "LOAN-" + DateTime.Now.ToString("yyyyMMddHHmmss"));
                    cmd.Parameters.AddWithValue("type", loan.LoanType ?? "installment");
                    cmd.Parameters.AddWithValue("bank", loan.BankAccountId.HasValue ? (object)loan.BankAccountId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("principal", loan.PrincipalAmount);
                    cmd.Parameters.AddWithValue("disburse", loan.DisbursementDate);
                    cmd.Parameters.AddWithValue("firstPay", loan.FirstPaymentDate.HasValue ? (object)loan.FirstPaymentDate.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("end", loan.EndDate.HasValue ? (object)loan.EndDate.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("num", loan.NumInstallments);
                    cmd.Parameters.AddWithValue("freq", loan.PaymentFrequency ?? "monthly");
                    cmd.Parameters.AddWithValue("rate", loan.InterestRate);
                    cmd.Parameters.AddWithValue("intType", loan.InterestType ?? "fixed");
                    cmd.Parameters.AddWithValue("intMethod", loan.InterestCalcMethod ?? "declining");
                    cmd.Parameters.AddWithValue("grace", loan.GracePeriod);
                    cmd.Parameters.AddWithValue("admin", loan.AdminFees);
                    cmd.Parameters.AddWithValue("ins", loan.InsuranceAmount);
                    cmd.Parameters.AddWithValue("limit", loan.CreditLimit);
                    cmd.Parameters.AddWithValue("dcb", loan.DayCountBasis ?? "365");
                    cmd.Parameters.AddWithValue("lia", loan.LiabilityAccountId.HasValue ? (object)loan.LiabilityAccountId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("intExp", loan.InterestExpenseAccountId.HasValue ? (object)loan.InterestExpenseAccountId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("status", loan.Status ?? "draft");
                    cmd.Parameters.AddWithValue("notes", loan.Notes ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("user", userId);
                    cmd.Parameters.AddWithValue("borrowerType", "company");
                    cmd.Parameters.AddWithValue("maturity", loan.EndDate ?? (DateTime?)DateTime.Now.AddMonths(loan.NumInstallments > 0 ? loan.NumInstallments : 12));
                    cmd.Parameters.AddWithValue("termMonths", loan.NumInstallments);
                    cmd.Parameters.AddWithValue("instAmt", 0);
                    cmd.Parameters.AddWithValue("totalInt", 0);
                    cmd.Parameters.AddWithValue("totalAmt", loan.PrincipalAmount);
                    cmd.Parameters.AddWithValue("paidAmt", 0);
                    cmd.Parameters.AddWithValue("remainingAmt", loan.PrincipalAmount);
                    cmd.Parameters.AddWithValue("cashBoxId", loan.BankAccountId ?? 0);
                    loanId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                if (loan.LoanType == "installment" && loan.NumInstallments > 0)
                {
                    GenerateSchedule(conn, tx, loanId, loan);
                }

                AuthService.LogAudit("CREATE_LOAN", "loan", loanId, "Loan created: " + loan.LoanName);
                tx.Commit();
                return loanId;
            }
            catch { tx.Rollback(); throw; }
        }

        private static void GenerateSchedule(NpgsqlConnection conn, NpgsqlTransaction tx, int loanId, Loan loan)
        {
            double principal = loan.PrincipalAmount;
            double rate = loan.InterestRate / 100.0;
            int n = loan.NumInstallments;
            
            double installmentAmount = 0;
            if (loan.InterestCalcMethod == "flat")
            {
                installmentAmount = (principal + (principal * rate * n / 12)) / n;
            }
            else
            {
                double r = rate / 12;
                installmentAmount = principal * (r * Math.Pow(1 + r, n)) / (Math.Pow(1 + r, n) - 1);
            }

            DateTime dueDate = loan.FirstPaymentDate ?? loan.DisbursementDate.AddMonths(1);
            double remainingBalance = principal;

            for (int i = 1; i <= n; i++)
            {
                double interest = 0;
                double principalPart = 0;

                if (loan.InterestCalcMethod == "flat")
                {
                    interest = (principal * rate * n / 12) / n;
                    principalPart = installmentAmount - interest;
                }
                else
                {
                    interest = remainingBalance * (rate / 12);
                    principalPart = installmentAmount - interest;
                }

                remainingBalance -= principalPart;

                using var cmd = new NpgsqlCommand(
                    "INSERT INTO loan_installments (loan_id, installment_number, due_date, " +
                    "principal_amount, interest_amount, fees_amount, total_amount, remaining_balance, paid_amount, status) " +
                    "VALUES (@loanId, @num, @due, @principal, @interest, @fees, @total, @remaining, 0, 'upcoming')", conn, tx);
                
                cmd.Parameters.AddWithValue("loanId", loanId);
                cmd.Parameters.AddWithValue("num", i);
                cmd.Parameters.AddWithValue("due", dueDate);
                cmd.Parameters.AddWithValue("principal", principalPart);
                cmd.Parameters.AddWithValue("interest", interest);
                cmd.Parameters.AddWithValue("fees", 0);
                cmd.Parameters.AddWithValue("total", installmentAmount);
                cmd.Parameters.AddWithValue("remaining", Math.Max(0, remainingBalance));
                cmd.ExecuteNonQuery();

                dueDate = loan.PaymentFrequency switch
                {
                    "monthly" => dueDate.AddMonths(1),
                    "quarterly" => dueDate.AddMonths(3),
                    "yearly" => dueDate.AddYears(1),
                    "weekly" => dueDate.AddDays(7),
                    _ => dueDate.AddMonths(1)
                };
            }
        }

        public static void UpdateLoanStatus(int loanId, string status)
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var cmd = new NpgsqlCommand("UPDATE loans SET status=@s WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("s", status);
            cmd.Parameters.AddWithValue("id", loanId);
            cmd.ExecuteNonQuery();
        }

        public static void RecordRepayment(int installmentId, double amount, DateTime payDate, int userId)
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                double totalDue, paidSoFar;
                int loanId;
                using (var cmd = new NpgsqlCommand("SELECT total_amount, paid_amount, loan_id FROM loan_installments WHERE id=@id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("id", installmentId);
                    using var r = cmd.ExecuteReader();
                    if (!r.Read()) throw new InvalidOperationException("Installment not found");
                    totalDue = r.GetDouble(0);
                    paidSoFar = r.GetDouble(1);
                    loanId = r.GetInt32(2);
                }

                double newPaid = paidSoFar + amount;
                string status = newPaid >= totalDue ? "paid" : "partially_paid";

                using var cmd2 = new NpgsqlCommand(
                    "UPDATE loan_installments SET paid_amount=@paid, paid_date=@date, status=@status WHERE id=@id", conn, tx);
                cmd2.Parameters.AddWithValue("paid", newPaid);
                cmd2.Parameters.AddWithValue("date", payDate);
                cmd2.Parameters.AddWithValue("status", status);
                cmd2.Parameters.AddWithValue("id", installmentId);
                cmd2.ExecuteNonQuery();

                using var cmd3 = new NpgsqlCommand(
                    "INSERT INTO payments (date, amount, payment_type, payment_method, reference_number, notes, currency_id, cash_box_id, account_id) " +
                    "VALUES (@date, @amt, 'payment', 'تحويل بنكي', @ref, @notes, 1, NULL, (SELECT bank_account_id FROM loans WHERE id=@loanId))", conn, tx);
                cmd3.Parameters.AddWithValue("date", payDate);
                cmd3.Parameters.AddWithValue("amt", amount);
                cmd3.Parameters.AddWithValue("ref", "LOAN-" + loanId + "-INST-" + installmentId);
                cmd3.Parameters.AddWithValue("notes", "Loan repayment");
                cmd3.Parameters.AddWithValue("loanId", loanId);
                cmd3.ExecuteNonQuery();

                AuthService.LogAudit("LOAN_REPAYMENT", "loan", loanId, "Repayment: " + amount);
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        public static void DeleteLoan(int loanId)
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                using (var cmd = new NpgsqlCommand("DELETE FROM loan_installments WHERE loan_id=@id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("id", loanId);
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = new NpgsqlCommand("DELETE FROM overdraft_transactions WHERE loan_id=@id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("id", loanId);
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = new NpgsqlCommand("DELETE FROM loans WHERE id=@id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("id", loanId);
                    cmd.ExecuteNonQuery();
                }
                AuthService.LogAudit("DELETE_LOAN", "loan", loanId, "Loan deleted");
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        public static void UpdateLoan(Loan loan)
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var cmd = new NpgsqlCommand(
                "UPDATE loans SET borrower_name=@name, contract_number=@contract, interest_rate=@rate, " +
                "num_installments=@num, first_payment_date=@firstPay, end_date=@end, " +
                "admin_fees=@admin, insurance_amount=@ins, credit_limit=@limit, notes=@notes, status=@status " +
                "WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", loan.Id);
            cmd.Parameters.AddWithValue("name", loan.LoanName ?? "");
            cmd.Parameters.AddWithValue("contract", loan.ContractNumber ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("rate", loan.InterestRate);
            cmd.Parameters.AddWithValue("num", loan.NumInstallments);
            cmd.Parameters.AddWithValue("firstPay", loan.FirstPaymentDate.HasValue ? (object)loan.FirstPaymentDate.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("end", loan.EndDate.HasValue ? (object)loan.EndDate.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("admin", loan.AdminFees);
            cmd.Parameters.AddWithValue("ins", loan.InsuranceAmount);
            cmd.Parameters.AddWithValue("limit", loan.CreditLimit);
            cmd.Parameters.AddWithValue("notes", loan.Notes ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("status", loan.Status ?? "active");
            cmd.ExecuteNonQuery();
            AuthService.LogAudit("UPDATE_LOAN", "loan", loan.Id, "Loan updated: " + loan.LoanName);
        }

        public static List<Loan> GetAllLoans(string typeFilter = null)
        {
            var list = new List<Loan>();
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            var sql = "SELECT l.id, COALESCE(l.borrower_name, l.loan_number, '') AS loan_name, l.contract_number, l.loan_type, " +
                      "l.bank_account_id, COALESCE(c.name, '') AS bank_name, l.principal_amount, " +
                      "COALESCE(l.loan_date, l.created_at) AS disbursement_date, l.first_payment_date, l.end_date, " +
                      "l.num_installments, l.payment_frequency, l.interest_rate, l.interest_type, l.interest_calc_method, " +
                      "l.grace_period, l.admin_fees, l.insurance_amount, l.credit_limit, l.day_count_basis, " +
                      "l.liability_account_id, l.interest_expense_account_id, l.status, l.notes, l.created_by, l.created_at " +
                      "FROM loans l LEFT JOIN cash_boxes c ON l.bank_account_id = c.id";
            if (!string.IsNullOrEmpty(typeFilter)) sql += " WHERE l.loan_type=@t";
            sql += " ORDER BY l.id DESC";
            using var cmd = new NpgsqlCommand(sql, conn);
            if (!string.IsNullOrEmpty(typeFilter)) cmd.Parameters.AddWithValue("t", typeFilter);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new Loan
                {
                    Id = r.GetInt32(0),
                    LoanName = r.GetString(1),
                    ContractNumber = r.IsDBNull(2) ? "" : r.GetString(2),
                    LoanType = r.GetString(3),
                    BankAccountId = r.IsDBNull(4) ? (int?)null : r.GetInt32(4),
                    BankName = r.IsDBNull(5) ? "" : r.GetString(5),
                    PrincipalAmount = r.GetDouble(6),
                    DisbursementDate = r.GetDateTime(7),
                    FirstPaymentDate = r.IsDBNull(8) ? (DateTime?)null : r.GetDateTime(8),
                    EndDate = r.IsDBNull(9) ? (DateTime?)null : r.GetDateTime(9),
                    NumInstallments = r.IsDBNull(10) ? 0 : r.GetInt32(10),
                    PaymentFrequency = r.IsDBNull(11) ? "" : r.GetString(11),
                    InterestRate = r.GetDouble(12),
                    InterestType = r.IsDBNull(13) ? "" : r.GetString(13),
                    InterestCalcMethod = r.IsDBNull(14) ? "" : r.GetString(14),
                    GracePeriod = r.IsDBNull(15) ? 0 : r.GetInt32(15),
                    AdminFees = r.GetDouble(16),
                    InsuranceAmount = r.GetDouble(17),
                    CreditLimit = r.GetDouble(18),
                    DayCountBasis = r.IsDBNull(19) ? "" : r.GetString(19),
                    LiabilityAccountId = r.IsDBNull(20) ? (int?)null : r.GetInt32(20),
                    InterestExpenseAccountId = r.IsDBNull(21) ? (int?)null : r.GetInt32(21),
                    Status = r.GetString(22),
                    Notes = r.IsDBNull(23) ? "" : r.GetString(23),
                    CreatedBy = r.IsDBNull(24) ? (int?)null : r.GetInt32(24),
                    CreatedAt = r.GetDateTime(25)
                });
            }
            return list;
        }

        public static Loan GetById(int id)
        {
            var all = GetAllLoans();
            return all.Find(l => l.Id == id);
        }

        public static List<LoanInstallment> GetSchedule(int loanId)
        {
            var list = new List<LoanInstallment>();
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var cmd = new NpgsqlCommand(
                "SELECT id, loan_id, installment_number, due_date, principal_amount, interest_amount, fees_amount, " +
                "total_amount, remaining_balance, paid_amount, paid_date, penalty_amount, status " +
                "FROM loan_installments WHERE loan_id=@id ORDER BY installment_number", conn);
            cmd.Parameters.AddWithValue("id", loanId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new LoanInstallment
                {
                    Id = r.GetInt32(0),
                    LoanId = r.GetInt32(1),
                    InstallmentNumber = r.GetInt32(2),
                    DueDate = r.GetDateTime(3),
                    PrincipalAmount = r.GetDouble(4),
                    InterestAmount = r.GetDouble(5),
                    FeesAmount = r.GetDouble(6),
                    TotalAmount = r.GetDouble(7),
                    RemainingBalance = r.GetDouble(8),
                    PaidAmount = r.GetDouble(9),
                    PaidDate = r.IsDBNull(10) ? (DateTime?)null : r.GetDateTime(10),
                    PenaltyAmount = r.GetDouble(11),
                    Status = r.GetString(12)
                });
            }
            return list;
        }
    }
}