using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Npgsql;
using Techtoon.UI.Services;

namespace Techtoon.UI.ViewModels
{
    // ================= سلف الموظفين =================
    public class AdvanceRow : ViewModelBase
    {
        public int Id { get; set; }
        public string EmployeeName { get; set; } = "";
        public decimal Amount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Remaining => Amount - PaidAmount;
        public DateTime AdvanceDate { get; set; }
        public string CustodyDisplay { get; set; } = "";
        public string StatusDisplay { get; set; } = "";
        public string AmountDisplay => Amount.ToString("N2");
        public string PaidDisplay => PaidAmount.ToString("N2");
        public string RemainingDisplay => Remaining.ToString("N2");
        public string DateDisplay => AdvanceDate.ToString("yyyy-MM-dd");
    }

    // ================= قروض المنشأة =================
    public class LoanRow : ViewModelBase
    {
        public int Id { get; set; }
        public string LoanNumber { get; set; } = "";
        public string Lender { get; set; } = "";
        public string LoanType { get; set; } = "";
        public decimal Principal { get; set; }
        public decimal Rate { get; set; }
        public decimal Installment { get; set; }
        public decimal Paid { get; set; }
        public string Status { get; set; } = "";
        public decimal Remaining => Principal - Paid;
        public string TypeDisplay => TypeName(LoanType);
        public string StatusDisplay => Status == "closed" ? "مغلق" : "نشط";
        public string PrincipalDisplay => Principal.ToString("N2");
        public string RateDisplay => Rate.ToString("0.##") + "%";
        public string InstallmentDisplay => Installment.ToString("N2");
        public string PaidDisplay => Paid.ToString("N2");
        public string RemainingDisplay => Remaining.ToString("N2");

        public static string TypeName(string t) => t switch
        {
            "installment_fixed" => "أقساط - فائدة ثابتة",
            "installment_variable" => "أقساط - فائدة متغيرة",
            "running_interest" => "جارٍ مدين - فائدة فقط",
            "interest_free" => "بدون فائدة",
            "overdraft" => "تسهيل ائتماني (سحب على البنك)",
            _ => t ?? ""
        };
    }

    public class LoansViewModel : ViewModelBase
    {
        public ObservableCollection<AdvanceRow> Advances { get; } = new();
        public ObservableCollection<LoanRow> Loans { get; } = new();

        private AdvanceRow _selAdv;
        public AdvanceRow SelAdv { get => _selAdv; set => SetProperty(ref _selAdv, value); }
        private LoanRow _selLoan;
        public LoanRow SelLoan { get => _selLoan; set => SetProperty(ref _selLoan, value); }

        private string _searchText = "";
        public string SearchText { get => _searchText; set { SetProperty(ref _searchText, value); _ = LoadAdvancesAsync(); } }
        private string _loanSearchText = "";
        public string LoanSearchText { get => _loanSearchText; set { SetProperty(ref _loanSearchText, value); _ = LoadLoansAsync(); } }

        private string _advStatus = "";
        public string AdvStatus { get => _advStatus; set => SetProperty(ref _advStatus, value); }
        private string _loanStatus = "";
        public string LoanStatus { get => _loanStatus; set => SetProperty(ref _loanStatus, value); }

        public ICommand RefreshAdvancesCmd { get; }
        public ICommand AddAdvanceCmd { get; }
        public ICommand EditAdvanceCmd { get; }
        public ICommand RepayAdvanceCmd { get; }
        public ICommand DeleteAdvanceCmd { get; }

        public ICommand RefreshLoansCmd { get; }
        public ICommand AddLoanCmd { get; }
        public ICommand ScheduleLoanCmd { get; }
        public ICommand EditLoanCmd { get; }
        public ICommand CloseLoanCmd { get; }
        public ICommand DeleteLoanCmd { get; }

