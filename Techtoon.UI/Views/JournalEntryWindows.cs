using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Npgsql;
using NpgsqlTypes;
using Techtoon.Core.Data;
using Techtoon.UI.Services;

namespace Techtoon.UI.Views
{
    /// <summary>خيار حساب في قائمة الاختيار.</summary>
    public class JournalAccountOption
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string Display => string.IsNullOrWhiteSpace(Code) ? Name : (Code + " - " + Name);
    }

    /// <summary>صف بند داخل نافذة القيد.</summary>
    internal class JournalLineRow
    {
        public ComboBox AccountBox { get; set; } = null!;
        public TextBox DebitBox { get; set; } = null!;
        public TextBox CreditBox { get; set; } = null!;
        public FrameworkElement Host { get; set; } = null!;
    }

    /// <summary>
    /// نافذة إضافة وتعديل قيد محاسبي يدوي: التاريخ + الوصف + بنود (حساب + مدين أو دائن).
    /// التحقق من التوازن يتم قبل الحفظ، والحفظ يتم في معاملة واحدة مع سجل تدقيق.
    /// </summary>
    public class JournalEntryEditWindow : Window
    {
        private readonly int? _entryId;
        private readonly bool _readOnly;
        private readonly List<JournalAccountOption> _accounts = new();
        private readonly List<JournalLineRow> _lines = new();
        private readonly StackPanel _linesPanel = new();
        private readonly TextBlock _lblTotals = new();
        private readonly TextBlock _lblStatus = new();
        private readonly TextBox _txtDescription = new() { Margin = new Thickness(4), Padding = new Thickness(6, 4, 6, 4) };
        private readonly DatePicker _dpDate = new() { Margin = new Thickness(4), Padding = new Thickness(6, 4, 6, 4), SelectedDate = DateTime.Today };

        public static string GetCS() => SecureConnectionManager.GetConnectionString();

        public JournalEntryEditWindow(int? entryId = null, bool readOnly = false)
        {
            _entryId = entryId;
            _readOnly = readOnly;

            Title = _readOnly ? "عرض بنود القيد المحاسبي" : (_entryId.HasValue ? "تعديل قيد محاسبي" : "قيد محاسبي جديد");
            Width = 880;
            Height = 680;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            FlowDirection = FlowDirection.RightToLeft;
            FontFamily = new FontFamily("Tahoma");
            Background = new SolidColorBrush(Color.FromRgb(245, 247, 250));

            var root = new Grid { Margin = new Thickness(14) };
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            var title = new TextBlock
            {
                Text = Title,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 105, 92)),
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(title, 0);
            root.Children.Add(title);

            // ===== الترويسة: التاريخ + الوصف =====
            var body = new StackPanel();

            var head = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
            head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var lblDate = new TextBlock { Text = "التاريخ:", FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4) };
            Grid.SetColumn(lblDate, 0);
            Grid.SetColumn(_dpDate, 1);
            _dpDate.IsEnabled = !_readOnly;

            var lblDesc = new TextBlock { Text = "الوصف:", FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4) };
            Grid.SetColumn(lblDesc, 2);
            Grid.SetColumn(_txtDescription, 3);
            _txtDescription.IsReadOnly = _readOnly;

            head.Children.Add(lblDate);
            head.Children.Add(_dpDate);
            head.Children.Add(lblDesc);
            head.Children.Add(_txtDescription);
            body.Children.Add(head);

            // ===== عنوان جدول البنود =====
            var linesHeader = BuildLineHeader();
            body.Children.Add(linesHeader);

            // ===== البنود =====
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, MaxHeight = 330, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
            scroll.Content = _linesPanel;
            body.Children.Add(scroll);

            // ===== المجاميع =====
            _lblTotals.FontWeight = FontWeights.SemiBold;
            _lblTotals.Foreground = new SolidColorBrush(Color.FromRgb(33, 33, 33));
            _lblTotals.Margin = new Thickness(4, 8, 4, 4);
            _lblTotals.TextWrapping = TextWrapping.Wrap;
            body.Children.Add(_lblTotals);

            Grid.SetRow(body, 1);
            root.Children.Add(body);

            // ===== الأزرار =====
            var footer = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 12, 0, 0) };
            var btnSave = new Button
            {
                Content = "حفظ القيد",
                Background = new SolidColorBrush(Color.FromRgb(0, 137, 123)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                Padding = new Thickness(22, 8, 22, 8),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(4),
                IsEnabled = !_readOnly
            };
            btnSave.Click += BtnSave_Click;

            var btnAddLine = new Button
            {
                Content = "+ سطر جديد",
                Background = new SolidColorBrush(Color.FromRgb(21, 101, 192)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                Padding = new Thickness(18, 8, 18, 8),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(4),
                IsEnabled = !_readOnly
            };
            btnAddLine.Click += (s, e) => AddLine();

            var btnCancel = new Button
            {
                Content = "إلغاء",
                Background = new SolidColorBrush(Color.FromRgb(117, 117, 117)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                Padding = new Thickness(22, 8, 22, 8),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(4)
            };
            btnCancel.Click += (s, e) => { DialogResult = false; Close(); };

            footer.Children.Add(btnSave);
            footer.Children.Add(btnAddLine);
            footer.Children.Add(btnCancel);

            _lblStatus.Foreground = Brushes.Firebrick;
            _lblStatus.TextWrapping = TextWrapping.Wrap;
            _lblStatus.Margin = new Thickness(4, 8, 4, 0);
            _lblStatus.HorizontalAlignment = HorizontalAlignment.Center;

            var statusHost = new StackPanel();
            statusHost.Children.Add(footer);
            statusHost.Children.Add(_lblStatus);
            Grid.SetRow(statusHost, 2);
            root.Children.Add(statusHost);

            Content = root;

            LoadAccounts();
            if (_entryId.HasValue) LoadEntry();
            else { AddLine(); AddLine(); }
            UpdateTotals();
        }

        private Grid BuildLineHeader()
        {
            var g = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

            var c0 = new TextBlock { Text = "الحساب", FontWeight = FontWeights.Bold, Margin = new Thickness(4) };
            var c1 = new TextBlock { Text = "مدين", FontWeight = FontWeights.Bold, Margin = new Thickness(4), HorizontalAlignment = HorizontalAlignment.Center };
            var c2 = new TextBlock { Text = "دائن", FontWeight = FontWeights.Bold, Margin = new Thickness(4), HorizontalAlignment = HorizontalAlignment.Center };
            var c3 = new TextBlock { Text = "إزالة", FontWeight = FontWeights.Bold, Margin = new Thickness(4), HorizontalAlignment = HorizontalAlignment.Center };
            Grid.SetColumn(c0, 0); Grid.SetColumn(c1, 1); Grid.SetColumn(c2, 2); Grid.SetColumn(c3, 3);
            g.Children.Add(c0); g.Children.Add(c1); g.Children.Add(c2); g.Children.Add(c3);
            return g;
        }

        private void LoadAccounts()
        {
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var cmd = new NpgsqlCommand("SELECT id, COALESCE(code, ''), COALESCE(name, '') FROM accounts ORDER BY code, name", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    _accounts.Add(new JournalAccountOption
                    {
                        Id = r.GetInt32(0),
                        Code = r.IsDBNull(1) ? "" : r.GetString(1),
                        Name = r.IsDBNull(2) ? "" : r.GetString(2)
                    });
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ في تحميل الحسابات: " + ex.Message; }
        }

        private static decimal ToDecimal(object? value)
        {
            if (value == null || value == DBNull.Value) return 0m;
            if (value is decimal d) return d;
            return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        }

        private void LoadEntry()
        {
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using (var cmd = new NpgsqlCommand("SELECT date, COALESCE(description, '') FROM journal_entries WHERE id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", _entryId!.Value);
                    using var r = cmd.ExecuteReader();
                    if (r.Read())
                    {
                        var d = DbValue.AsDateTime(r.GetValue(0));
                        if (d != default) _dpDate.SelectedDate = DateTime.SpecifyKind(d.Date, DateTimeKind.Unspecified);
                        _txtDescription.Text = r.IsDBNull(1) ? "" : r.GetString(1);
                    }
                }
                using (var cmd = new NpgsqlCommand(
                    "SELECT account_id, COALESCE(debit, 0), COALESCE(credit, 0) FROM journal_lines WHERE entry_id = @id ORDER BY id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", _entryId!.Value);
                    using var r = cmd.ExecuteReader();
                    while (r.Read())
                        AddLine(r.GetInt32(0), ToDecimal(r.GetValue(1)), ToDecimal(r.GetValue(2)));
                }
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ في تحميل القيد: " + ex.Message; }
        }

        private void AddLine(int accountId = 0, decimal debit = 0m, decimal credit = 0m)
        {
            var accountBox = new ComboBox
            {
                Margin = new Thickness(4),
                Padding = new Thickness(6, 4, 6, 4),
                ItemsSource = _accounts,
                DisplayMemberPath = "Display",
                IsEnabled = !_readOnly
            };
            var debitBox = new TextBox { Margin = new Thickness(4), Padding = new Thickness(6, 4, 6, 4), IsReadOnly = _readOnly };
            var creditBox = new TextBox { Margin = new Thickness(4), Padding = new Thickness(6, 4, 6, 4), IsReadOnly = _readOnly };

            if (debit != 0m) debitBox.Text = debit.ToString("0.##", CultureInfo.InvariantCulture);
            if (credit != 0m) creditBox.Text = credit.ToString("0.##", CultureInfo.InvariantCulture);
            if (accountId > 0)
            {
                for (int i = 0; i < _accounts.Count; i++)
                    if (_accounts[i].Id == accountId) { accountBox.SelectedIndex = i; break; }
            }

            var row = new JournalLineRow { AccountBox = accountBox, DebitBox = debitBox, CreditBox = creditBox };

            var removeBtn = new Button
            {
                Content = "حذف",
                Margin = new Thickness(4),
                Padding = new Thickness(6, 4, 6, 4),
                IsEnabled = !_readOnly
            };
            removeBtn.Click += (s, e) => RemoveLine(row);

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            Grid.SetColumn(accountBox, 0);
            Grid.SetColumn(debitBox, 1);
            Grid.SetColumn(creditBox, 2);
            Grid.SetColumn(removeBtn, 3);
            grid.Children.Add(accountBox);
            grid.Children.Add(debitBox);
            grid.Children.Add(creditBox);
            grid.Children.Add(removeBtn);
            row.Host = grid;

            _lines.Add(row);
            _linesPanel.Children.Add(grid);

            debitBox.TextChanged += (s, e) => UpdateTotals();
            creditBox.TextChanged += (s, e) => UpdateTotals();
        }

        private void RemoveLine(JournalLineRow row)
        {
            if (_readOnly) return;
            _lines.Remove(row);
            if (row.Host != null) _linesPanel.Children.Remove(row.Host);
            UpdateTotals();
        }

        private static bool TryParseAmount(string? text, out decimal value)
        {
            value = 0m;
            if (string.IsNullOrWhiteSpace(text)) return true;
            return decimal.TryParse(text.Trim(), NumberStyles.Any, CultureInfo.CurrentCulture, out value);
        }

        private void UpdateTotals()
        {
            decimal debit = 0m, credit = 0m;
            foreach (var line in _lines)
            {
                if (TryParseAmount(line.DebitBox.Text, out var d)) debit += d;
                if (TryParseAmount(line.CreditBox.Text, out var c)) credit += c;
            }
            _lblTotals.Text = "إجمالي المدين: " + debit.ToString("N2") +
                              "   —   إجمالي الدائن: " + credit.ToString("N2") +
                              "   —   الفرق: " + (debit - credit).ToString("N2");
        }

        /// <summary>إيجاد الفترة المحاسبية التي تحتوي تاريخ القيد (قد لا توجد).</summary>
        private static int? ResolvePeriodId(NpgsqlConnection conn, DateTime date)
        {
            try
            {
                using var cmd = new NpgsqlCommand(
                    "SELECT id FROM accounting_periods WHERE CAST(@d AS timestamp) BETWEEN start_date AND end_date ORDER BY id LIMIT 1", conn);
                cmd.Parameters.AddWithValue("@d", date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                var v = cmd.ExecuteScalar();
                if (v == null || v == DBNull.Value) return null;
                return Convert.ToInt32(v);
            }
            catch { return null; }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            _lblStatus.Text = "";

            if (string.IsNullOrWhiteSpace(_txtDescription.Text))
            {
                _lblStatus.Text = "وصف القيد مطلوب.";
                return;
            }

            var lines = new List<(int accountId, decimal debit, decimal credit)>();
            decimal totalDebit = 0m, totalCredit = 0m;
            int index = 1;

            foreach (var line in _lines)
            {
                var account = line.AccountBox.SelectedItem as JournalAccountOption;
                if (account == null)
                {
                    _lblStatus.Text = "يجب اختيار الحساب في السطر رقم " + index + ".";
                    return;
                }
                if (!TryParseAmount(line.DebitBox.Text, out var debit))
                {
                    _lblStatus.Text = "قيمة المدين في السطر رقم " + index + " غير صحيحة.";
                    return;
                }
                if (!TryParseAmount(line.CreditBox.Text, out var credit))
                {
                    _lblStatus.Text = "قيمة الدائن في السطر رقم " + index + " غير صحيحة.";
                    return;
                }
                if (debit < 0m || credit < 0m)
                {
                    _lblStatus.Text = "لا يمكن إدخال مبلغ سالب في السطر رقم " + index + ".";
                    return;
                }
                bool hasDebit = debit > 0m;
                bool hasCredit = credit > 0m;
                if (hasDebit && hasCredit)
                {
                    _lblStatus.Text = "السطر رقم " + index + ": لا يمكن إدخال مدين ودائن في نفس السطر — اختر أحدهما فقط.";
                    return;
                }
                if (!hasDebit && !hasCredit)
                {
                    _lblStatus.Text = "السطر رقم " + index + ": يجب إدخال مبلغ في المدين أو في الدائن.";
                    return;
                }
                totalDebit += debit;
                totalCredit += credit;
                lines.Add((account.Id, debit, credit));
                index++;
            }

            if (lines.Count < 2)
            {
                _lblStatus.Text = "يجب إدخال بندين على الأقل (مدين ودائن) حتى يمكن حفظ القيد.";
                return;
            }

            if (Math.Abs(totalDebit - totalCredit) >= 0.005m)
            {
                _lblStatus.Text = "القيد غير متوازن: إجمالي المدين " + totalDebit.ToString("N2") +
                                  " لا يساوي إجمالي الدائن " + totalCredit.ToString("N2") +
                                  " (الفرق " + (totalDebit - totalCredit).ToString("N2") + "). يجب أن يتساوى الطرفان قبل الحفظ.";
                return;
            }

            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                DateTime entryDate = _dpDate.SelectedDate ?? DateTime.Today;
                int? periodId = ResolvePeriodId(conn, entryDate);
                int? createdBy = AuthService.CurrentUser?.Id;

                using var tx = conn.BeginTransaction();
                int entryId;

                if (_entryId.HasValue)
                {
                    entryId = _entryId.Value;
                    using (var upd = new NpgsqlCommand(
                        "UPDATE journal_entries SET date = CAST(@d AS timestamp), description = @desc WHERE id = @id", conn, tx))
                    {
                        upd.Parameters.AddWithValue("@d", entryDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                        upd.Parameters.AddWithValue("@desc", _txtDescription.Text.Trim());
                        upd.Parameters.AddWithValue("@id", entryId);
                        upd.ExecuteNonQuery();
                    }
                    using (var del = new NpgsqlCommand("DELETE FROM journal_lines WHERE entry_id = @id", conn, tx))
                    {
                        del.Parameters.AddWithValue("@id", entryId);
                        del.ExecuteNonQuery();
                    }
                }
                else
                {
                    using (var ins = new NpgsqlCommand(
                        "INSERT INTO journal_entries (date, description, reference_type, created_by, period_id, cash_basis_posted) " +
                        "VALUES (CAST(@d AS timestamp), @desc, 'manual', @by, @pid, FALSE) RETURNING id", conn, tx))
                    {
                        ins.Parameters.AddWithValue("@d", entryDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                        ins.Parameters.AddWithValue("@desc", _txtDescription.Text.Trim());
                        ins.Parameters.Add(new NpgsqlParameter("@by", NpgsqlDbType.Integer) { Value = createdBy.HasValue ? (object)createdBy.Value : DBNull.Value });
                        ins.Parameters.Add(new NpgsqlParameter("@pid", NpgsqlDbType.Integer) { Value = periodId.HasValue ? (object)periodId.Value : DBNull.Value });
                        var v = ins.ExecuteScalar();
                        entryId = v == null || v == DBNull.Value ? 0 : Convert.ToInt32(v);
                    }
                }

                foreach (var l in lines)
                {
                    using (var ins = new NpgsqlCommand(
                        "INSERT INTO journal_lines (entry_id, account_id, debit, credit) VALUES (@eid, @aid, @d, @c)", conn, tx))
                    {
                        ins.Parameters.AddWithValue("@eid", entryId);
                        ins.Parameters.AddWithValue("@aid", l.accountId);
                        ins.Parameters.AddWithValue("@d", l.debit);
                        ins.Parameters.AddWithValue("@c", l.credit);
                        ins.ExecuteNonQuery();
                    }
                }

                tx.Commit();
                AuditService.Log(_entryId.HasValue ? "UPDATE" : "CREATE", "JournalEntry", entryId,
                    "Manual journal entry date=" + entryDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) +
                    " lines=" + lines.Count + " total=" + totalDebit.ToString("N2", CultureInfo.InvariantCulture));
                DialogResult = true;
                Close();
            }
            catch (Npgsql.PostgresException px)
            {
                _lblStatus.Text = "خطأ في قاعدة البيانات: " + px.Message;
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "خطأ: " + ex.Message;
            }
        }
    }
}
