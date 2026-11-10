using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Npgsql;
using Techtoon.UI.Services;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    // ============================================================
    // نافذة إنشاء/تعديل اشتراك + توليد الفترات
    // ============================================================
    public class SubscriptionEditWindow : Window
    {
        private readonly int _id;
        private readonly SubscriptionsViewModel _vm;
        private readonly ComboBox _cmbParty = new() { DisplayMemberPath = "Name", Margin = new Thickness(4) };
        private readonly TextBox _txtService = new() { Margin = new Thickness(4) };
        private readonly TextBox _txtAmount = new() { Margin = new Thickness(4), Text = "0" };
        private readonly ComboBox _cmbFreq = new() { Margin = new Thickness(4), Width = 120 };
        private readonly DatePicker _dpStart = new() { Margin = new Thickness(4), SelectedDate = DateTime.Today };
        private readonly TextBox _txtPeriods = new() { Margin = new Thickness(4), Text = "12" };
        private readonly CheckBox _chkAuto = new() { Content = "إنشاء سند دفع تلقائي عند الاستحقاق", IsChecked = true, Margin = new Thickness(4) };
        private readonly TextBox _txtNotes = new() { Margin = new Thickness(4), Height = 45, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };
        private readonly TextBlock _lblStatus = new() { Foreground = System.Windows.Media.Brushes.Red, TextWrapping = TextWrapping.Wrap };

        public SubscriptionEditWindow(int id, SubscriptionsViewModel vm)
        {
            _id = id;
            _vm = vm;
            Title = id > 0 ? "تعديل اشتراك" : "إضافة اشتراك";
            Width = 470; Height = 560; WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FlowDirection = FlowDirection.RightToLeft;
            _cmbFreq.Items.Add(new ComboItem { Id = 0, Name = "شهري" });
            _cmbFreq.Items.Add(new ComboItem { Id = 1, Name = "سنوي" });
            _cmbFreq.SelectedIndex = 0;

            var grid = new Grid { Margin = new Thickness(12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int i = 0; i < 10; i++) grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            Row(grid, 0, "العميل:"); grid.Children.Add(_cmbParty); Grid.SetRow(_cmbParty, 0); Grid.SetColumn(_cmbParty, 1);
            Row(grid, 1, "الخدمة:"); grid.Children.Add(_txtService); Grid.SetRow(_txtService, 1); Grid.SetColumn(_txtService, 1);
            Row(grid, 2, "المبلغ:"); grid.Children.Add(_txtAmount); Grid.SetRow(_txtAmount, 2); Grid.SetColumn(_txtAmount, 1);
            Row(grid, 3, "الدورية:"); grid.Children.Add(_cmbFreq); Grid.SetRow(_cmbFreq, 3); Grid.SetColumn(_cmbFreq, 1);
            Row(grid, 4, "تاريخ بداية الاشتراك:"); grid.Children.Add(_dpStart); Grid.SetRow(_dpStart, 4); Grid.SetColumn(_dpStart, 1);
            Row(grid, 5, "عدد الفترات:"); grid.Children.Add(_txtPeriods); Grid.SetRow(_txtPeriods, 5); Grid.SetColumn(_txtPeriods, 1);
            Row(grid, 6, "الفوترة التلقائية:"); grid.Children.Add(_chkAuto); Grid.SetRow(_chkAuto, 6); Grid.SetColumn(_chkAuto, 1);
            Row(grid, 7, "ملاحظات:"); grid.Children.Add(_txtNotes); Grid.SetRow(_txtNotes, 7); Grid.SetColumn(_txtNotes, 1);
            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
            var save = new Button { Content = "حفظ", Padding = new Thickness(20, 6, 20, 6), Margin = new Thickness(4) };
            var cancel = new Button { Content = "إلغاء", Padding = new Thickness(20, 6, 20, 6), Margin = new Thickness(4) };
            save.Click += Save_Click; cancel.Click += (s, e) => Close();
            btns.Children.Add(save); btns.Children.Add(cancel);
            Grid.SetRow(btns, 8); grid.Children.Add(btns); Grid.SetColumn(btns, 1);
            Grid.SetRow(_lblStatus, 9); grid.Children.Add(_lblStatus); Grid.SetColumn(_lblStatus, 1);
            Content = new ScrollViewer { Content = grid };

            LoadParties();
            if (_id > 0) LoadData();
        }

        private static void Row(Grid g, int i, string label)
        {
            g.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold });
            Grid.SetRow(g.Children[g.Children.Count - 1], i); Grid.SetColumn(g.Children[g.Children.Count - 1], 0);
        }

        private void LoadParties()
        {
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                _cmbParty.Items.Clear();
                using var cmd = new NpgsqlCommand("SELECT id, name FROM parties WHERE COALESCE(is_active, TRUE) ORDER BY name", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read()) _cmbParty.Items.Add(new ComboItem { Id = r.GetInt32(0), Name = r.GetString(1) });
                if (_cmbParty.Items.Count > 0) _cmbParty.SelectedIndex = 0;
                else _lblStatus.Text = "لا يوجد عملاء — أضف عميلًا أولًا من صفحة العملاء.";
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ: " + ex.Message; }
        }

        private void LoadData()
        {
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var cmd = new NpgsqlCommand(@"SELECT party_id, service_name, amount, frequency, start_date, auto_invoice, notes FROM subscriptions WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("@id", _id);
                using var r = cmd.ExecuteReader();
                if (r.Read())
                {
                    Pick(_cmbParty, r.IsDBNull(0) ? 0 : r.GetInt32(0));
                    _txtService.Text = r.GetString(1);
                    _txtAmount.Text = Convert.ToDecimal(r.GetValue(2)).ToString("0.##");
                    _cmbFreq.SelectedIndex = r.GetString(3) == "yearly" ? 1 : 0;
                    _dpStart.SelectedDate = r.IsDBNull(4) ? DateTime.Today : Techtoon.Core.Data.DbValue.AsDateTime(r.GetValue(4));
                    _chkAuto.IsChecked = !r.IsDBNull(5) && r.GetBoolean(5);
                    _txtNotes.Text = r.GetString(6);
                }
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ: " + ex.Message; }
        }

        private static void Pick(ComboBox c, int id)
        { for (int i = 0; i < c.Items.Count; i++) if (((ComboItem)c.Items[i]).Id == id) { c.SelectedIndex = i; return; } }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_cmbParty.SelectedItem is not ComboItem party) { _lblStatus.Text = "اختر العميل (إلزامي)."; return; }
            if (string.IsNullOrWhiteSpace(_txtService.Text)) { _lblStatus.Text = "اسم الخدمة مطلوب."; return; }
            if (!decimal.TryParse(_txtAmount.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var amount) || amount <= 0) { _lblStatus.Text = "المبلغ غير صحيح."; return; }
            if (!int.TryParse(_txtPeriods.Text, out var periods) || periods <= 0) { _lblStatus.Text = "عدد الفترات غير صحيح."; return; }
            var freq = _cmbFreq.SelectedIndex == 1 ? "yearly" : "monthly";
            var start = _dpStart.SelectedDate ?? DateTime.Today;

            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var tx = conn.BeginTransaction();
                int subId;
                if (_id > 0)
                {
                    using var up = new NpgsqlCommand(@"UPDATE subscriptions SET party_id=@p, service_name=@s, amount=@a, frequency=@f,
                        start_date=@sd, auto_invoice=@ai, notes=@n WHERE id=@id", conn, tx);
                    up.Parameters.AddWithValue("p", party.Id); up.Parameters.AddWithValue("s", _txtService.Text.Trim());
                    up.Parameters.AddWithValue("a", amount); up.Parameters.AddWithValue("f", freq);
                    up.Parameters.AddWithValue("sd", start); up.Parameters.AddWithValue("ai", (bool)(_chkAuto.IsChecked == true));
                    up.Parameters.AddWithValue("n", string.IsNullOrWhiteSpace(_txtNotes.Text) ? DBNull.Value : (object)_txtNotes.Text);
                    up.Parameters.AddWithValue("id", _id); up.ExecuteNonQuery();
                    subId = _id;
                }
                else
                {
                    using var ins = new NpgsqlCommand(@"INSERT INTO subscriptions (party_id, service_name, description, amount, frequency,
                        start_date, auto_invoice, status, notes, created_by, created_at)
                        VALUES (@p,@s,@d,@a,@f,@sd,@ai,'active',@n,1,NOW()) RETURNING id", conn, tx);
                    ins.Parameters.AddWithValue("p", party.Id); ins.Parameters.AddWithValue("s", _txtService.Text.Trim());
                    ins.Parameters.AddWithValue("d", _txtService.Text.Trim());
                    ins.Parameters.AddWithValue("a", amount); ins.Parameters.AddWithValue("f", freq);
                    ins.Parameters.AddWithValue("sd", start); ins.Parameters.AddWithValue("ai", (bool)(_chkAuto.IsChecked == true));
                    ins.Parameters.AddWithValue("n", string.IsNullOrWhiteSpace(_txtNotes.Text) ? DBNull.Value : (object)_txtNotes.Text);
                    subId = Convert.ToInt32(ins.ExecuteScalar());
                }

                // توليد الفترات (شهرية/سنوية)
                GeneratePeriods(conn, tx, subId, amount, freq, start, periods, _id == 0);
                tx.Commit();
                AuditService.Log(_id > 0 ? "UPDATE" : "CREATE", "Subscription", subId,
                    "Subscription saved: " + _txtService.Text.Trim() + " (" + (freq == "yearly" ? "سنوي" : "شهري") + ")");
                DialogResult = true;
                Close();
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ الحفظ: " + ex.Message; }
        }

        /// <summary>توليد فترات الاستحقاق: شهرية (نهاية الشهر) أو سنوية.</summary>
        private static void GeneratePeriods(NpgsqlConnection conn, NpgsqlTransaction tx, int subId, decimal amount,
            string freq, DateTime start, int count, bool replaceExisting)
        {
            if (replaceExisting)
            {
                using var del = new NpgsqlCommand("DELETE FROM subscription_periods WHERE subscription_id=@id", conn, tx);
                del.Parameters.AddWithValue("id", subId); del.ExecuteNonQuery();
            }
            else
            {
                using var del = new NpgsqlCommand("DELETE FROM subscription_periods WHERE subscription_id=@id AND paid_amount=0", conn, tx);
                del.Parameters.AddWithValue("id", subId); del.ExecuteNonQuery();
            }

            for (int i = 0; i < count; i++)
            {
                var periodStart = freq == "yearly" ? start.AddYears(i) : start.AddMonths(i);
                var periodEnd = freq == "yearly" ? periodStart.AddYears(1).AddDays(-1) : periodStart.AddMonths(1).AddDays(-1);
                var due = periodEnd;
                using var ins = new NpgsqlCommand(@"INSERT INTO subscription_periods (subscription_id, period_number, period_start, period_end,
                    due_date, amount, paid_amount, status) VALUES (@sub,@n,@ps,@pe,@due,@amt,0,'due')", conn, tx);
                ins.Parameters.AddWithValue("sub", subId);
                ins.Parameters.AddWithValue("n", i + 1);
                ins.Parameters.AddWithValue("ps", periodStart);
                ins.Parameters.AddWithValue("pe", periodEnd);
                ins.Parameters.AddWithValue("due", due);
                ins.Parameters.AddWithValue("amt", amount);
                ins.ExecuteNonQuery();
            }
        }

        public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();
    }

    // ============================================================
    // نافذة سداد فترة — تنشئ سند دفع حقيقيًا (cash) ويربطه بالفترة
    // ============================================================
    public class SubscriptionPaymentWindow : Window
    {
        private readonly SubscriptionPeriodRow _period;
        private readonly TextBox _txtAmount = new() { Margin = new Thickness(4) };
        private readonly DatePicker _dpDate = new() { Margin = new Thickness(4), SelectedDate = DateTime.Today };
        private readonly ComboBox _cmbCB = new() { DisplayMemberPath = "Name", Margin = new Thickness(4) };
        private readonly TextBox _txtNotes = new() { Margin = new Thickness(4), Height = 40, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };
        private readonly TextBlock _lblStatus = new() { Foreground = System.Windows.Media.Brushes.Red, TextWrapping = TextWrapping.Wrap };

        public SubscriptionPaymentWindow(SubscriptionPeriodRow period)
        {
            _period = period;
            Title = "سداد فترة اشتراك";
            Width = 440; Height = 420; WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FlowDirection = FlowDirection.RightToLeft;
            _txtAmount.Text = period.Remaining.ToString("0.##");

            var grid = new Grid { Margin = new Thickness(12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int i = 0; i < 7; i++) grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            Row(grid, 0, "الاشتراك:"); grid.Children.Add(new TextBlock { Text = period.ServiceName + " — " + period.PartyName, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4) }); Grid.SetRow(grid.Children[grid.Children.Count - 1], 0); Grid.SetColumn(grid.Children[grid.Children.Count - 1], 1);
            Row(grid, 1, "المبلغ:"); grid.Children.Add(_txtAmount); Grid.SetRow(_txtAmount, 1); Grid.SetColumn(_txtAmount, 1);
            Row(grid, 2, "التاريخ:"); grid.Children.Add(_dpDate); Grid.SetRow(_dpDate, 2); Grid.SetColumn(_dpDate, 1);
            Row(grid, 3, "الخزينة:"); grid.Children.Add(_cmbCB); Grid.SetRow(_cmbCB, 3); Grid.SetColumn(_cmbCB, 1);
            Row(grid, 4, "ملاحظات:"); grid.Children.Add(_txtNotes); Grid.SetRow(_txtNotes, 4); Grid.SetColumn(_txtNotes, 1);
            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
            var save = new Button { Content = "تسديد", Padding = new Thickness(20, 6, 20, 6), Margin = new Thickness(4) };
            var cancel = new Button { Content = "إلغاء", Padding = new Thickness(20, 6, 20, 6), Margin = new Thickness(4) };
            save.Click += Save_Click; cancel.Click += (s, e) => Close();
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
                if (_cmbCB.Items.Count > 0) _cmbCB.SelectedIndex = 0;
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ: " + ex.Message; }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(_txtAmount.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var amount) || amount <= 0) { _lblStatus.Text = "المبلغ غير صحيح."; return; }
            if (amount > _period.Remaining + 0.005m) { _lblStatus.Text = "المبلغ أكبر من المتبقي (" + _period.Remaining.ToString("N2") + ")."; return; }
            if (_cmbCB.SelectedItem is not ComboItem cb) { _lblStatus.Text = "اختر الخزينة (إلزامي)."; return; }
            var date = _dpDate.SelectedDate ?? DateTime.Today;
            var note = string.IsNullOrWhiteSpace(_txtNotes.Text)
                ? $"سداد اشتراك: {_period.ServiceName} - {_period.PartyName} (فترة {_period.PeriodNumber})"
                : _txtNotes.Text;
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var tx = conn.BeginTransaction();

                // 1) سند دفع حقيقي (يدخل الخزينة) — مرجعه يربطه بالفترة
                int paymentId;
                using (var pay = new NpgsqlCommand(@"INSERT INTO payments (date, amount, payment_type, payment_method, reference_number, notes, cash_box_id, currency_id)
                    VALUES (@d,@a,'receipt','cash',@ref,@n,@cb,@cur) RETURNING id", conn, tx))
                {
                    pay.Parameters.AddWithValue("d", date);
                    pay.Parameters.AddWithValue("a", amount);
                    pay.Parameters.AddWithValue("ref", "SUB:" + _period.SubscriptionId + ":" + _period.Id);
                    pay.Parameters.AddWithValue("n", note);
                    pay.Parameters.AddWithValue("cb", cb.Id);
                    pay.Parameters.AddWithValue("cur", Techtoon.UI.Services.DbDefaults.GetDefaultCurrencyId(conn));
                    paymentId = Convert.ToInt32(pay.ExecuteScalar());
                }

                // 2) تحديث الفترة
                using (var up = new NpgsqlCommand(@"UPDATE subscription_periods SET paid_amount = paid_amount + @a,
                    paid_at = COALESCE(paid_at, @d), payment_id = @pid,
                    status = CASE WHEN paid_amount + @a >= amount - 0.005 THEN 'paid' ELSE 'due' END
                    WHERE id=@id", conn, tx))
                {
                    up.Parameters.AddWithValue("a", amount);
                    up.Parameters.AddWithValue("d", date);
                    up.Parameters.AddWithValue("pid", paymentId);
                    up.Parameters.AddWithValue("id", _period.Id);
                    up.ExecuteNonQuery();
                }

                tx.Commit();
                AuditService.Log("PAYMENT", "Subscription", _period.SubscriptionId,
                    "Subscription period paid: period " + _period.PeriodNumber + ", amount " + amount + ", cash row " + paymentId);
                DialogResult = true;
                Close();
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ السداد: " + ex.Message; }
        }

        public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();
    }
}