        public LoansViewModel()
        {
            RefreshAdvancesCmd = new UiRelayCommand(_ => _ = LoadAdvancesAsync());
            AddAdvanceCmd = new UiRelayCommand(_ => OpenAdvance(0));
            EditAdvanceCmd = new UiRelayCommand(_ => OpenAdvance(SelAdv?.Id ?? 0), _ => SelAdv != null);
            RepayAdvanceCmd = new UiRelayCommand(_ => RepayAdvance(), _ => SelAdv != null);
            DeleteAdvanceCmd = new UiRelayCommand(_ => DeleteAdvance(), _ => SelAdv != null);

            RefreshLoansCmd = new UiRelayCommand(_ => _ = LoadLoansAsync());
            AddLoanCmd = new UiRelayCommand(_ => OpenLoan(0));
            ScheduleLoanCmd = new UiRelayCommand(_ => OpenSchedule(), _ => SelLoan != null);
            EditLoanCmd = new UiRelayCommand(_ => OpenLoan(SelLoan?.Id ?? 0), _ => SelLoan != null);
            CloseLoanCmd = new UiRelayCommand(_ => CloseLoanRunner(), _ => SelLoan != null);
            DeleteLoanCmd = new UiRelayCommand(_ => DeleteLoan(), _ => SelLoan != null);

            _ = LoadAdvancesAsync();
            _ = LoadLoansAsync();
        }

        public static string GetCS() => SecureConnectionManager.GetConnectionString();

