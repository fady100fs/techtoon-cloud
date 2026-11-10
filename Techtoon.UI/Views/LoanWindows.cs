using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Npgsql;
using Techtoon.UI.Services;

namespace Techtoon.UI.Views
{
    // ============================================================
    // حاسبة جدول الأقساط — منطق خالص في Techtoon.Core (قابل للاختبار)
    // ============================================================
    public static class LoanScheduleCalc
    {
        public class Row
        {
            public int N;
            public DateTime Due;
            public decimal Principal, Interest, Total;
        }

        public static int PeriodsPerYear(string freq) => Techtoon.Core.Finance.LoanScheduleCalculator.PeriodsPerYear(freq);

        public static List<Row> Build(string loanType, decimal principal, decimal annualRatePct, int count, string freq, DateTime firstDue, int graceMonths)
        {
            return Techtoon.Core.Finance.LoanScheduleCalculator
                .Build(loanType, principal, annualRatePct, count, freq, firstDue, graceMonths)
                .Select(x => new Row { N = x.Number, Due = x.DueDate, Principal = x.Principal, Interest = x.Interest, Total = x.Total })
                .ToList();
        }
    }

    // ============================================================
    // أدوات مشتركة لربط الأقساط بقاعدة البيانات (الحلقة المغلقة)
    // ============================================================
    public static class LoanDbHelpers
    {
        public static int SchedulePeriodsPerYear(string freq) => LoanScheduleCalc.PeriodsPerYear(freq);

