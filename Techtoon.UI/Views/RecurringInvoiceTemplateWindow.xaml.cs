using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Npgsql;
using Techtoon.Core.Data;
using Techtoon.UI.Services;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    /// <summary>P5: نافذة قالب الفاتورة المتكررة (بيانات القالب + أصنافه).</summary>
    public class RecurringInvoiceTemplateWindow : Window
    {
        private readonly int _id;
        private readonly RecurringInvoicesViewModel _vm;
        private readonly TextBox _txtName = new() { Margin = new Thickness(4) };
        private readonly ComboBox _cmbParty = new() { DisplayMemberPath = "Name", Margin = new Thickness(4) };
        private readonly ComboBox _cmbType = new() { Margin = new Thickness(4), Width = 110 };
        private readonly ComboBox _cmbFreq = new() { Margin = new Thickness(4), Width = 110 };
        private readonly DatePicker _dpStart = new() { Margin = new Thickness(4), SelectedDate = DateTime.Today };
        private readonly DatePicker _dpNext = new() { Margin = new Thickness(4), SelectedDate = DateTime.Today.AddMonths(1) };
        private readonly TextBox _txtDiscount = new() { Margin = new Thickness(4), Text = "0" };
        private readonly TextBox _txtTax = new() { Margin = new Thickness(4), Text = "0" };
        private readonly CheckBox _chkActive = new() { Content = "نشط", IsChecked = true, Margin = new Thickness(4) };
        private readonly TextBox _txtNotes = new() { Margin = new Thickness(4), Height = 40, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };
        private readonly ComboBox _cmbItem = new() { DisplayMemberPath = "Name", Margin = new Thickness(4) };
        private readonly TextBox _txtQty = new() { Margin = new Thickness(4), Text = "1", Width = 70 };
        private readonly TextBox _txtPrice = new() { Margin = new Thickness(4), Text = "0", Width = 90 };
        private readonly DataGrid _grid = new()
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            CanUserAddRows = false,
            HeadersVisibility = DataGridHeadersVisibility.Column
        };
        private readonly TextBlock _lblStatus = new() { Foreground = System.Windows.Media.Brushes.Red, TextWrapping = TextWrapping.Wrap };

        public RecurringInvoiceTemplateWindow(int id, RecurringInvoicesViewModel vm)
        {
            _id = id; _vm = vm;
            Title = id > 0 ? "تعديل قالب فاتورة متكررة" : "قالب فاتورة متكررة جديد";
            Width = 620; Height = 700; WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FlowDirection = FlowDirection.RightToLeft;

            _cmbType.Items.Add("مبيعات"); _cmbType.Items.Add("مشتريات"); _cmbType.SelectedIndex = 0;
            _cmbFreq.Items.Add("شهري"); _cmbFreq.Items.Add("أسبوعي"); _cmbFreq.Items.Add("ربع سنوي"); _cmbFreq.Items.Add("سنوي");
            _cmbFreq.SelectedIndex = 0;

            _grid.Columns.Add(new DataGridTextColumn { Header = "الصنف", Binding = new System.Windows.Data.Binding("ItemName"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            _grid.Columns.Add(new DataGridTextColumn { Header = "الكمية", Binding = new System.Windows.Data.Binding("QuantityDisplay"), Width = 90 });
            _grid.Columns.Add(new DataGridTextColumn { Header = "السعر", Binding = new System.Windows.Data.Binding("PriceDisplay"), Width = 90 });
            _grid.Columns.Add(new DataGridTextColumn { Header = "الإجمالي", Binding = new System.Windows.Data.Binding("TotalDisplay"), Width = 90 });

            var grid = new Grid { Margin = new Thickness(10) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int i = 0; i < 12; i++) grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            Row(grid, 0, "اسم القالب:"); grid.Children.Add(_txtName); Grid.SetRow(_txtName, 0); Grid.SetColumn(_txtName, 1);
            Row(grid, 1, "العميل:"); grid.Children.Add(_cmbParty); Grid.SetRow(_cmbParty, 1); Grid.SetColumn(_cmbParty, 1);
            Row(grid, 2, "نوع الفاتورة:"); grid.Children.Add(_cmbType); Grid.SetRow(_cmbType, 2); Grid.SetColumn(_cmbType, 1);
            Row(grid, 3, "الدورية:"); grid.Children.Add(_cmbFreq); Grid.SetRow(_cmbFreq, 3); Grid.SetColumn(_cmbFreq, 1);
            Row(grid, 4, "تاريخ البداية:"); grid.Children.Add(_dpStart); Grid.SetRow(_dpStart, 4); Grid.SetColumn(_dpStart, 1);
            Row(grid, 5, "تاريخ التشغيل التالي:"); grid.Children.Add(_dpNext); Grid.SetRow(_dpNext, 5); Grid.SetColumn(_dpNext, 1);
            Row(grid, 6, "خصم %:"); grid.Children.Add(_txtDiscount); Grid.SetRow(_txtDiscount, 6); Grid.SetColumn(_txtDiscount, 1);
            Row(grid, 7, "ضريبة %:"); grid.Children.Add(_txtTax); Grid.SetRow(_txtTax, 7); Grid.SetColumn(_txtTax, 1);
            Row(grid, 8, "الحالة:"); grid.Children.Add(_chkActive); Grid.SetRow(_chkActive, 8); Grid.SetColumn(_chkActive, 1);
            Row(grid, 9, "ملاحظات:"); grid.Children.Add(_txtNotes); Grid.SetRow(_txtNotes, 9); Grid.SetColumn(_txtNotes, 1);

            var itemPanel = new StackPanel { Orientation = Orientation.Horizontal };
            itemPanel.Children.Add(_cmbItem);
            itemPanel.Children.Add(new TextBlock { Text = "كمية", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 0, 0) });
            itemPanel.Children.Add(_txtQty);
            itemPanel.Children.Add(new TextBlock { Text = "سعر", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 0, 0) });
            itemPanel.Children.Add(_txtPrice);
            var addLine = new Button { Content = "➕ إضافة صنف", Padding = new Thickness(10, 4, 10, 4), Margin = new Thickness(8, 0, 0, 0) };
            addLine.Click += AddLine_Click;
            itemPanel.Children.Add(addLine);
            var delLine = new Button { Content = "🗑️ حذف الصنف", Padding = new Thickness(10, 4, 10, 4), Margin = new Thickness(6, 0, 0, 0) };
            delLine.Click += DelLine_Click;
            itemPanel.Children.Add(delLine);
            Grid.SetRow(itemPanel, 10); grid.Children.Add(itemPanel); Grid.SetColumn(itemPanel, 1);

            Grid.SetRow(_grid, 11); grid.Children.Add(_grid); Grid.SetColumn(_grid, 1);

            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
            var save = new Button { Content = "حفظ", Padding = new Thickness(20, 6, 20, 6), Margin = new Thickness(4) };
            var cancel = new Button { Content = "إلغاء", Padding = new Thickness(20, 6, 20, 6), Margin = new Thickness(4) };
            save.Click += Save_Click; cancel.Click += (s, e) => Close();
            btns.Children.Add(save); btns.Children.Add(cancel);
            Grid.SetRow(btns, 12); grid.Children.Add(btns); Grid.SetColumn(btns, 1);
            Grid.SetRow(_lblStatus, 13); grid.Children.Add(_lblStatus); Grid.SetColumn(_lblStatus, 1);

            Content = new ScrollViewer { Content = grid };

            LoadLookups();
            if (_id > 0) LoadData();
        }

        private static void Row(Grid g, int i, string label)
        {
            g.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold });
            Grid.SetRow(g.Children[g.Children.Count - 1], i); Grid.SetColumn(g.Children[g.Children.Count - 1], 0);
        }

        private void LoadLookups()
        {
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                _cmbParty.Items.Clear();
                using (var cmd = new NpgsqlCommand("SELECT id, name FROM parties WHERE COALESCE(is_active,TRUE)=TRUE ORDER BY name", conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) _cmbParty.Items.Add(new ComboItem { Id = r.GetInt32(0), Name = r.GetString(1) });
                if (_cmbParty.Items.Count > 0) _cmbParty.SelectedIndex = 0;

                _cmbItem.Items.Clear();
                using (var cmd = new NpgsqlCommand("SELECT id, name FROM items WHERE COALESCE(is_active,TRUE) AND COALESCE(is_kit,FALSE)=FALSE ORDER BY name", conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) _cmbItem.Items.Add(new ComboItem { Id = r.GetInt32(0), Name = r.GetString(1) });
                if (_cmbItem.Items.Count > 0) _cmbItem.SelectedIndex = 0;
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ: " + ex.Message; }
        }

        private readonly List<RecurringLineRow> _lines = new();

        private void AddLine_Click(object sender, RoutedEventArgs e)
        {
            if (_cmbItem.SelectedItem is not ComboItem item) { _lblStatus.Text = "اختر الصنف."; return; }
            if (!decimal.TryParse(_txtQty.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var qty) || qty <= 0) { _lblStatus.Text = "الكمية غير صحيحة."; return; }
            if (!decimal.TryParse(_txtPrice.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var price) || price < 0) { _lblStatus.Text = "السعر غير صحيح."; return; }
            _lines.Add(new RecurringLineRow { ItemId = item.Id, ItemName = item.Name, Quantity = qty, Price = price });
            _grid.ItemsSource = _lines;
            _lblStatus.Text = "";
        }

        private void DelLine_Click(object sender, RoutedEventArgs e)
        {
            var sel = _grid.SelectedItem as RecurringLineRow;
            if (sel == null) { _lblStatus.Text = "اختر صنفًا من الجدول لحذفه."; return; }
            _lines.Remove(sel);
            _grid.ItemsSource = _lines;
        }

        private void LoadData()
        {
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using (var cmd = new NpgsqlCommand(@"SELECT COALESCE(name,''), party_id, COALESCE(invoice_type,'sales'), COALESCE(frequency,'monthly'),
                    COALESCE(start_date,CURRENT_DATE), COALESCE(next_run_date,CURRENT_DATE),
                    COALESCE(discount_percentage,0), COALESCE(tax_rate,0), COALESCE(is_active,TRUE), COALESCE(notes,'')
                    FROM recurring_invoice_templates WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", _id);
                    using var r = cmd.ExecuteReader();
                    if (r.Read())
                    {
                        _txtName.Text = r.GetString(0);
                        Pick(_cmbParty, r.IsDBNull(1) ? 0 : r.GetInt32(1));
                        _cmbType.SelectedIndex = r.GetString(2) == "purchase" ? 1 : 0;
                        _cmbFreq.SelectedIndex = r.GetString(3) switch { "weekly" => 1, "quarterly" => 2, "yearly" => 3, _ => 0 };
                        _dpStart.SelectedDate = DbValue.AsDateTime(r.GetValue(4));
                        _dpNext.SelectedDate = DbValue.AsDateTime(r.GetValue(5));
                        _txtDiscount.Text = Convert.ToDecimal(r.GetValue(6)).ToString("0.##");
                        _txtTax.Text = Convert.ToDecimal(r.GetValue(7)).ToString("0.##");
                        _chkActive.IsChecked = r.GetBoolean(8);
                        _txtNotes.Text = r.GetString(9);
                    }
                }
                using (var cmd = new NpgsqlCommand("SELECT item_id, quantity, price FROM recurring_invoice_lines WHERE template_id=@t ORDER BY id", conn))
                {
                    cmd.Parameters.AddWithValue("@t", _id);
                    using var r = cmd.ExecuteReader();
                    while (r.Read())
                        _lines.Add(new RecurringLineRow
                        {
                            ItemId = r.GetInt32(0),
                            Quantity = Convert.ToDecimal(r.GetValue(1)),
                            Price = Convert.ToDecimal(r.GetValue(2))
                        });
                }
                // أسماء الأصناف
                foreach (var l in _lines)
                {
                    using var cmd = new NpgsqlCommand("SELECT COALESCE(name,''), COALESCE(barcode,'') FROM items WHERE id=@i", conn);
                    cmd.Parameters.AddWithValue("@i", l.ItemId);
                    using var r = cmd.ExecuteReader();
                    if (r.Read()) { l.ItemName = r.GetString(0); l.Barcode = r.GetString(1); }
                }
                _grid.ItemsSource = _lines;
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ: " + ex.Message; }
        }

        private static void Pick(ComboBox c, int id)
        { for (int i = 0; i < c.Items.Count; i++) if (((ComboItem)c.Items[i]).Id == id) { c.SelectedIndex = i; return; } }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtName.Text)) { _lblStatus.Text = "اسم القالب مطلوب."; return; }
            if (_cmbParty.SelectedItem is not ComboItem party) { _lblStatus.Text = "اختر العميل (إلزامي)."; return; }
            if (_lines.Count == 0) { _lblStatus.Text = "أضف صنفًا واحدًا على الأقل."; return; }
            decimal.TryParse(_txtDiscount.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var discount);
            decimal.TryParse(_txtTax.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var tax);
            if (discount < 0 || discount > 100) { _lblStatus.Text = "نسبة الخصم بين 0 و100."; return; }
            var type = _cmbType.SelectedIndex == 1 ? "purchase" : "sales";
            var freq = _cmbFreq.SelectedIndex switch { 1 => "weekly", 2 => "quarterly", 3 => "yearly", _ => "monthly" };
            var start = _dpStart.SelectedDate ?? DateTime.Today;
            var next = _dpNext.SelectedDate ?? start.AddMonths(1);

            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var tx = conn.BeginTransaction();
                int templateId = _id;
                if (_id > 0)
                {
                    using var upd = new NpgsqlCommand(@"UPDATE recurring_invoice_templates SET name=@n, party_id=@p, invoice_type=@t,
                        frequency=@f, start_date=@sd, next_run_date=@nd, discount_percentage=@d, tax_rate=@tx,
                        is_active=@a, notes=@no WHERE id=@id", conn, tx);
                    upd.Parameters.AddWithValue("@n", _txtName.Text.Trim());
                    upd.Parameters.AddWithValue("@p", party.Id);
                    upd.Parameters.AddWithValue("@t", type);
                    upd.Parameters.AddWithValue("@f", freq);
                    upd.Parameters.AddWithValue("@sd", start);
                    upd.Parameters.AddWithValue("@nd", next);
                    upd.Parameters.AddWithValue("@d", discount);
                    upd.Parameters.AddWithValue("@tx", tax);
                    upd.Parameters.AddWithValue("@a", (bool)(_chkActive.IsChecked == true));
                    upd.Parameters.AddWithValue("@no", string.IsNullOrWhiteSpace(_txtNotes.Text) ? DBNull.Value : (object)_txtNotes.Text);
                    upd.Parameters.AddWithValue("@id", _id);
                    upd.ExecuteNonQuery();

                    using var del = new NpgsqlCommand("DELETE FROM recurring_invoice_lines WHERE template_id=@t", conn, tx);
                    del.Parameters.AddWithValue("@t", _id);
                    del.ExecuteNonQuery();
                }
                else
                {
                    using var ins = new NpgsqlCommand(@"INSERT INTO recurring_invoice_templates
                        (name, party_id, invoice_type, frequency, start_date, next_run_date, discount_percentage, tax_rate,
                         is_active, notes, runs_count, created_by, created_at)
                        VALUES (@n, @p, @t, @f, @sd, @nd, @d, @tx, @a, @no, 0, 1, NOW()) RETURNING id", conn, tx);
                    ins.Parameters.AddWithValue("@n", _txtName.Text.Trim());
                    ins.Parameters.AddWithValue("@p", party.Id);
                    ins.Parameters.AddWithValue("@t", type);
                    ins.Parameters.AddWithValue("@f", freq);
                    ins.Parameters.AddWithValue("@sd", start);
                    ins.Parameters.AddWithValue("@nd", next);
                    ins.Parameters.AddWithValue("@d", discount);
                    ins.Parameters.AddWithValue("@tx", tax);
                    ins.Parameters.AddWithValue("@a", (bool)(_chkActive.IsChecked == true));
                    ins.Parameters.AddWithValue("@no", string.IsNullOrWhiteSpace(_txtNotes.Text) ? DBNull.Value : (object)_txtNotes.Text);
                    templateId = Convert.ToInt32(ins.ExecuteScalar());
                }

                foreach (var l in _lines)
                {
                    using var insL = new NpgsqlCommand("INSERT INTO recurring_invoice_lines (template_id, item_id, quantity, price) VALUES (@t, @i, @q, @p)", conn, tx);
                    insL.Parameters.AddWithValue("@t", templateId);
                    insL.Parameters.AddWithValue("@i", l.ItemId);
                    insL.Parameters.AddWithValue("@q", l.Quantity);
                    insL.Parameters.AddWithValue("@p", l.Price);
                    insL.ExecuteNonQuery();
                }
                tx.Commit();
                AuditService.Log(_id > 0 ? "UPDATE" : "CREATE", "RecurringInvoiceTemplate", templateId,
                    "Template '" + _txtName.Text.Trim() + "' (" + freq + ") lines=" + _lines.Count);
                DialogResult = true;
                Close();
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ الحفظ: " + ex.Message; }
        }

        public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();
    }
}