        // ============ تحميل سلف الموظفين ============
        public async Task LoadAdvancesAsync()
        {
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                await conn.OpenAsync();
                string sql = @"SELECT l.id,
                       COALESCE((SELECT e.full_name FROM employees e WHERE e.id = l.employee_id), COALESCE(l.borrower_name,'')) AS emp,
                       COALESCE(l.principal_amount,0), COALESCE(l.paid_amount,0), COALESCE(l.loan_date, NOW()),
                       COALESCE((SELECT COALESCE(c.custody_number, 'عهدة #' || c.id) FROM petty_cash_custodies c WHERE c.id = l.custody_id), '') AS custody,
                       COALESCE(l.status,'active')
                       FROM loans l WHERE COALESCE(l.borrower_type,'') = 'employee'";
                if (!string.IsNullOrWhiteSpace(SearchText)) sql += " AND (l.borrower_name ILIKE @q OR COALESCE(l.notes,'') ILIKE @q)";
                sql += " ORDER BY l.loan_date DESC LIMIT 500";
                using var cmd = new NpgsqlCommand(sql, conn);
                if (!string.IsNullOrWhiteSpace(SearchText)) cmd.Parameters.AddWithValue("q", "%" + SearchText + "%");
                Advances.Clear();
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    var amount = Convert.ToDecimal(r.GetValue(2));
                    var paid = Convert.ToDecimal(r.GetValue(3));
                    var rawStatus = r.GetString(6);
                    Advances.Add(new AdvanceRow
                    {
                        Id = r.GetInt32(0),
                        EmployeeName = r.GetString(1),
                        Amount = amount,
                        PaidAmount = paid,
                        AdvanceDate = r.IsDBNull(4) ? DateTime.MinValue : (DateTime)r.GetValue(4),
                        CustodyDisplay = r.GetString(5),
                        StatusDisplay = paid >= amount && amount > 0 ? "مغلق" : (rawStatus == "closed" ? "مغلق" : "نشطة")
                    });
                }
                AdvStatus = "تم التحميل: " + Advances.Count + " سلفة";
            }
            catch (Exception ex) { AdvStatus = "خطأ: " + ex.Message; }
        }

        // ============ تحميل قروض المنشأة ============
        public async Task LoadLoansAsync()
        {
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                await conn.OpenAsync();
                string sql = @"SELECT id, COALESCE(loan_number,''), COALESCE(borrower_name,''), COALESCE(loan_type,''),
                       COALESCE(principal_amount,0), COALESCE(interest_rate,0), COALESCE(installment_amount,0), COALESCE(paid_amount,0),
                       COALESCE(status,'active') FROM loans WHERE COALESCE(borrower_type,'') <> 'employee'";
                if (!string.IsNullOrWhiteSpace(LoanSearchText)) sql += " AND borrower_name ILIKE @q";
                sql += " ORDER BY created_at DESC, id DESC LIMIT 500";
                using var cmd = new NpgsqlCommand(sql, conn);
                if (!string.IsNullOrWhiteSpace(LoanSearchText)) cmd.Parameters.AddWithValue("q", "%" + LoanSearchText + "%");
                Loans.Clear();
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    Loans.Add(new LoanRow
                    {
                        Id = r.GetInt32(0),
                        LoanNumber = r.GetString(1),
                        Lender = r.GetString(2),
                        LoanType = r.GetString(3),
                        Principal = Convert.ToDecimal(r.GetValue(4)),
                        Rate = Convert.ToDecimal(r.GetValue(5)),
                        Installment = Convert.ToDecimal(r.GetValue(6)),
                        Paid = Convert.ToDecimal(r.GetValue(7)),
                        Status = r.GetString(8)
                    });
                }
                LoanStatus = "تم التحميل: " + Loans.Count + " قرض";
            }
            catch (Exception ex) { LoanStatus = "خطأ: " + ex.Message; }
        }

        // ============ فتح نافذة سلفة (إضافة/تعديل) ============
        private void OpenAdvance(int id)
        {
            var win = new Views.EmployeeAdvanceWindow(id);
            win.ShowDialog();
            _ = LoadAdvancesAsync();
        }

        // ============ تسديد (استرداد) سلفة ============
        private void RepayAdvance()
        {
            if (SelAdv == null || SelAdv.Remaining <= 0) { MessageBox.Show("السلفة مسددة بالكامل."); return; }
            var win = new Views.LoanPayWindow(
                referenceNumber: "",
                title: "استرداد سلفة - " + SelAdv.EmployeeName,
                maxAmount: SelAdv.Remaining,
                payType: "receipt",
                refPrefix: "LOAN-ADV-REP:" + SelAdv.Id,
                loanId: SelAdv.Id,
                installmentId: null,
                notes: "استرداد سلفة الموظف " + SelAdv.EmployeeName);
            win.ShowDialog();
            _ = LoadAdvancesAsync();
        }

        // ============ حذف سلفة ============
        private void DeleteAdvance()
        {
            if (SelAdv == null) return;
            if (SelAdv.PaidAmount > 0)
            {
                MessageBox.Show("لا يمكن الحذف: تم استرداد مبلغ (" + SelAdv.PaidAmount.ToString("N2") + ") من السلفة.\nاستخدم التسديد الجزئي حتى الإغلاق بدلًا من الحذف.", "ممنوع", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (MessageBox.Show("حذف سلفة '" + SelAdv.EmployeeName + "'؟\nسيتم أيضًا حذف حركة الصرف المرتبطة بها من الخزينة.", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var tx = conn.BeginTransaction();
                // 1) إلغاء حركة الصرف المرتبطة (ref LOAN-ADV:{id})
                using (var del = new NpgsqlCommand("DELETE FROM payments WHERE reference_number = 'LOAN-ADV:' || @id", conn, tx))
                { del.Parameters.AddWithValue("id", SelAdv.Id); del.ExecuteNonQuery(); }
                // 2) حذف السلفة
                using (var del2 = new NpgsqlCommand("DELETE FROM loans WHERE id = @id AND COALESCE(borrower_type,'')='employee' AND COALESCE(paid_amount,0)=0", conn, tx))
                { del2.Parameters.AddWithValue("id", SelAdv.Id); if (del2.ExecuteNonQuery() == 0) throw new Exception("تعذّر الحذف (ربما تم تسديد جزء منها للتو)."); }
                tx.Commit();
                AuditService.Log("DELETE", "Loan", SelAdv.Id, "Deleted employee advance (closed loop: related cash movement removed)");
                _ = LoadAdvancesAsync();
            }
            catch (Exception ex) { MessageBox.Show("خطأ: " + ex.Message); }
        }

        // ============ فتح نافذة قرض منشأة ============
        private void OpenLoan(int id)
        {
            var win = new Views.EnterpriseLoanWindow(id);
            win.ShowDialog();
            _ = LoadLoansAsync();
            if (win.CreatedLoanId > 0)
            {
                // الحلقة: بعد إنشاء القرض تُفتح نافذة جدول الأقساط تلقائيًا
                new Views.LoanScheduleWindow(win.CreatedLoanId).ShowDialog();
                _ = LoadLoansAsync();
            }
        }

        // ============ جدول الأقساط ============
        private void OpenSchedule()
        {
            new Views.LoanScheduleWindow(SelLoan.Id).ShowDialog();
            _ = LoadLoansAsync();
        }

        // ============ إقفال قرض (تسوية الجارى مدين) ============
        private void CloseLoanRunner()
        {
            if (SelLoan == null) return;
            if (SelLoan.Status == "closed") { MessageBox.Show("القرض مغلق بالفعل."); return; }
            if (SelLoan.LoanType == "running_interest")
            {
                if (MessageBox.Show("إغلاق القرض الجاري مدين يتطلب تسديد كامل رأس المال المتبقي (" + SelLoan.Remaining.ToString("N2") + ") من الخزينة.\nمتابعة؟", "تسوية القرض", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
                var win = new Views.LoanPayWindow(
                    referenceNumber: "",
                    title: "تسوية قرض جارٍ مدين - " + SelLoan.Lender,
                    maxAmount: SelLoan.Remaining,
                    payType: "payment",
                    refPrefix: "LOAN-CLOSE:" + SelLoan.Id,
                    loanId: SelLoan.Id,
                    installmentId: null,
                    notes: "إقفال قرض جارٍ مدين - تسديد رأس المال المتبقي (القرض رقم " + SelLoan.LoanNumber + ")",
                    principalOnly: true,
                    closeOnFull: true);
                win.ShowDialog();
                _ = LoadLoansAsync();
                return;
            }
            if (SelLoan.Remaining > 0) { MessageBox.Show("لا يمكن الإقفال قبل سداد كامل الأقساط (المتبقي " + SelLoan.Remaining.ToString("N2") + ")."); return; }
        }

        // ============ حذف قرض ============
        private void DeleteLoan()
        {
            if (SelLoan == null) return;
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var chk = new NpgsqlCommand("SELECT COUNT(*) FROM loan_payments WHERE loan_id=@id", conn);
                chk.Parameters.AddWithValue("id", SelLoan.Id);
                if (Convert.ToInt64(chk.ExecuteScalar()) > 0)
                {
                    MessageBox.Show("لا يمكن الحذف: القرض عليه تسديدات مسجلة.\nاحذف القرض لن يبقى رصيدًا يتيمًا — استخدام الإقفال بدلًا من الحذف.", "ممنوع", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (MessageBox.Show("حذف القرض '" + SelLoan.Lender + "'؟\nسيُحذف جدول الأقساط وحركة استلام القرض من الخزينة.", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
                using var tx = conn.BeginTransaction();
                using (var del = new NpgsqlCommand("DELETE FROM payments WHERE reference_number = 'LOAN:' || @id", conn, tx))
                { del.Parameters.AddWithValue("id", SelLoan.Id); del.ExecuteNonQuery(); }
                using (var del2 = new NpgsqlCommand("DELETE FROM loans WHERE id=@id", conn, tx))
                { del2.Parameters.AddWithValue("id", SelLoan.Id); del2.ExecuteNonQuery(); }
                tx.Commit();
                AuditService.Log("DELETE", "Loan", SelLoan.Id, "Deleted enterprise loan (installments + cash movement cascaded)");
                _ = LoadLoansAsync();
            }
            catch (Exception ex) { MessageBox.Show("خطأ: " + ex.Message); }
        }
    }
}
