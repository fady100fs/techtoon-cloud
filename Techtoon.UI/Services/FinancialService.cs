#pragma warning disable CS0162
// ============================================================
// FinancialService.cs - دوال النظام المالي
// ضع هذا الملف في: Techtoon.UI/Services/FinancialService.cs
// ============================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;

namespace Techtoon.UI.Services
{
    public class CashBoxBalance
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal TotalReceipts { get; set; }
        public decimal TotalPayments { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal TotalTransfersIn { get; set; }
        public decimal TotalTransfersOut { get; set; }
        public decimal CurrentBalance { get; set; }
    }

    public class CashFlowReport
    {
        public decimal TotalReceipts { get; set; }
        public decimal TotalPayments { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetCashFlow { get; set; }
        public decimal TotalReceivables { get; set; }
        public decimal TotalPayables { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal OperatingProfit { get; set; }
    }

    public class ShiftClosing
    {
        public int Id { get; set; }
        public int CashBoxId { get; set; }
        public DateTime ShiftDate { get; set; }
        public decimal OpeningAmount { get; set; }
        public decimal ExpectedClosing { get; set; }
        public decimal ActualClosing { get; set; }
        public decimal Difference { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
    }

    public class FinancialService
    {
        public static string GetCS() { return Techtoon.UI.Services.SecureConnectionManager.GetConnectionString(); }

        /// <summary>
        /// حساب رصيد الصندوق الحالي
        /// الرصيد = افتتاحي + قبض - صرف - مصروفات + تحويلات واردة - تحويلات صادرة
        /// </summary>
        public static CashBoxBalance GetCashBoxBalance(int cashBoxId)
        {
            var result = new CashBoxBalance();
            try
            {
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    
                    // 1) بيانات الصندوق
                    using (var cmd = new NpgsqlCommand("SELECT id, name, COALESCE(opening_balance,0) FROM cash_boxes WHERE id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("id", cashBoxId);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                result.Id = r.GetInt32(0);
                                result.Name = r.GetString(1);
                                result.OpeningBalance = Convert.ToDecimal(r.GetValue(2));
                            }
                        }
                    }

                    // 2) إجمالي القبض (payments type='receipt')
                    using (var cmd = new NpgsqlCommand("SELECT COALESCE(SUM(amount),0) FROM payments WHERE cash_box_id=@id AND type='receipt'", conn))
                    {
                        cmd.Parameters.AddWithValue("id", cashBoxId);
                        result.TotalReceipts = Convert.ToDecimal(cmd.ExecuteScalar());
                    }

                    // 3) إجمالي الصرف (payments type='payment')
                    using (var cmd = new NpgsqlCommand("SELECT COALESCE(SUM(amount),0) FROM payments WHERE cash_box_id=@id AND type='payment'", conn))
                    {
                        cmd.Parameters.AddWithValue("id", cashBoxId);
                        result.TotalPayments = Convert.ToDecimal(cmd.ExecuteScalar());
                    }

                    // 4) إجمالي المصروفات
                    using (var cmd = new NpgsqlCommand("SELECT COALESCE(SUM(amount),0) FROM expenses WHERE cash_box_id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("id", cashBoxId);
                        result.TotalExpenses = Convert.ToDecimal(cmd.ExecuteScalar());
                    }

                    // 5) تحويلات واردة
                    using (var cmd = new NpgsqlCommand("SELECT COALESCE(SUM(amount),0) FROM cash_transfers WHERE to_cash_box_id=@id AND status='completed'", conn))
                    {
                        cmd.Parameters.AddWithValue("id", cashBoxId);
                        result.TotalTransfersIn = Convert.ToDecimal(cmd.ExecuteScalar());
                    }

                    // 6) تحويلات صادرة
                    using (var cmd = new NpgsqlCommand("SELECT COALESCE(SUM(amount),0) FROM cash_transfers WHERE from_cash_box_id=@id AND status='completed'", conn))
                    {
                        cmd.Parameters.AddWithValue("id", cashBoxId);
                        result.TotalTransfersOut = Convert.ToDecimal(cmd.ExecuteScalar());
                    }

                    // 7) الرصيد الحالي
                    result.CurrentBalance = result.OpeningBalance + result.TotalReceipts 
                        - result.TotalPayments - result.TotalExpenses 
                        + result.TotalTransfersIn - result.TotalTransfersOut;
                }
            }
            catch { }
            return result;
        }

        /// <summary>
        /// تحويل بين الصناديق (Transaction)
        /// </summary>
        public static async Task<bool> TransferBetweenCashBoxesAsync(int fromId, int toId, decimal amount, string notes)
        {
            if (amount <= 0 || fromId == toId) return false;
            try
            {
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var tran = conn.BeginTransaction())
                    {
                        try
                        {
                            // تحقق من الرصيد
                            var balance = GetCashBoxBalance(fromId);
                            if (balance.CurrentBalance < amount)
                            {
                                return false; // رصيد غير كافٍ
                            }

                            // سجل التحويل
                            using (var cmd = new NpgsqlCommand(@"
                                INSERT INTO cash_transfers (date, from_cash_box_id, to_cash_box_id, amount, notes, status, created_at)
                                VALUES (NOW(), @from, @to, @amt, @notes, 'completed', NOW())", conn, tran))
                            {
                                cmd.Parameters.AddWithValue("from", fromId);
                                cmd.Parameters.AddWithValue("to", toId);
                                cmd.Parameters.AddWithValue("amt", amount);
                                cmd.Parameters.AddWithValue("notes", (object)notes ?? DBNull.Value);
                                cmd.ExecuteNonQuery();
                            }

                            tran.Commit();
                            return true;
                        }
                        catch
                        {
                            try { tran.Rollback(); } catch { }
                            throw;
                        }
                    }
                }
            }
            catch { return false; }
            await Task.CompletedTask;
        }

        /// <summary>
        /// إغلاق وردية صندوق
        /// </summary>
        public static async Task<bool> CloseShiftAsync(int cashBoxId, decimal actualClosing, string notes)
        {
            try
            {
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var tran = conn.BeginTransaction())
                    {
                        try
                        {
                            var balance = GetCashBoxBalance(cashBoxId);
                            decimal expected = balance.CurrentBalance;
                            decimal difference = actualClosing - expected;

                            using (var cmd = new NpgsqlCommand(@"
                                INSERT INTO shift_closings (cash_box_id, shift_date, opening_amount, expected_closing, actual_closing, difference, status, notes, created_at)
                                VALUES (@cbId, NOW(), @opening, @expected, @actual, @diff, 'closed', @notes, NOW())", conn, tran))
                            {
                                cmd.Parameters.AddWithValue("cbId", cashBoxId);
                                cmd.Parameters.AddWithValue("opening", balance.OpeningBalance);
                                cmd.Parameters.AddWithValue("expected", expected);
                                cmd.Parameters.AddWithValue("actual", actualClosing);
                                cmd.Parameters.AddWithValue("diff", difference);
                                cmd.Parameters.AddWithValue("notes", (object)notes ?? DBNull.Value);
                                cmd.ExecuteNonQuery();
                            }

                            // لو فيه عجز/زيادة، سجل تسوية
                            if (difference != 0)
                            {
                                using (var cmd = new NpgsqlCommand(@"
                                    INSERT INTO expenses (date, category_id, amount, description, payment_method, cash_box_id, notes)
                                    VALUES (NOW(), (SELECT id FROM expense_categories WHERE name='\u062A\u0633\u0648\u064A\u0629 \u0639\u062F\u0645/\u0632\u064A\u0627\u062F\u0629' LIMIT 1), @amt, '\u062A\u0633\u0648\u064A\u0629 \u0639\u062F\u0645/\u0632\u064A\u0627\u062F\u0629', 'adjustment', @cbId, @notes)", conn, tran))
                                {
                                    cmd.Parameters.AddWithValue("amt", Math.Abs(difference));
                                    cmd.Parameters.AddWithValue("cbId", cashBoxId);
                                    cmd.Parameters.AddWithValue("notes", (object)notes ?? DBNull.Value);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            tran.Commit();
                            return true;
                        }
                        catch
                        {
                            try { tran.Rollback(); } catch { }
                            throw;
                        }
                    }
                }
            }
            catch { return false; }
            await Task.CompletedTask;
        }

        /// <summary>
        /// تقرير التدفق النقدي
        /// </summary>
        public static CashFlowReport GetCashFlowReport(DateTime fromDate, DateTime toDate)
        {
            var report = new CashFlowReport();
            try
            {
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();

                    // المقبوضات
                    using (var cmd = new NpgsqlCommand("SELECT COALESCE(SUM(amount),0) FROM payments WHERE type='receipt' AND date BETWEEN @from AND @to", conn))
                    {
                        cmd.Parameters.AddWithValue("from", fromDate);
                        cmd.Parameters.AddWithValue("to", toDate);
                        report.TotalReceipts = Convert.ToDecimal(cmd.ExecuteScalar());
                    }

                    // المدفوعات
                    using (var cmd = new NpgsqlCommand("SELECT COALESCE(SUM(amount),0) FROM payments WHERE type='payment' AND date BETWEEN @from AND @to", conn))
                    {
                        cmd.Parameters.AddWithValue("from", fromDate);
                        cmd.Parameters.AddWithValue("to", toDate);
                        report.TotalPayments = Convert.ToDecimal(cmd.ExecuteScalar());
                    }

                    // المصروفات
                    using (var cmd = new NpgsqlCommand("SELECT COALESCE(SUM(amount),0) FROM expenses WHERE date BETWEEN @from AND @to", conn))
                    {
                        cmd.Parameters.AddWithValue("from", fromDate);
                        cmd.Parameters.AddWithValue("to", toDate);
                        report.TotalExpenses = Convert.ToDecimal(cmd.ExecuteScalar());
                    }

                    // صافي التدفق
                    report.NetCashFlow = report.TotalReceipts - report.TotalPayments - report.TotalExpenses;

                    // المستحقات للعملاء (فواتير مبيعات غير مدفوعة)
                    using (var cmd = new NpgsqlCommand("SELECT COALESCE(SUM(total - paid_amount),0) FROM invoices WHERE type='sales' AND paid_amount < total", conn))
                    {
                        report.TotalReceivables = Convert.ToDecimal(cmd.ExecuteScalar());
                    }

                    // المستحقات للموردين (فواتير مشتريات غير مدفوعة)
                    using (var cmd = new NpgsqlCommand("SELECT COALESCE(SUM(total - paid_amount),0) FROM invoices WHERE type='purchase' AND paid_amount < total", conn))
                    {
                        report.TotalPayables = Convert.ToDecimal(cmd.ExecuteScalar());
                    }

                    // الربح الإجمالي = مبيعات - تكلفة البضاعة المباعة
                    using (var cmd = new NpgsqlCommand(@"
                        SELECT COALESCE(SUM(il.total),0) - COALESCE(SUM(il.quantity * il.cost_price),0)
                        FROM invoice_lines il
                        JOIN invoices i ON il.invoice_id = i.id
                        WHERE i.type='sales' AND i.date BETWEEN @from AND @to", conn))
                    {
                        cmd.Parameters.AddWithValue("from", fromDate);
                        cmd.Parameters.AddWithValue("to", toDate);
                        report.GrossProfit = Convert.ToDecimal(cmd.ExecuteScalar());
                    }

                    // الربح التشغيلي = الربح الإجمالي - المصروفات
                    report.OperatingProfit = report.GrossProfit - report.TotalExpenses;
                }
            }
            catch { }
            return report;
        }
    }
}
