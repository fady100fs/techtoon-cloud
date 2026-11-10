using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Npgsql;
using Techtoon.UI.Services;

namespace Techtoon.UI.Views
{
    // ============================================================
    // نافذة جدول الأقساط — عرض + تسديد كل قسط + إعادة حساب الفائدة المتغيرة
    // ============================================================
    public class LoanScheduleWindow : Window
    {
        private class InstRow
        {
            public int Id;
            public int Number;
            public DateTime Due;
            public decimal Principal, Interest, Total, Paid;
            public decimal Remaining => Total - Paid;
            public string Status = "pending";
        }

        private class WrapRow
        {
            public InstRow R = new();
            public int Number => R.Number;
            public string DueDisplay => R.Due.ToString("yyyy-MM-dd");
            public string PrincipalDisplay => R.Principal.ToString("N2");
            public string InterestDisplay => R.Interest.ToString("N2");
            public string TotalDisplay => R.Total.ToString("N2");
            public string PaidDisplay => R.Paid.ToString("N2");
            public string RemainingDisplay => R.Remaining.ToString("N2");
            public string StatusDisplay => R.Status switch { "paid" => "مسدد", "partial" => "جزئي", _ => "مستحق" };
        }

        private readonly int _loanId;
        private readonly ObservableCollection<WrapRow> _view = new();
        private readonly DataGrid _grid = new()
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            CanUserAddRows = false,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
            FlowDirection = FlowDirection.RightToLeft
        };
        private readonly TextBlock _lblInfo = new() { FontSize = 14, FontWeight = FontWeights.SemiBold, Margin = new Thickness(4), TextWrapping = TextWrapping.Wrap };
        private readonly TextBlock _lblTotals = new() { FontSize = 13, FontWeight = FontWeights.Bold, Margin = new Thickness(4) };
        private readonly TextBlock _lblStatus = new() { Foreground = System.Windows.Media.Brushes.Red, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4, 35, 0, 0) };
        private Button _recalcBtn;
        private string _loanNumber = "", _lender = "", _loanType = "", _freq = "";
        private decimal _rate, _principal;

        public LoanScheduleWindow(int loanId)
        {
            _loanId = loanId;
            Title = "جدول الأقساط";
            Width = 920; Height = 640; WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FlowDirection = FlowDirection.RightToLeft;

            var grid = new Grid { Margin = new Thickness(10) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            grid.Children.Add(_lblInfo); Grid.SetRow(_lblInfo, 0);

            _grid.Columns.Add(new DataGridTextColumn { Header = "#", Binding = new System.Windows.Data.Binding("Number"), Width = 45 });
            _grid.Columns.Add(new DataGridTextColumn { Header = "الاستحقاق", Binding = new System.Windows.Data.Binding("DueDisplay"), Width = 100 });
            _grid.Columns.Add(new DataGridTextColumn { Header = "قسط أصل", Binding = new System.Windows.Data.Binding("PrincipalDisplay"), Width = 110 });
            _grid.Columns.Add(new DataGridTextColumn { Header = "فائدة", Binding = new System.Windows.Data.Binding("InterestDisplay"), Width = 100 });
            _grid.Columns.Add(new DataGridTextColumn { Header = "الإجمالي", Binding = new System.Windows.Data.Binding("TotalDisplay"), Width = 110 });
            _grid.Columns.Add(new DataGridTextColumn { Header = "مسدد", Binding = new System.Windows.Data.Binding("PaidDisplay"), Width = 110 });
            _grid.Columns.Add(new DataGridTextColumn { Header = "المتبقي", Binding = new System.Windows.Data.Binding("RemainingDisplay"), Width = 110 });
            _grid.Columns.Add(new DataGridTextColumn { Header = "الحالة", Binding = new System.Windows.Data.Binding("StatusDisplay"), Width = 90 });
            _grid.ItemsSource = _view;

            var wrap = new Border { Child = _grid, BorderBrush = System.Windows.Media.Brushes.LightGray, BorderThickness = new Thickness(1) };
            grid.Children.Add(wrap); Grid.SetRow(wrap, 1);
            grid.Children.Add(_lblTotals); Grid.SetRow(_lblTotals, 2);

            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8, 0, 0) };
            var payBtn = new Button { Content = "تسديد القسط المحدد", Padding = new Thickness(16, 6, 16, 6), Margin = new Thickness(4), Background = System.Windows.Media.Brushes.SeaGreen, Foreground = System.Windows.Media.Brushes.White };
            payBtn.Click += PaySelected_Click;
            btns.Children.Add(payBtn);
            _recalcBtn = new Button { Content = "إعادة حساب الفائدة (أسعار متغيرة)", Padding = new Thickness(16, 6, 16, 6), Margin = new Thickness(4), Background = System.Windows.Media.Brushes.DarkOrange, Foreground = System.Windows.Media.Brushes.White };
            _recalcBtn.Click += RecalcInterest_Click;
            btns.Children.Add(_recalcBtn);
            var closeBtn = new Button { Content = "إغلاق", Padding = new Thickness(16, 6, 16, 6), Margin = new Thickness(4) };
            closeBtn.Click += (_, _) => Close();
            btns.Children.Add(closeBtn);
            grid.Children.Add(btns); Grid.SetRow(btns, 3);
            grid.Children.Add(_lblStatus); Grid.SetRow(_lblStatus, 3);
            Content = grid;

            Load();
        }

        private void Load()
        {
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using (var cmd = new NpgsqlCommand(@"SELECT COALESCE(loan_number,''), COALESCE(borrower_name,''), COALESCE(loan_type,''),
                    COALESCE(principal_amount,0), COALESCE(interest_rate,0), COALESCE(installment_amount,0), COALESCE(total_interest,0),
                    COALESCE(total_amount,0), COALESCE(paid_amount,0), COALESCE(remaining_amount,0), COALESCE(status,'active'),
                    COALESCE(payment_frequency,'monthly') FROM loans WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", _loanId);
                    using var r = cmd.ExecuteReader();
                    if (!r.Read()) { _lblStatus.Text = "القرض غير موجود."; return; }
                    _loanNumber = r.GetString(0); _lender = r.GetString(1); _loanType = r.GetString(2);
                    _principal = Convert.ToDecimal(r.GetValue(3)); _rate = Convert.ToDecimal(r.GetValue(4));
                    var inst = Convert.ToDecimal(r.GetValue(5)); var ti = Convert.ToDecimal(r.GetValue(6));
                    var ta = Convert.ToDecimal(r.GetValue(7)); var paid = Convert.ToDecimal(r.GetValue(8));
                    var rem = Convert.ToDecimal(r.GetValue(9)); var status = r.GetString(10); _freq = r.GetString(11);
                    _lblInfo.Text = _loanNumber + " — " + _lender + "   |   " + LoansViewModel_TypeName(_loanType) +
                        "   |   رأس المال " + _principal.ToString("N2") + "   |   فائدة " + _rate.ToString("0.##") + "%" +
                        "   |   القسط " + inst.ToString("N2");
                    _lblTotals.Text = "إجمالي الالتزام " + ta.ToString("N2") + "   |   فائدة " + ti.ToString("N2") +
                        "   |   مسدد " + paid.ToString("N2") + "   |   متبقٍ " + rem.ToString("N2") + "   |   الحالة " + (status == "closed" ? "مغلق" : "نشط");
                }
                _recalcBtn.Visibility = _loanType == "installment_variable" ? Visibility.Visible : Visibility.Collapsed;

                _view.Clear();
                using (var cmd = new NpgsqlCommand(@"SELECT id, installment_number, due_date, COALESCE(principal_amount,0),
                    COALESCE(interest_amount,0), COALESCE(total_amount,0), COALESCE(paid_amount,0), COALESCE(status,'pending')
                    FROM loan_installments WHERE loan_id=@id ORDER BY installment_number", conn))
                {
                    cmd.Parameters.AddWithValue("@id", _loanId);
                    using var r = cmd.ExecuteReader();
                    while (r.Read())
                        _view.Add(new WrapRow
                        {
                            R = new InstRow
                            {
                                Id = r.GetInt32(0),
                                Number = Convert.ToInt32(r.GetValue(1)),
                                Due = (DateTime)r.GetValue(2),
                                Principal = Convert.ToDecimal(r.GetValue(3)),
                                Interest = Convert.ToDecimal(r.GetValue(4)),
                                Total = Convert.ToDecimal(r.GetValue(5)),
                                Paid = Convert.ToDecimal(r.GetValue(6)),
                                Status = r.GetString(7)
                            }
                        });
                }
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ: " + ex.Message; }
        }

        private static string LoansViewModel_TypeName(string t) => t switch
        {
            "installment_fixed" => "أقساط - فائدة ثابتة",
            "installment_variable" => "أقساط - فائدة متغيرة",
            "running_interest" => "جارٍ مدين - فائدة فقط",
            "interest_free" => "بدون فائدة",
            _ => t ?? ""
        };

        private void PaySelected_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sel = _grid.SelectedItem as WrapRow;
                if (sel == null) { MessageBox.Show("اختر قسطًا من الجدول."); return; }
                if (sel.R.Remaining <= 0) { MessageBox.Show("القسط مسدد بالكامل."); return; }
                var win = new LoanPayWindow(
                    "", "تسديد القسط رقم " + sel.Number + " — " + _loanNumber,
                    sel.R.Remaining, "payment", "LOAN-INST:" + _loanId + ":" + sel.R.Id, _loanId, sel.R.Id,
                    "تسديد قسط " + sel.Number + " للقرض " + _loanNumber + " (" + _lender + ")");
                win.ShowDialog();
                Load();
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ: " + ex.Message; }
        }

        /// <summary>قروض الفائدة المتغيرة: إعادة حساب الفائدة على الأقساط غير المسددة بالسعر الحالي.</summary>
        private void RecalcInterest_Click(object sender, RoutedEventArgs e)
        {
            if (_loanType != "installment_variable") return;
            if (MessageBox.Show("إعادة حساب الفائدة للأقساط غير المسددة بالسعر الحالي (" + _rate.ToString("0.##") + "%)؟", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var tx = conn.BeginTransaction();
                using var cmd = new NpgsqlCommand(@"SELECT id, COALESCE(principal_amount,0)
                    FROM loan_installments WHERE loan_id=@id AND COALESCE(paid_amount,0) < COALESCE(total_amount,0)
                    ORDER BY installment_number", conn, tx);
                cmd.Parameters.AddWithValue("@id", _loanId);
                var items = new List<(int id, decimal principal)>();
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) items.Add((r.GetInt32(0), Convert.ToDecimal(r.GetValue(1))));

                decimal outstanding = _principal;
                using (var c = new NpgsqlCommand("SELECT COALESCE(SUM(principal_part),0) FROM loan_payments WHERE loan_id=@id", conn, tx))
                { c.Parameters.AddWithValue("@id", _loanId); outstanding -= Convert.ToDecimal(c.ExecuteScalar()); }

                decimal rperiod = _rate / 100m / LoanScheduleCalc.PeriodsPerYear(_freq);
                foreach (var it in items)
                {
                    var newInterest = Math.Round(Math.Max(0, outstanding) * rperiod, 2);
                    using var up = new NpgsqlCommand(@"UPDATE loan_installments SET interest_amount=@ii, total_amount=COALESCE(principal_amount,0)+@ii WHERE id=@id", conn, tx);
                    up.Parameters.AddWithValue("@ii", newInterest);
                    up.Parameters.AddWithValue("@id", it.id);
                    up.ExecuteNonQuery();
                    outstanding -= it.principal;
                }
                using (var up3 = new NpgsqlCommand(@"UPDATE loans SET total_interest =
                    COALESCE((SELECT SUM(interest_amount) FROM loan_installments WHERE loan_id=@id),0)
                    WHERE id=@id", conn, tx))
                { up3.Parameters.AddWithValue("@id", _loanId); up3.ExecuteNonQuery(); }
                using (var up4 = new NpgsqlCommand("UPDATE loans SET total_amount = COALESCE(principal_amount,0) + COALESCE(total_interest,0) WHERE id=@id", conn, tx))
                { up4.Parameters.AddWithValue("@id", _loanId); up4.ExecuteNonQuery(); }
                tx.Commit();
                AuditService.Log("UPDATE", "Loan", _loanId, "Variable-rate interest recalculated for unpaid installments");
                Load();
                MessageBox.Show("تم إعادة حساب الفائدة على " + items.Count + " قسط غير مسدد.", "تم", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show("خطأ: " + ex.Message); }
        }

        public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();
    }

    // ============================================================
    // نافذة التسديد المشتركة (سلفة / قسط / تسوية قرض جارٍ مدين)
    // ============================================================
    public class LoanPayWindow : Window
    {
        private readonly string _payType, _refPrefix, _notes;
        private readonly decimal _maxAmount;
        private readonly int _loanId, _installmentId;
        private readonly bool _principalOnly;
        private readonly ComboBox _cmbCB = new() { DisplayMemberPath = "Name", Margin = new Thickness(4) };
        private readonly ComboBox _cmbMethod = new() { DisplayMemberPath = "Name", Margin = new Thickness(4) };
        private readonly TextBox _txtAmount = new() { Margin = new Thickness(4) };
        private readonly DatePicker _dpDate = new() { Margin = new Thickness(4), SelectedDate = DateTime.Today };
        private readonly TextBox _txtNotes = new() { Margin = new Thickness(4), Height = 40, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };
        private readonly TextBlock _lblStatus = new() { Foreground = System.Windows.Media.Brushes.Red, TextWrapping = TextWrapping.Wrap };

        public LoanPayWindow(string referenceNumber, string title, decimal maxAmount, string payType,
            string refPrefix, int loanId, int? installmentId, string notes, bool principalOnly = false, bool closeOnFull = false)
        {
            _maxAmount = maxAmount; _payType = payType; _refPrefix = refPrefix;
            _loanId = loanId; _installmentId = installmentId ?? 0;
            _notes = notes; _principalOnly = principalOnly;
            Title = title;
            Width = 440; Height = 420; WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FlowDirection = FlowDirection.RightToLeft;
            _txtAmount.Text = maxAmount.ToString("0.##");
            _cmbMethod.Items.Add(new ComboItem { Id = 0, Name = "نقدي" });
            _cmbMethod.Items.Add(new ComboItem { Id = 1, Name = "تحويل بنكي" });
            _cmbMethod.SelectedIndex = 0;

            var grid = new Grid { Margin = new Thickness(12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int i = 0; i < 7; i++) grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            Row(grid, 0, "المبلغ:"); grid.Children.Add(_txtAmount); Grid.SetRow(_txtAmount, 0); Grid.SetColumn(_txtAmount, 1);
            Row(grid, 1, "التاريخ:"); grid.Children.Add(_dpDate); Grid.SetRow(_dpDate, 1); Grid.SetColumn(_dpDate, 1);
            Row(grid, 2, "الخزينة:"); grid.Children.Add(_cmbCB); Grid.SetRow(_cmbCB, 2); Grid.SetColumn(_cmbCB, 1);
            Row(grid, 3, "الطريقة:"); grid.Children.Add(_cmbMethod); Grid.SetRow(_cmbMethod, 3); Grid.SetColumn(_cmbMethod, 1);
            Row(grid, 4, "ملاحظات:"); grid.Children.Add(_txtNotes); Grid.SetRow(_txtNotes, 4); Grid.SetColumn(_txtNotes, 1);
            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
            var save = new Button { Content = "تسديد", Padding = new Thickness(20, 6, 20, 6), Margin = new Thickness(4) };
            var cancel = new Button { Content = "إلغاء", Padding = new Thickness(20, 6, 20, 6), Margin = new Thickness(4) };
            save.Click += Save_Click; cancel.Click += (_, _) => Close();
            btns.Children.Add(save); btns.Children.Add(cancel);
            Grid.SetRow(btns, 5); grid.Children.Add(btns); Grid.SetColumn(btns, 1);
            Grid.SetRow(_lblStatus, 6); grid.Children.Add(_lblStatus); Grid.SetColumn(_lblStatus, 1);
            Content = grid;
            LoadCashBoxes();
        }

        private static void Row(Grid g, int i, string label)
        {
            g.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold });
            Grid.SetRow(g.Children[g.Children.Count - 1], i); Grid.SetColumn(g.Children[g.Children.Count - 1], 0);
        }

        private void LoadCashBoxes()
        {
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                _cmbCB.Items.Clear();
                using var cmd = new NpgsqlCommand("SELECT id, name FROM cash_boxes WHERE COALESCE(is_active,TRUE)=TRUE ORDER BY name", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read()) _cmbCB.Items.Add(new ComboItem { Id = r.GetInt32(0), Name = r.GetString(1) });
                _cmbCB.SelectedIndex = _cmbCB.Items.Count > 0 ? 0 : -1;
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ: " + ex.Message; }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(_txtAmount.Text, out var amount) || amount <= 0) { _lblStatus.Text = "المبلغ غير صحيح."; return; }
            if (_installmentId > 0 && amount > _maxAmount + 0.005m) { _lblStatus.Text = "المبلغ أكبر من متبقي القسط (" + _maxAmount.ToString("N2") + ")."; return; }
            if (_cmbCB.SelectedItem is not ComboItem cb) { _lblStatus.Text = "اختر الخزينة."; return; }
            var method = ((ComboItem)_cmbMethod.SelectedItem).Id == 1 ? "bank_transfer" : "cash";
            var date = _dpDate.SelectedDate ?? DateTime.Today;
            var note = string.IsNullOrWhiteSpace(_txtNotes.Text) ? _notes : _txtNotes.Text;
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var tx = conn.BeginTransaction();

                // توزيع المبلغ: الفائدة أولًا ثم الأصل
                decimal interestPart = 0;
                if (_installmentId > 0)
                {
                    using (var c = new NpgsqlCommand(@"SELECT COALESCE(interest_amount,0),
                        COALESCE((SELECT SUM(interest_part) FROM loan_payments WHERE installment_id=@i),0) FROM loan_installments WHERE id=@i", conn, tx))
                    {
                        c.Parameters.AddWithValue("@i", _installmentId);
                        using var r = c.ExecuteReader();
                        if (!r.Read()) throw new Exception("القسط غير موجود.");
                        var instInterest = Convert.ToDecimal(r.GetValue(0));
                        var paidInterest = Convert.ToDecimal(r.GetValue(1));
                        var remInterest = Math.Max(0, instInterest - paidInterest);
                        interestPart = Math.Min(amount, remInterest);
                    }
                }
                var principalPart = _principalOnly ? amount : amount - interestPart;
                if (principalPart < 0) principalPart = 0;
                if (interestPart + principalPart > amount) interestPart = amount - principalPart;

                // 1) حركة الخزينة (الفعلية بالاتجاه الصحيح)
                int payRowId;
                using (var pay = new NpgsqlCommand(@"INSERT INTO payments (date, amount, payment_type, payment_method, reference_number, notes, cash_box_id, currency_id)
                    VALUES (@d, @a, @t, @m, @r, @n, @cb, @cur) RETURNING id", conn, tx))
                {
                    pay.Parameters.AddWithValue("@d", date);
                    pay.Parameters.AddWithValue("@a", amount);
                    pay.Parameters.AddWithValue("@t", _payType);
                    pay.Parameters.AddWithValue("@m", method);
                    pay.Parameters.AddWithValue("@r", string.IsNullOrEmpty(_refPrefix) ? DBNull.Value : (object)_refPrefix);
                    pay.Parameters.AddWithValue("@n", note);
                    pay.Parameters.AddWithValue("@cb", cb.Id);
                    pay.Parameters.AddWithValue("@cur", Techtoon.UI.Services.DbDefaults.GetDefaultCurrencyId(conn));
                    payRowId = Convert.ToInt32(pay.ExecuteScalar());
                }

                // 2) دفتر القرض: loan_payments + تحديث القسط
                if (_installmentId > 0)
                {
                    using (var lp = new NpgsqlCommand(@"INSERT INTO loan_payments (loan_id, installment_id, payment_date, amount,
                        principal_part, interest_part, method, cash_box_id, reference_number, notes, created_by)
                        VALUES (@loan, @li, @d, @a, @pp, @ip, @m, @cb, @r, @n, 1)", conn, tx))
                    {
                        lp.Parameters.AddWithValue("@loan", _loanId);
                        lp.Parameters.AddWithValue("@li", _installmentId);
                        lp.Parameters.AddWithValue("@d", date);
                        lp.Parameters.AddWithValue("@a", amount);
                        lp.Parameters.AddWithValue("@pp", principalPart);
                        lp.Parameters.AddWithValue("@ip", interestPart);
                        lp.Parameters.AddWithValue("@m", method);
                        lp.Parameters.AddWithValue("@cb", cb.Id);
                        lp.Parameters.AddWithValue("@r", string.IsNullOrEmpty(_refPrefix) ? DBNull.Value : (object)_refPrefix);
                        lp.Parameters.AddWithValue("@n", note);
                        lp.ExecuteNonQuery();
                    }
                    using (var upL = new NpgsqlCommand(@"UPDATE loan_installments SET
                        paid_amount = COALESCE(paid_amount,0) + @a,
                        paid_date = COALESCE(paid_date, @d),
                        status = CASE WHEN COALESCE(paid_amount,0) + @a >= COALESCE(total_amount,0) - 0.005 THEN 'paid' ELSE 'partial' END
                        WHERE id=@i", conn, tx))
                    { upL.Parameters.AddWithValue("@a", amount); upL.Parameters.AddWithValue("@d", date); upL.Parameters.AddWithValue("@i", _installmentId); upL.ExecuteNonQuery(); }
                }

                // 3) رأس القرض: paid/remaining + الإغلاق التلقائي عند الصفر
                if (_loanId > 0 && principalPart > 0)
                {
                    using (var up = new NpgsqlCommand(@"UPDATE loans SET
                        paid_amount = COALESCE(paid_amount,0) + @pp,
                        remaining_amount = GREATEST(COALESCE(remaining_amount,0) - @pp, 0),
                        status = CASE WHEN COALESCE(remaining_amount,0) - @pp <= 0.005 THEN 'closed' ELSE status END
                        WHERE id=@id", conn, tx))
                    {
                        up.Parameters.AddWithValue("@pp", principalPart);
                        up.Parameters.AddWithValue("@id", _loanId);
                        up.ExecuteNonQuery();
                    }
                }

                tx.Commit();
                AuditService.Log("PAYMENT", "Loan", _loanId, "Loan payment recorded (ref " + _refPrefix + ", cash row " + payRowId + ")");
                DialogResult = true; Close();
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ التسديد: " + ex.Message; }
        }

        public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();
    }
}