        /// <summary>حذف كل الأقساط غير المسددة للقرض وإعادة توليدها + تحديث حقول القرض المالية.</summary>
        public static void RebuildSchedule(NpgsqlConnection conn, NpgsqlTransaction tx, int loanId,
            string loanType, decimal principal, decimal rate, int count, string freq, DateTime first, int grace)
        {
            using (var del = new NpgsqlCommand("DELETE FROM loan_installments WHERE loan_id=@id AND COALESCE(paid_amount,0)=0", conn, tx))
            { del.Parameters.AddWithValue("@id", loanId); del.ExecuteNonQuery(); }
            var rows = LoanScheduleCalc.Build(loanType, principal, rate, count, freq, first, grace);
            decimal totalInterest = rows.Sum(x => x.Interest);
            decimal remBal = principal;
            // لو فيه أقساط مسددة قديمة نكمل الترقيم بعدها
            int firstN = 1;
            using (var c = new NpgsqlCommand("SELECT COALESCE(MAX(installment_number),0) FROM loan_installments WHERE loan_id=@id", conn, tx))
            { c.Parameters.AddWithValue("@id", loanId); firstN = Convert.ToInt32(c.ExecuteScalar()) + 1; }
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                remBal -= row.Principal;
                int n = firstN + i;
                using var insLi = new NpgsqlCommand(@"INSERT INTO loan_installments
                    (loan_id, installment_number, due_date, principal_amount, interest_amount, total_amount, paid_amount, status, remaining_balance)
                    VALUES (@loan, @n, @due, @pi, @ii, @tt, 0, 'pending', @rem)", conn, tx);
                insLi.Parameters.AddWithValue("@loan", loanId);
                insLi.Parameters.AddWithValue("@n", n);
                insLi.Parameters.AddWithValue("@due", row.Due);
                insLi.Parameters.AddWithValue("@pi", row.Principal);
                insLi.Parameters.AddWithValue("@ii", row.Interest);
                insLi.Parameters.AddWithValue("@tt", row.Total);
                insLi.Parameters.AddWithValue("@rem", remBal);
                insLi.ExecuteNonQuery();
            }
            var lastDue = rows.Last().Due;
            using (var up = new NpgsqlCommand(@"UPDATE loans SET installment_amount=@ia, total_interest=@ti, total_amount=@ta,
                end_date=@ed, maturity_date=@md, term_months=@tm
                WHERE id=@id", conn, tx))
            {
                up.Parameters.AddWithValue("@ia", rows[0].Total);
                up.Parameters.AddWithValue("@ti", totalInterest);
                up.Parameters.AddWithValue("@ta", principal + totalInterest);
                up.Parameters.AddWithValue("@ed", lastDue);
                up.Parameters.AddWithValue("@md", lastDue);
                up.Parameters.AddWithValue("@tm", rows.Count);
                up.Parameters.AddWithValue("@id", loanId);
                up.ExecuteNonQuery();
            }
        }

        public static void FillLoanParams(NpgsqlCommand cmd, string lender, string kindVal, string? contract, string loanType,
            decimal principal, decimal rate, string freq, int count, DateTime first, int grace, decimal fees,
            int? cashBoxId, int? liabilityAcc, int? interestExpAcc, string? notes)
        {
            cmd.Parameters.AddWithValue("@l", lender);
            cmd.Parameters.AddWithValue("@k", kindVal);
            cmd.Parameters.AddWithValue("@lt", loanType);
            cmd.Parameters.AddWithValue("@p", principal);
            cmd.Parameters.AddWithValue("@r", rate);
            cmd.Parameters.AddWithValue("@f", freq);
            cmd.Parameters.AddWithValue("@cnt", count);
            cmd.Parameters.AddWithValue("@first", first);
            cmd.Parameters.AddWithValue("@g", grace);
            cmd.Parameters.AddWithValue("@fees", fees);
            cmd.Parameters.AddWithValue("@cb", cashBoxId ?? 0);
            cmd.Parameters.AddWithValue("@liab", liabilityAcc.HasValue ? (object)liabilityAcc.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@iexp", interestExpAcc.HasValue ? (object)interestExpAcc.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@c", string.IsNullOrWhiteSpace(contract) ? DBNull.Value : (object)contract.Trim());
            cmd.Parameters.AddWithValue("@n", string.IsNullOrWhiteSpace(notes) ? DBNull.Value : (object)notes.Trim());
        }
    }

    // ============================================================
    // نافذة سلفة موظف (مرتبطة بالعهدة + خزينة — حلقة مغلقة)
    // ============================================================
    public class EmployeeAdvanceWindow : Window
    {
        private readonly int _id;
        private readonly ComboBox _cmbEmp = new() { DisplayMemberPath = "Name", Margin = new Thickness(4) };
        private readonly ComboBox _cmbCustody = new() { DisplayMemberPath = "Name", Margin = new Thickness(4) };
        private readonly ComboBox _cmbCB = new() { DisplayMemberPath = "Name", Margin = new Thickness(4) };
        private readonly ComboBox _cmbDeduct = new() { DisplayMemberPath = "Name", Margin = new Thickness(4) };
        private readonly TextBox _txtAmount = new() { Margin = new Thickness(4) };
        private readonly DatePicker _dpDate = new() { Margin = new Thickness(4), SelectedDate = DateTime.Today };
        private readonly TextBox _txtNotes = new() { Margin = new Thickness(4), Height = 50, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };
        private readonly TextBlock _lblStatus = new() { Foreground = System.Windows.Media.Brushes.Red, TextWrapping = TextWrapping.Wrap };

        public EmployeeAdvanceWindow(int id)
        {
            _id = id;
            Title = id > 0 ? "تعديل سلفة موظف" : "صرف سلفة موظف";
            Width = 460; Height = 560; WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FlowDirection = FlowDirection.RightToLeft;
            _cmbDeduct.Items.Add(new ComboItem { Id = 0, Name = "استرداد نقدي من الخزينة" });
            _cmbDeduct.Items.Add(new ComboItem { Id = 1, Name = "خصم من الراتب (تسوية يدوية)" });
            _cmbDeduct.SelectedIndex = 0;
            _cmbEmp.SelectionChanged += (_, _) => LoadCustodies();

            var grid = new Grid { Margin = new Thickness(12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int i = 0; i < 9; i++) grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            Row(grid, 0, "الموظف:"); grid.Children.Add(_cmbEmp); Grid.SetRow(_cmbEmp, 0); Grid.SetColumn(_cmbEmp, 1);
            Row(grid, 1, "عهدة مرتبطة (اختياري):"); grid.Children.Add(_cmbCustody); Grid.SetRow(_cmbCustody, 1); Grid.SetColumn(_cmbCustody, 1);
            Row(grid, 2, "صرف من خزينة:"); grid.Children.Add(_cmbCB); Grid.SetRow(_cmbCB, 2); Grid.SetColumn(_cmbCB, 2);
            Row(grid, 3, "المبلغ:"); grid.Children.Add(_txtAmount); Grid.SetRow(_txtAmount, 3); Grid.SetColumn(_txtAmount, 1);
            Row(grid, 4, "تاريخ الصرف:"); grid.Children.Add(_dpDate); Grid.SetRow(_dpDate, 4); Grid.SetColumn(_dpDate, 1);
            Row(grid, 5, "طريقة الاسترداد:"); grid.Children.Add(_cmbDeduct); Grid.SetRow(_cmbDeduct, 5); Grid.SetColumn(_cmbDeduct, 1);
            Row(grid, 6, "ملاحظات:"); grid.Children.Add(_txtNotes); Grid.SetRow(_txtNotes, 6); Grid.SetColumn(_txtNotes, 1);
            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
            var save = new Button { Content = "حفظ", Padding = new Thickness(20, 6, 20, 6), Margin = new Thickness(4) };
            var cancel = new Button { Content = "إلغاء", Padding = new Thickness(20, 6, 20, 6), Margin = new Thickness(4) };
            save.Click += Save_Click; cancel.Click += (_, _) => Close();
            btns.Children.Add(save); btns.Children.Add(cancel);
            Grid.SetRow(btns, 7); grid.Children.Add(btns); Grid.SetColumn(btns, 1);
            Grid.SetRow(_lblStatus, 8); grid.Children.Add(_lblStatus); Grid.SetColumn(_lblStatus, 1);
            Content = new ScrollViewer { Content = grid };

            var ok = LoadCombos();
            if (!ok)
            {
                _cantContinue = true;
                Loaded += (_, _) => Close();
            }
            if (_id > 0 && ok) LoadData();
        }

        private bool _cantContinue;

        private static void Row(Grid g, int i, string label)
        {
            g.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold });
            Grid.SetRow(g.Children[g.Children.Count - 1], i); Grid.SetColumn(g.Children[g.Children.Count - 1], 0);
        }

        private bool LoadCombos()
        {
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                _cmbEmp.Items.Clear();
                using (var cmd = new NpgsqlCommand("SELECT id, full_name FROM employees WHERE COALESCE(status,'ACTIVE') ILIKE 'active%' ORDER BY full_name", conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) _cmbEmp.Items.Add(new ComboItem { Id = r.GetInt32(0), Name = r.GetString(1) });
                if (_cmbEmp.Items.Count == 0)
                {
                    MessageBox.Show("لا يوجد موظفين. أضف موظفًا أولًا من صفحة الموظفين.", "لا يمكن المتابعة", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
                _cmbEmp.SelectedIndex = 0;
                _cmbCB.Items.Clear();
                using (var cmd = new NpgsqlCommand("SELECT id, name FROM cash_boxes WHERE COALESCE(is_active,TRUE)=TRUE ORDER BY name", conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) _cmbCB.Items.Add(new ComboItem { Id = r.GetInt32(0), Name = r.GetString(1) });
                if (_cmbCB.Items.Count == 0)
                {
                    MessageBox.Show("لا توجد خزائن. أضف خزينة أولًا من صفحة الخزائن.", "لا يمكن المتابعة", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
                _cmbCB.SelectedIndex = 0;
                return true;
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ: " + ex.Message; return true; }
        }

        private void LoadCustodies()
        {
            try
            {
                _cmbCustody.Items.Clear();
                _cmbCustody.Items.Add(new ComboItem { Id = 0, Name = "— بدون ربط بعهدة —" });
                var emp = _cmbEmp.SelectedItem as ComboItem;
                if (emp != null)
                {
                    using var conn = new NpgsqlConnection(GetCS());
                    conn.Open();
                    using var cmd = new NpgsqlCommand(@"SELECT id, COALESCE(custody_number, 'عهدة #' || id) FROM petty_cash_custodies
                        WHERE employee_id=@e AND COALESCE(status,'open') IN ('open','partially_settled') ORDER BY issued_date DESC", conn);
                    cmd.Parameters.AddWithValue("e", emp.Id);
                    using var r = cmd.ExecuteReader();
                    while (r.Read()) _cmbCustody.Items.Add(new ComboItem { Id = r.GetInt32(0), Name = r.GetString(1) });
                }
                _cmbCustody.SelectedIndex = 0;
            }
            catch { _cmbCustody.SelectedIndex = 0; }
        }

        private void LoadData()
        {
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var cmd = new NpgsqlCommand(@"SELECT employee_id, COALESCE(principal_amount,0), loan_date, COALESCE(cash_box_id,0), COALESCE(custody_id,0), COALESCE(notes,''), COALESCE(paid_amount,0) FROM loans WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("id", _id);
                using var r = cmd.ExecuteReader();
                if (r.Read())
                {
                    if (Convert.ToDecimal(r.GetValue(6)) > 0)
                        _lblStatus.Text = "تنبيه: تم استرداد جزء من السلفة — التعديل على المبلغ غير مسموح.";
                    _txtAmount.Text = Convert.ToDecimal(r.GetValue(1)).ToString("0.##");
                    if (!r.IsDBNull(2)) _dpDate.SelectedDate = Techtoon.Core.Data.DbValue.AsDateTime(r.GetValue(2));
                    _txtNotes.Text = r.GetString(5);
                    Pick(_cmbEmp, r.IsDBNull(0) ? 0 : r.GetInt32(0));
                    Pick(_cmbCB, r.GetInt32(3));
                    LoadCustodies();
                    Pick(_cmbCustody, r.IsDBNull(4) ? 0 : r.GetInt32(4));
                }
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ: " + ex.Message; }
        }

        private static void Pick(ComboBox c, int id)
        { for (int i = 0; i < c.Items.Count; i++) if (((ComboItem)c.Items[i]).Id == id) { c.SelectedIndex = i; return; } }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_cantContinue) return;
            if (_cmbEmp.SelectedItem is not ComboItem emp) { _lblStatus.Text = "اختر الموظف."; return; }
            if (!decimal.TryParse(_txtAmount.Text, out var amount) || amount <= 0) { _lblStatus.Text = "المبلغ غير صحيح."; return; }
            if (_cmbCB.SelectedItem is not ComboItem cb) { _lblStatus.Text = "اختر الخزينة."; return; }
            var custody = _cmbCustody.SelectedItem as ComboItem;
            var deduct = _cmbDeduct.SelectedItem as ComboItem;
            var date = _dpDate.SelectedDate ?? DateTime.Today;
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var tx = conn.BeginTransaction();
                if (_id > 0)
                {
                    using (var chk = new NpgsqlCommand("SELECT COALESCE(paid_amount,0) FROM loans WHERE id=@id", conn, tx))
                    { chk.Parameters.AddWithValue("id", _id); if (Convert.ToDecimal(chk.ExecuteScalar()) > 0) { MessageBox.Show("تم استرداد جزء من السلفة — التعديل غير مسموح."); return; } }
                    using (var up = new NpgsqlCommand(@"UPDATE loans SET employee_id=@e, borrower_name=@bn, custody_id=@c, principal_amount=@a, remaining_amount=@a,
                        loan_date=@d, cash_box_id=@cb, notes=@n, repayment_method=@rm WHERE id=@id", conn, tx))
                    {
                        up.Parameters.AddWithValue("@e", emp.Id); up.Parameters.AddWithValue("@bn", emp.Name);
                        up.Parameters.AddWithValue("@c", custody == null || custody.Id == 0 ? DBNull.Value : (object)custody.Id);
                        up.Parameters.AddWithValue("@a", amount); up.Parameters.AddWithValue("@d", date);
                        up.Parameters.AddWithValue("@cb", cb.Id); up.Parameters.AddWithValue("@n", string.IsNullOrWhiteSpace(_txtNotes.Text) ? DBNull.Value : (object)_txtNotes.Text);
                        up.Parameters.AddWithValue("@rm", deduct != null && deduct.Id == 1 ? "salary_deduction" : "cash");
                        up.Parameters.AddWithValue("@id", _id); up.ExecuteNonQuery();
                    }
                    // مزامنة حركة الخزينة (الصرف) مع التعديل — حلقة مغلقة
                    using (var up2 = new NpgsqlCommand(@"UPDATE payments SET amount=@a, date=@d, cash_box_id=@cb WHERE reference_number='LOAN-ADV:' || @id", conn, tx))
                    { up2.Parameters.AddWithValue("@a", amount); up2.Parameters.AddWithValue("@d", date); up2.Parameters.AddWithValue("@cb", cb.Id); up2.Parameters.AddWithValue("@id", _id); up2.ExecuteNonQuery(); }
                    tx.Commit();
                    AuditService.Log("UPDATE", "Loan", _id, "Updated employee advance (cash movement synchronized)");
                }
                else
                {
                    int newId;
                    using (var ins = new NpgsqlCommand(@"INSERT INTO loans
                        (loan_number, borrower_name, borrower_type, principal_amount, interest_rate, loan_date,
                         installment_amount, total_interest, total_amount, paid_amount, remaining_amount,
                         cash_box_id, notes, created_by, created_at, loan_type, status, employee_id, custody_id, repayment_method)
                        VALUES ('ADV', @bn, 'employee', @a, 0, @d, 0, 0, @a, 0, @a, @cb, @n, 1, NOW(),
                         'interest_free', 'active', @e, @c, @rm)
                        RETURNING id", conn, tx))
                    {
                        ins.Parameters.AddWithValue("@bn", emp.Name);
                        ins.Parameters.AddWithValue("@a", amount); ins.Parameters.AddWithValue("@d", date);
                        ins.Parameters.AddWithValue("@cb", cb.Id);
                        ins.Parameters.AddWithValue("@n", string.IsNullOrWhiteSpace(_txtNotes.Text) ? DBNull.Value : (object)_txtNotes.Text);
                        ins.Parameters.AddWithValue("@e", emp.Id);
                        ins.Parameters.AddWithValue("@c", custody == null || custody.Id == 0 ? DBNull.Value : (object)custody.Id);
                        ins.Parameters.AddWithValue("@rm", deduct != null && deduct.Id == 1 ? "salary_deduction" : "cash");
                        newId = Convert.ToInt32(ins.ExecuteScalar());
                    }
                    using (var up = new NpgsqlCommand("UPDATE loans SET loan_number=@ln WHERE id=@id", conn, tx))
                    { up.Parameters.AddWithValue("@ln", "ADV-" + newId.ToString("0000")); up.Parameters.AddWithValue("@id", newId); up.ExecuteNonQuery(); }
                    // الحلقة: الصرف الفعلي من الخزينة (تاريخ + مبلغ + خزينة + عملة)
                    using (var pay = new NpgsqlCommand(@"INSERT INTO payments (date, amount, payment_type, payment_method, reference_number, notes, cash_box_id, currency_id)
                        VALUES (@d, @a, 'payment', 'cash', 'LOAN-ADV:' || @id, @note, @cb, @cur)", conn, tx))
                    {
                        pay.Parameters.AddWithValue("@d", date); pay.Parameters.AddWithValue("@a", amount);
                        pay.Parameters.AddWithValue("@id", newId);
                        pay.Parameters.AddWithValue("@note", "صرف سلفة الموظف: " + emp.Name);
                        pay.Parameters.AddWithValue("@cb", cb.Id);
                        pay.Parameters.AddWithValue("@cur", DbDefaults.GetDefaultCurrencyId(conn));
                        pay.ExecuteNonQuery();
                    }
                    tx.Commit();
                    AuditService.Log("CREATE", "Loan", newId, "Employee advance issued - cash movement posted (closed loop)");
                }
                DialogResult = true; Close();
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ الحفظ: " + ex.Message; }
        }

        public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();
    }

    // ============================================================
    // نافذة قرض منشأة (أنواع متعددة + توليد جدول أقساط + حركة خزينة)
    // ============================================================
    public class EnterpriseLoanWindow : Window
    {
        private readonly int _id;
        private readonly TextBox _txtLender = new() { Margin = new Thickness(4) };
        private readonly ComboBox _cmbKind = new() { DisplayMemberPath = "Name", Margin = new Thickness(4) };
        private readonly ComboBox _cmbType = new() { DisplayMemberPath = "Name", Margin = new Thickness(4) };
        private readonly ComboBox _cmbFreq = new() { DisplayMemberPath = "Name", Margin = new Thickness(4) };
        private readonly ComboBox _cmbCB = new() { DisplayMemberPath = "Name", Margin = new Thickness(4) };
        private readonly ComboBox _cmbLiab = new() { DisplayMemberPath = "Name", Margin = new Thickness(4) };
        private readonly ComboBox _cmbIntExp = new() { DisplayMemberPath = "Name", Margin = new Thickness(4) };
        private readonly TextBox _txtPrincipal = new() { Margin = new Thickness(4) };
        private readonly TextBox _txtRate = new() { Margin = new Thickness(4), Text = "0" };
        private readonly TextBox _txtCount = new() { Margin = new Thickness(4), Text = "12" };
        private readonly TextBox _txtGrace = new() { Margin = new Thickness(4), Text = "0" };
        private readonly TextBox _txtFees = new() { Margin = new Thickness(4), Text = "0" };
        private readonly TextBox _txtContract = new() { Margin = new Thickness(4) };
        private readonly DatePicker _dpFirst = new() { Margin = new Thickness(4), SelectedDate = DateTime.Today.AddMonths(1) };
        private readonly TextBox _txtNotes = new() { Margin = new Thickness(4), Height = 45, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };
        private readonly TextBlock _lblStatus = new() { Foreground = System.Windows.Media.Brushes.Red, TextWrapping = TextWrapping.Wrap };

        public int CreatedLoanId { get; private set; }

        public EnterpriseLoanWindow(int id)
        {
            _id = id;
            Title = id > 0 ? "تعديل قرض المنشأة" : "إضافة قرض منشأة";
            Width = 520; Height = 740; WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FlowDirection = FlowDirection.RightToLeft;

            _cmbKind.Items.Add(new ComboItem { Id = 0, Name = "بنك" });
            _cmbKind.Items.Add(new ComboItem { Id = 1, Name = "مؤسسة تمويل / شركة" });
            _cmbKind.Items.Add(new ComboItem { Id = 2, Name = "طرف آخر" });
            _cmbKind.SelectedIndex = 0;
            _cmbFreq.Items.Add(new ComboItem { Id = 0, Name = "شهري" });
            _cmbFreq.Items.Add(new ComboItem { Id = 1, Name = "ربع سنوي" });
            _cmbFreq.Items.Add(new ComboItem { Id = 2, Name = "سنوي" });
            _cmbFreq.SelectedIndex = 0;
            _cmbType.Items.Add(new ComboItem { Id = 0, Name = "سداد بأقساط - فائدة ثابتة" });
            _cmbType.Items.Add(new ComboItem { Id = 1, Name = "سداد بأقساط - فائدة متغيرة" });
            _cmbType.Items.Add(new ComboItem { Id = 2, Name = "جارٍ مدين - تسديد فائدة فقط" });
            _cmbType.Items.Add(new ComboItem { Id = 3, Name = "بدون فائدة" });
            _cmbType.SelectedIndex = 0;

            var grid = new Grid { Margin = new Thickness(12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(170) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int i = 0; i < 14; i++) grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            R(grid, 0, "اسم المُقرض/البنك:"); grid.Children.Add(_txtLender); Grid.SetRow(_txtLender, 0); Grid.SetColumn(_txtLender, 1);
            R(grid, 1, "نوع المُقرض:"); grid.Children.Add(_cmbKind); Grid.SetRow(_cmbKind, 1); Grid.SetColumn(_cmbKind, 1);
            R(grid, 2, "نوع القرض:"); grid.Children.Add(_cmbType); Grid.SetRow(_cmbType, 2); Grid.SetColumn(_cmbType, 1);
            R(grid, 3, "رأس المال:"); grid.Children.Add(_txtPrincipal); Grid.SetRow(_txtPrincipal, 3); Grid.SetColumn(_txtPrincipal, 1);
            R(grid, 4, "الفائدة السنوية %:"); grid.Children.Add(_txtRate); Grid.SetRow(_txtRate, 4); Grid.SetColumn(_txtRate, 1);
            R(grid, 5, "تكرار القسط:"); grid.Children.Add(_cmbFreq); Grid.SetRow(_cmbFreq, 5); Grid.SetColumn(_cmbFreq, 1);
            R(grid, 6, "عدد الأقساط:"); grid.Children.Add(_txtCount); Grid.SetRow(_txtCount, 6); Grid.SetColumn(_txtCount, 1);
            R(grid, 7, "أول قسط تاريخ:"); grid.Children.Add(_dpFirst); Grid.SetRow(_dpFirst, 7); Grid.SetColumn(_dpFirst, 1);
            R(grid, 8, "فترة سماح (شهور):"); grid.Children.Add(_txtGrace); Grid.SetRow(_txtGrace, 8); Grid.SetColumn(_txtGrace, 1);
            R(grid, 9, "رسوم إدارية:"); grid.Children.Add(_txtFees); Grid.SetRow(_txtFees, 9); Grid.SetColumn(_txtFees, 1);
            R(grid, 10, "رقم العقد:"); grid.Children.Add(_txtContract); Grid.SetRow(_txtContract, 10); Grid.SetColumn(_txtContract, 1);
            R(grid, 11, "خزينة الاستلام:"); grid.Children.Add(_cmbCB); Grid.SetRow(_cmbCB, 11); Grid.SetColumn(_cmbCB, 1);
            var accPanel = new StackPanel { Orientation = Orientation.Horizontal };
            accPanel.Children.Add(_cmbLiab); accPanel.Children.Add(_cmbIntExp);
            R(grid, 12, "حساب الالتزام / مصروف الفائدة:"); grid.Children.Add(accPanel); Grid.SetRow(accPanel, 12); Grid.SetColumn(accPanel, 1);
            R(grid, 13, "ملاحظات:"); grid.Children.Add(_txtNotes); Grid.SetRow(_txtNotes, 13); Grid.SetColumn(_txtNotes, 1);

            var outer = new Grid { Margin = new Thickness(6) };
            outer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            outer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            outer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            outer.Children.Add(new ScrollViewer { Content = grid });
            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8, 0, 0) };
            var save = new Button { Content = "حفظ وإنشاء الجدول", Padding = new Thickness(20, 6, 20, 6), Margin = new Thickness(4) };
            var cancel = new Button { Content = "إلغاء", Padding = new Thickness(20, 6, 20, 6), Margin = new Thickness(4) };
            save.Click += Save_Click; cancel.Click += (_, _) => Close();
            btns.Children.Add(save); btns.Children.Add(cancel);
            Grid.SetRow(btns, 1); outer.Children.Add(btns);
            Grid.SetRow(_lblStatus, 2); outer.Children.Add(_lblStatus);
            Content = outer;

            LoadAccounts();
            var ok = LoadCashBoxes();
            if (!ok)
            {
                _cantContinue = true;
                Loaded += (_, _) => Close();
            }
            if (_id > 0 && ok) LoadData();
        }

        private bool _cantContinue;

        private static void R(Grid g, int i, string label)
        {
            g.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold });
            Grid.SetRow(g.Children[g.Children.Count - 1], i); Grid.SetColumn(g.Children[g.Children.Count - 1], 0);
        }

        private void LoadAccounts()
        {
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                _cmbLiab.Items.Clear();
                _cmbIntExp.Items.Clear();
                _cmbLiab.Items.Add(new ComboItem { Id = 0, Name = "— تلقائي —" });
                _cmbIntExp.Items.Add(new ComboItem { Id = 0, Name = "— تلقائي —" });
                using (var cmd = new NpgsqlCommand("SELECT id, name FROM accounts WHERE UPPER(COALESCE(type,''))='LIABILITY' ORDER BY name", conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) _cmbLiab.Items.Add(new ComboItem { Id = r.GetInt32(0), Name = r.GetString(1) });
                using (var cmd = new NpgsqlCommand("SELECT id, name FROM accounts WHERE UPPER(COALESCE(type,''))='EXPENSE' ORDER BY name", conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) _cmbIntExp.Items.Add(new ComboItem { Id = r.GetInt32(0), Name = r.GetString(1) });
                // P4: ربط تلقائي بأول حساب التزام/مصروف (حلقة محاسبية مغلقة) — "تلقائي" يبقى متاحًا
                if (_cmbLiab.Items.Count > 1) _cmbLiab.SelectedIndex = 1;
                if (_cmbIntExp.Items.Count > 1) _cmbIntExp.SelectedIndex = 1;
            }
            catch { }
        }

        private bool LoadCashBoxes()
        {
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                _cmbCB.Items.Clear();
                using var cmd = new NpgsqlCommand("SELECT id, name FROM cash_boxes WHERE COALESCE(is_active,TRUE)=TRUE ORDER BY name", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read()) _cmbCB.Items.Add(new ComboItem { Id = r.GetInt32(0), Name = r.GetString(1) });
                if (_cmbCB.Items.Count == 0)
                {
                    MessageBox.Show("لا توجد خزائن. أضف خزينة أولًا.", "لا يمكن المتابعة", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
                _cmbCB.SelectedIndex = 0;
                return true;
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ: " + ex.Message; return true; }
        }

        private void LoadData()
        {
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var cmd = new NpgsqlCommand(@"SELECT borrower_name, contract_number, loan_type, principal_amount, interest_rate,
                    payment_frequency, num_installments, first_payment_date, COALESCE(grace_period,0), COALESCE(admin_fees,0),
                    COALESCE(cash_box_id,0), COALESCE(liability_account_id,0), COALESCE(interest_expense_account_id,0), COALESCE(notes,''),
                    COALESCE(borrower_type,'bank') FROM loans WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("id", _id);
                using var r = cmd.ExecuteReader();
                if (r.Read())
                {
                    _txtLender.Text = r.GetString(0);
                    _txtContract.Text = r.GetString(1);
                    _cmbType.SelectedIndex = r.GetString(2) switch { "installment_variable" => 1, "running_interest" => 2, "interest_free" => 3, _ => 0 };
                    _txtPrincipal.Text = Convert.ToDecimal(r.GetValue(3)).ToString("0.##");
                    _txtRate.Text = Convert.ToDecimal(r.GetValue(4)).ToString("0.##");
                    _cmbFreq.SelectedIndex = (r.IsDBNull(5) ? "" : r.GetString(5)) switch { "quarterly" => 1, "yearly" => 2, _ => 0 };
                    _txtCount.Text = (r.IsDBNull(6) ? 12 : r.GetInt32(6)).ToString();
                    _dpFirst.SelectedDate = r.IsDBNull(7) ? DateTime.Today.AddMonths(1) : Techtoon.Core.Data.DbValue.AsDateTime(r.GetValue(7));
                    _txtGrace.Text = r.GetInt32(8).ToString();
                    _txtFees.Text = Convert.ToDecimal(r.GetValue(9)).ToString("0.##");
                    Pick(_cmbCB, r.GetInt32(10));
                    Pick(_cmbLiab, r.GetInt32(11));
                    Pick(_cmbIntExp, r.GetInt32(12));
                    _txtNotes.Text = r.GetString(13);
                    _cmbKind.SelectedIndex = r.GetString(14) switch { "institution" => 1, "other" => 2, _ => 0 };
                }
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ: " + ex.Message; }
        }

        private static void Pick(ComboBox c, int id)
        { for (int i = 0; i < c.Items.Count; i++) if (((ComboItem)c.Items[i]).Id == id) { c.SelectedIndex = i; return; } }

        private static string FreqValue(int idx) => idx switch { 1 => "quarterly", 2 => "yearly", _ => "monthly" };

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_cantContinue) return;
            var kind = _cmbKind.SelectedItem as ComboItem;
            var kindVal = kind?.Id switch { 1 => "institution", 2 => "other", _ => "bank" };
            var loanType = (_cmbType.SelectedItem as ComboItem)?.Id switch { 1 => "installment_variable", 2 => "running_interest", 3 => "interest_free", _ => "installment_fixed" };
            var freq = FreqValue(_cmbFreq.SelectedIndex);
            var lender = _txtLender.Text.Trim();
            if (string.IsNullOrWhiteSpace(lender)) { _lblStatus.Text = "اسم المُقرض مطلوب."; return; }
            if (!decimal.TryParse(_txtPrincipal.Text, out var principal) || principal <= 0) { _lblStatus.Text = "رأس المال غير صحيح."; return; }
            if (!decimal.TryParse(_txtRate.Text, out var rate) || rate < 0) { _lblStatus.Text = "الفائدة غير صحيحة."; return; }
            if (!int.TryParse(_txtCount.Text, out var count) || count <= 0) { _lblStatus.Text = "عدد الأقساط غير صحيح."; return; }
            if (!int.TryParse(_txtGrace.Text, out var grace) || grace < 0) { _lblStatus.Text = "فترة السماح غير صحيحة."; return; }
            decimal.TryParse(_txtFees.Text, out var fees);
            if (_cmbType.SelectedIndex == 2 && rate <= 0) { _lblStatus.Text = "القرض الجاري مدين يتطلب فائدة أكبر من صفر."; return; }
            if (loanType == "installment_fixed" && rate <= 0) { _lblStatus.Text = "أدخل فائدة أو اختر: بدون فائدة."; return; }
            var first = _dpFirst.SelectedDate ?? DateTime.Today.AddMonths(1);
            var cbId = (_cmbCB.SelectedItem as ComboItem)?.Id ?? 0;
            var liabId = (_cmbLiab.SelectedItem as ComboItem)?.Id ?? 0;
            var iexpId = (_cmbIntExp.SelectedItem as ComboItem)?.Id ?? 0;
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var tx = conn.BeginTransaction();
                int loanId = _id;
                if (_id > 0)
                {
                    bool hasPayments;
                    using (var chk = new NpgsqlCommand("SELECT COUNT(*) FROM loan_payments WHERE loan_id=@id", conn, tx))
                    { chk.Parameters.AddWithValue("@id", _id); hasPayments = Convert.ToInt64(chk.ExecuteScalar()) > 0; }
                    using (var up = new NpgsqlCommand(@"UPDATE loans SET borrower_name=@l, borrower_type=@k, contract_number=@c, loan_type=@lt,
                        principal_amount=@p, remaining_amount=COALESCE(remaining_amount,0) + (@p - COALESCE(principal_amount,0)),
                        interest_rate=@r, payment_frequency=@f, num_installments=@cnt, first_payment_date=@first,
                        grace_period=@g, admin_fees=@fees, cash_box_id=@cb,
                        liability_account_id=@liab, interest_expense_account_id=@iexp, notes=@n
                        WHERE id=@id2", conn, tx))
                    {
                        LoanDbHelpers.FillLoanParams(up, lender, kindVal, _txtContract.Text, loanType, principal, rate, freq, count, first, grace, fees, cbId, liabId > 0 ? liabId : (int?)null, iexpId > 0 ? iexpId : (int?)null, _txtNotes.Text);
                        up.Parameters.AddWithValue("@id2", _id);
                        up.ExecuteNonQuery();
                    }
                    if (!hasPayments)
                        LoanDbHelpers.RebuildSchedule(conn, tx, loanId, loanType, principal, rate, count, freq, first, grace);
                }
                else
                {
                    using (var ins = new NpgsqlCommand(@"INSERT INTO loans
                        (loan_number, borrower_name, borrower_type, principal_amount, interest_rate, loan_date, maturity_date,
                         term_months, installment_amount, total_interest, total_amount, paid_amount, remaining_amount,
                         cash_box_id, notes, created_by, created_at, loan_type, status, contract_number,
                         payment_frequency, num_installments, first_payment_date, end_date, grace_period, admin_fees,
                         liability_account_id, interest_expense_account_id)
                        VALUES ('LN', @l, @k, @p, @r, NOW(), NOW(), @cnt, 0, 0, @p, 0, @p, @cb, @n, 1, NOW(),
                         @lt, 'active', @c, @f, @cnt, @first, NOW(), @g, @fees, @liab, @iexp)
                        RETURNING id", conn, tx))
                    {
                        LoanDbHelpers.FillLoanParams(ins, lender, kindVal, _txtContract.Text, loanType, principal, rate, freq, count, first, grace, fees, cbId, liabId > 0 ? liabId : (int?)null, iexpId > 0 ? iexpId : (int?)null, _txtNotes.Text);
                        loanId = Convert.ToInt32(ins.ExecuteScalar());
                    }
                    // حركة استلام القرض (دخل الخزينة = الحلقة 1) — الصافي بعد الرسوم الإدارية
                    using (var pay = new NpgsqlCommand(@"INSERT INTO payments (date, amount, payment_type, payment_method, reference_number, notes, cash_box_id, currency_id, account_id)
                        VALUES (@d, @amt, 'receipt', 'cash', 'LOAN:' || @id2, @note, @cb, @cur, @acc)", conn, tx))
                    {
                        pay.Parameters.AddWithValue("@d", DateTime.Today);
                        pay.Parameters.AddWithValue("@amt", principal - fees);
                        pay.Parameters.AddWithValue("@id2", loanId);
                        pay.Parameters.AddWithValue("@note", "استلام قرض من: " + lender + (fees > 0 ? " (صافي بعد رسوم " + fees.ToString("N2") + ")" : ""));
                        pay.Parameters.AddWithValue("@cb", cbId);
                        pay.Parameters.AddWithValue("@cur", DbDefaults.GetDefaultCurrencyId(conn));
                        pay.Parameters.AddWithValue("@acc", liabId > 0 ? (object)liabId : DBNull.Value);
                        pay.ExecuteNonQuery();
                    }
                    CreatedLoanId = loanId;
                }
                if (_id == 0) LoanDbHelpers.RebuildSchedule(conn, tx, loanId, loanType, principal, rate, count, freq, first, grace);
                using (var upn = new NpgsqlCommand("UPDATE loans SET loan_number=@ln WHERE id=@id", conn, tx))
                { upn.Parameters.AddWithValue("@ln", "LN-" + loanId.ToString("0000")); upn.Parameters.AddWithValue("@id", loanId); upn.ExecuteNonQuery(); }
                tx.Commit();
                AuditService.Log(_id > 0 ? "UPDATE" : "CREATE", "Loan", loanId, "Enterprise loan saved (closed loop)");
                DialogResult = true; Close();
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ الحفظ: " + ex.Message; }
        }

        public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();
    }
}
