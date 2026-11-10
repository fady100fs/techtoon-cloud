using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Npgsql;
using Techtoon.UI.Services;

namespace Techtoon.UI.Views
{
    // ============================================================
    // B3 — نافذة إضافة/تعديل أصل ثابت (fixed_assets)
    // ============================================================
    public class FixedAssetEditWindow : Window
    {
        private readonly int _assetId;

        private readonly TextBox _txtName = new() { Margin = new Thickness(4) };
        private readonly TextBox _txtCode = new() { Margin = new Thickness(4) };
        private readonly ComboBox _cmbCategory = new() { IsEditable = true, Margin = new Thickness(4), MinWidth = 220 };
        private readonly DatePicker _dpPurchase = new() { Margin = new Thickness(4), SelectedDate = DateTime.Today };
        private readonly TextBox _txtCost = new() { Margin = new Thickness(4) };
        private readonly TextBox _txtSalvage = new() { Margin = new Thickness(4) };
        private readonly TextBox _txtLife = new() { Margin = new Thickness(4) };
        private readonly TextBox _txtLocation = new() { Margin = new Thickness(4) };
        private readonly TextBox _txtResponsible = new() { Margin = new Thickness(4) };
        private readonly TextBox _txtNotes = new() { Margin = new Thickness(4), Height = 55, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };
        private readonly TextBlock _lblInfo = new() { Foreground = Brushes.DarkSlateGray, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4) };
        private readonly TextBlock _lblStatus = new() { Foreground = Brushes.Red, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4) };

        private decimal _accumulated;
        private string _existingStatus = "active";

        private static readonly string[] DefaultCategories =
        {
            "مركبات", "أثاث ومعدات مكتبية", "أجهزة كمبيوتر", "آلات ومعدات إنتاج",
            "مبانٍ", "أراضٍ", "أدوات ومعدات", "أثاث"
        };

        public FixedAssetEditWindow(int assetId)
        {
            _assetId = assetId;
            Title = assetId > 0 ? "تعديل أصل ثابت" : "إضافة أصل ثابت";
            Width = 560; Height = 660;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FlowDirection = FlowDirection.RightToLeft;
            FontFamily = new FontFamily("Tahoma");
            Background = new SolidColorBrush(Color.FromRgb(245, 247, 250));

            foreach (var c in DefaultCategories) _cmbCategory.Items.Add(c);

            var grid = new Grid { Margin = new Thickness(12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int i = 0; i < 13; i++) grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            AddRow(grid, 0, "اسم الأصل:", _txtName);
            AddRow(grid, 1, "كود الأصل:", _txtCode);
            AddRow(grid, 2, "التصنيف:", _cmbCategory);
            AddRow(grid, 3, "تاريخ الشراء:", _dpPurchase);
            AddRow(grid, 4, "تكلفة الشراء:", _txtCost);
            AddRow(grid, 5, "القيمة المتبقية:", _txtSalvage);
            AddRow(grid, 6, "العمر الإنتاجي (سنوات):", _txtLife);
            AddRow(grid, 7, "الموقع:", _txtLocation);
            AddRow(grid, 8, "المسؤول:", _txtResponsible);
            AddRow(grid, 9, "ملاحظات:", _txtNotes);
            Grid.SetRow(_lblInfo, 10); Grid.SetColumn(_lblInfo, 1); grid.Children.Add(_lblInfo);
            Grid.SetRow(_lblStatus, 11); Grid.SetColumn(_lblStatus, 1); grid.Children.Add(_lblStatus);

            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
            var save = new Button { Content = "حفظ", Padding = new Thickness(24, 6, 24, 6), Margin = new Thickness(4), Background = new SolidColorBrush(Color.FromRgb(0, 137, 123)), Foreground = Brushes.White, FontWeight = FontWeights.Bold };
            var cancel = new Button { Content = "إلغاء", Padding = new Thickness(24, 6, 24, 6), Margin = new Thickness(4) };
            save.Click += Save_Click;
            cancel.Click += (s, e) => Close();
            btns.Children.Add(save); btns.Children.Add(cancel);
            Grid.SetRow(btns, 12); Grid.SetColumn(btns, 1); grid.Children.Add(btns);

            Content = grid;

            if (_assetId <= 0)
            {
                _txtSalvage.Text = "0";
                _txtLife.Text = "5";
            }
            else LoadExisting();

            _txtCost.TextChanged += (s, e) => UpdateInfo();
            _txtSalvage.TextChanged += (s, e) => UpdateInfo();
            _txtLife.TextChanged += (s, e) => UpdateInfo();

            UpdateInfo();
        }

        private static void AddRow(Grid g, int row, string label, UIElement control)
        {
            var lbl = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 6, 0) };
            Grid.SetRow(lbl, row); Grid.SetColumn(lbl, 0); g.Children.Add(lbl);
            Grid.SetRow(control, row); Grid.SetColumn(control, 1); g.Children.Add(control);
        }

        public static string GetCS() => SecureConnectionManager.GetConnectionString();

        private static bool TryParseAmount(string? text, out decimal amount)
        {
            amount = 0m;
            string t = (text ?? "").Trim();
            if (t.Length == 0) return false;
            if (decimal.TryParse(t, NumberStyles.Any, CultureInfo.CurrentCulture, out amount)) return true;
            return decimal.TryParse(t, NumberStyles.Any, CultureInfo.InvariantCulture, out amount);
        }

        private void LoadExisting()
        {
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var cmd = new NpgsqlCommand(@"SELECT COALESCE(name,''), COALESCE(code,''), COALESCE(category,''),
                                                        purchase_date, purchase_cost, COALESCE(salvage_value,0),
                                                        useful_life_years, COALESCE(accumulated_depreciation,0),
                                                        COALESCE(location,''), COALESCE(responsible_person,''),
                                                        COALESCE(status,'active'), COALESCE(notes,'')
                                                FROM fixed_assets WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("id", _assetId);
                using var r = cmd.ExecuteReader();
                if (!r.Read()) { _lblStatus.Text = "الأصل غير موجود."; return; }
                _txtName.Text = BudgetAssetsDb.ToText(r.GetValue(0));
                _txtCode.Text = BudgetAssetsDb.ToText(r.GetValue(1));
                _cmbCategory.Text = BudgetAssetsDb.ToText(r.GetValue(2));
                _dpPurchase.SelectedDate = Techtoon.Core.Data.DbValue.AsDateTime(r.GetValue(3));
                _txtCost.Text = BudgetAssetsDb.ToDecimal(r.GetValue(4)).ToString("0.##");
                _txtSalvage.Text = BudgetAssetsDb.ToDecimal(r.GetValue(5)).ToString("0.##");
                _txtLife.Text = BudgetAssetsDb.ToInt(r.GetValue(6)).ToString();
                _accumulated = BudgetAssetsDb.ToDecimal(r.GetValue(7));
                _txtLocation.Text = BudgetAssetsDb.ToText(r.GetValue(8));
                _txtResponsible.Text = BudgetAssetsDb.ToText(r.GetValue(9));
                _existingStatus = BudgetAssetsDb.ToText(r.GetValue(10));
                _txtNotes.Text = BudgetAssetsDb.ToText(r.GetValue(11));
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ: " + ex.Message; }
        }

        private void UpdateInfo()
        {
            TryParseAmount(_txtCost.Text, out decimal cost);
            TryParseAmount(_txtSalvage.Text, out decimal salvage);
            int life = 0;
            int.TryParse((_txtLife.Text ?? "").Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture, out life);
            decimal baseAmount = Math.Max(0m, cost - salvage);
            decimal net = cost - _accumulated;
            decimal monthly = life > 0 ? Math.Round(baseAmount / life / 12m, 2, MidpointRounding.AwayFromZero) : 0m;
            _lblInfo.Text = "صافي القيمة الدفترية = تكلفة الشراء − الإهلاك المتراكم = " + net.ToString("N2")
                + "   |   الإهلاك الشهري = " + monthly.ToString("N2");
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string name = (_txtName.Text ?? "").Trim();
            string code = (_txtCode.Text ?? "").Trim();
            string category = (_cmbCategory.Text ?? "").Trim();

            if (string.IsNullOrWhiteSpace(name)) { _lblStatus.Text = "اسم الأصل مطلوب."; return; }
            if (string.IsNullOrWhiteSpace(code)) { _lblStatus.Text = "كود الأصل مطلوب."; return; }
            if (string.IsNullOrWhiteSpace(category)) { _lblStatus.Text = "تصنيف الأصل مطلوب."; return; }
            if (_dpPurchase.SelectedDate == null) { _lblStatus.Text = "تاريخ الشراء مطلوب."; return; }
            if (!TryParseAmount(_txtCost.Text, out decimal cost) || cost <= 0m) { _lblStatus.Text = "تكلفة الشراء مطلوبة ويجب أن تكون أكبر من صفر."; return; }
            if (!TryParseAmount(_txtSalvage.Text, out decimal salvage) || salvage < 0m) { _lblStatus.Text = "القيمة المتبقية يجب أن تكون صفرًا أو أكثر."; return; }
            if (salvage >= cost) { _lblStatus.Text = "القيمة المتبقية يجب أن تكون أقل من تكلفة الشراء."; return; }
            if (!int.TryParse((_txtLife.Text ?? "").Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture, out int life) || life <= 0)
            { _lblStatus.Text = "العمر الإنتاجي مطلوب ويجب أن يكون سنة واحدة على الأقل."; return; }
            if (cost - _accumulated < 0m) { _lblStatus.Text = "تكلفة الشراء أقل من الإهلاك المتراكم — لا يمكن التعديل."; return; }

            NpgsqlConnection? conn = null;
            NpgsqlTransaction? tx = null;
            try
            {
                conn = new NpgsqlConnection(GetCS());
                conn.Open();
                tx = conn.BeginTransaction();

                // منع تكرار الكود
                string dupSql = "SELECT id FROM fixed_assets WHERE code=@code";
                if (_assetId > 0) dupSql += " AND id<>@id";
                dupSql += " LIMIT 1";
                using (var dup = new NpgsqlCommand(dupSql, conn, tx))
                {
                    dup.Parameters.AddWithValue("code", code);
                    if (_assetId > 0) dup.Parameters.AddWithValue("id", _assetId);
                    var exists = dup.ExecuteScalar();
                    if (exists != null && exists != DBNull.Value)
                    {
                        _lblStatus.Text = "يوجد أصل ثابت آخر بنفس الكود (" + code + ") — اختر كودًا مختلفًا.";
                        return;
                    }
                }

                decimal netBook = Math.Round(cost - _accumulated, 2, MidpointRounding.AwayFromZero);
                string status = _existingStatus;
                if (_assetId <= 0) status = "active";
                if (_accumulated >= Math.Max(0m, cost - salvage) - 0.005m) status = "fully_depreciated";

                var values = new List<KeyValuePair<string, object?>>
                {
                    new KeyValuePair<string, object?>("name", name),
                    new KeyValuePair<string, object?>("code", code),
                    new KeyValuePair<string, object?>("category", category),
                    new KeyValuePair<string, object?>("purchase_date", _dpPurchase.SelectedDate.Value),
                    new KeyValuePair<string, object?>("purchase_cost", cost),
                    new KeyValuePair<string, object?>("salvage_value", salvage),
                    new KeyValuePair<string, object?>("useful_life_years", life),
                    new KeyValuePair<string, object?>("net_book_value", netBook),
                    new KeyValuePair<string, object?>("location", string.IsNullOrWhiteSpace(_txtLocation.Text) ? (object?)DBNull.Value : _txtLocation.Text.Trim()),
                    new KeyValuePair<string, object?>("responsible_person", string.IsNullOrWhiteSpace(_txtResponsible.Text) ? (object?)DBNull.Value : _txtResponsible.Text.Trim()),
                    new KeyValuePair<string, object?>("status", status),
                    new KeyValuePair<string, object?>("notes", string.IsNullOrWhiteSpace(_txtNotes.Text) ? (object?)DBNull.Value : _txtNotes.Text.Trim())
                };
                var raw = new Dictionary<string, string>();

                if (_assetId <= 0)
                {
                    values.Add(new KeyValuePair<string, object?>("accumulated_depreciation", 0m));
                    values.Add(new KeyValuePair<string, object?>("created_by", BudgetAssetsDb.CurrentUserId()));
                    values.Add(new KeyValuePair<string, object?>("created_at", null));
                    raw["created_at"] = "NOW()";

                    var columns = BudgetAssetsDb.GetColumns(conn, "fixed_assets");
                    if (columns.Contains("depreciation_method"))
                        values.Add(new KeyValuePair<string, object?>("depreciation_method",
                            BudgetAssetsDb.ResolveEnumLike(conn, "fixed_assets", "depreciation_method", "STRAIGHT_LINE")));

                    string insSql = BudgetAssetsDb.BuildInsertSql(conn, "fixed_assets", values, raw, out var insParams);
                    using (var ins = new NpgsqlCommand(insSql + " RETURNING id", conn, tx))
                    {
                        foreach (var p in insParams) ins.Parameters.AddWithValue("@" + p.Key, p.Value ?? DBNull.Value);
                        int newId = Convert.ToInt32(ins.ExecuteScalar(), CultureInfo.InvariantCulture);
                        tx.Commit();
                        tx = null;
                        AuditService.Log("CREATE", "FixedAsset", newId,
                            "Fixed asset created: " + code + " - " + name + ", cost=" + cost.ToString("N2") +
                            ", salvage=" + salvage.ToString("N2") + ", life=" + life + " years");
                        DialogResult = true;
                        Close();
                        return;
                    }
                }
                else
                {
                    string updSql = BudgetAssetsDb.BuildUpdateSql(conn, "fixed_assets", values, raw, "id", _assetId, out var updParams);
                    using (var upd = new NpgsqlCommand(updSql, conn, tx))
                    {
                        foreach (var p in updParams) upd.Parameters.AddWithValue("@" + p.Key, p.Value ?? DBNull.Value);
                        upd.Parameters.AddWithValue("__where", _assetId);
                        upd.ExecuteNonQuery();
                    }
                    tx.Commit();
                    tx = null;
                    AuditService.Log("UPDATE", "FixedAsset", _assetId,
                        "Fixed asset updated: " + code + " - " + name + ", cost=" + cost.ToString("N2") +
                        ", salvage=" + salvage.ToString("N2") + ", life=" + life + " years");
                    DialogResult = true;
                    Close();
                    return;
                }
            }
            catch (Npgsql.PostgresException px) when (px.SqlState == "23505")
            {
                _lblStatus.Text = "يوجد أصل ثابت آخر بنفس الكود — اختر كودًا مختلفًا.";
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "خطأ: " + ex.Message;
            }
            finally
            {
                try { if (tx != null) { tx.Rollback(); tx.Dispose(); } } catch { }
                try { if (conn != null) conn.Dispose(); } catch { }
            }
        }
    }

    // ============================================================
    // B3 — تسجيل الإهلاك الشهري (depreciation_records + fixed_assets) في معاملة واحدة
    // ============================================================
    public class DepreciationWindow : Window
    {
        private readonly int _assetId;
        private readonly string _assetCode;
        private readonly string _assetName;
        private readonly TextBox _txtNotes = new() { Margin = new Thickness(4), Height = 40, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };
        private readonly TextBlock _lblInfo = new() { Margin = new Thickness(4), TextWrapping = TextWrapping.Wrap };
        private readonly TextBlock _lblStatus = new() { Foreground = Brushes.Red, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4) };

        private decimal _cost, _salvage, _accumulated;
        private int _life;

        public DepreciationWindow(int assetId, string assetCode, string assetName)
        {
            _assetId = assetId; _assetCode = assetCode; _assetName = assetName;
            Title = "تسجيل إهلاك - " + assetCode;
            Width = 480; Height = 440;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FlowDirection = FlowDirection.RightToLeft;
            FontFamily = new FontFamily("Tahoma");
            Background = new SolidColorBrush(Color.FromRgb(245, 247, 250));

            _txtNotes.Text = "إهلاك شهري بطريقة القسط الثابت";

            var grid = new Grid { Margin = new Thickness(12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int i = 0; i < 5; i++) grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            Grid.SetRow(_lblInfo, 0); Grid.SetColumn(_lblInfo, 1); grid.Children.Add(_lblInfo);

            var lblNotes = new TextBlock { Text = "ملاحظات:", VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 6, 0) };
            Grid.SetRow(lblNotes, 1); Grid.SetColumn(lblNotes, 0); grid.Children.Add(lblNotes);
            Grid.SetRow(_txtNotes, 1); Grid.SetColumn(_txtNotes, 1); grid.Children.Add(_txtNotes);

            Grid.SetRow(_lblStatus, 2); Grid.SetColumn(_lblStatus, 1); grid.Children.Add(_lblStatus);

            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
            var save = new Button { Content = "تأكيد الإهلاك", Padding = new Thickness(24, 6, 24, 6), Margin = new Thickness(4), Background = new SolidColorBrush(Color.FromRgb(21, 101, 192)), Foreground = Brushes.White, FontWeight = FontWeights.Bold };
            var cancel = new Button { Content = "إلغاء", Padding = new Thickness(24, 6, 24, 6), Margin = new Thickness(4) };
            save.Click += Save_Click;
            cancel.Click += (s, e) => Close();
            btns.Children.Add(save); btns.Children.Add(cancel);
            Grid.SetRow(btns, 3); Grid.SetColumn(btns, 1); grid.Children.Add(btns);

            Content = grid;

            LoadPreview();
        }

        public static string GetCS() => SecureConnectionManager.GetConnectionString();

        private void LoadPreview()
        {
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var cmd = new NpgsqlCommand(@"SELECT purchase_cost, COALESCE(salvage_value,0), useful_life_years,
                                                        COALESCE(accumulated_depreciation,0), net_book_value
                                                FROM fixed_assets WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("id", _assetId);
                using var r = cmd.ExecuteReader();
                if (!r.Read()) { _lblStatus.Text = "الأصل غير موجود."; return; }
                _cost = BudgetAssetsDb.ToDecimal(r.GetValue(0));
                _salvage = BudgetAssetsDb.ToDecimal(r.GetValue(1));
                _life = BudgetAssetsDb.ToInt(r.GetValue(2));
                _accumulated = BudgetAssetsDb.ToDecimal(r.GetValue(3));
            }
            catch (Exception ex) { _lblStatus.Text = "خطأ: " + ex.Message; return; }

            decimal baseAmount = Math.Max(0m, _cost - _salvage);
            decimal monthly = _life > 0 ? Math.Round(baseAmount / _life / 12m, 2, MidpointRounding.AwayFromZero) : 0m;
            decimal amount = Math.Min(monthly, Math.Max(0m, baseAmount - _accumulated));
            amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
            decimal newAcc = Math.Round(_accumulated + amount, 2, MidpointRounding.AwayFromZero);
            decimal newNet = Math.Round(_cost - newAcc, 2, MidpointRounding.AwayFromZero);

            _lblInfo.Text =
                "الأصل: " + _assetCode + " - " + _assetName + "\n" +
                "تكلفة الشراء: " + _cost.ToString("N2") + "   |   القيمة المتبقية: " + _salvage.ToString("N2") +
                "   |   العمر الإنتاجي: " + _life + " سنة\n" +
                "الأساس القابل للإهلاك: " + baseAmount.ToString("N2") +
                "   |   الإهلاك الشهري: " + monthly.ToString("N2") + "\n" +
                "الإهلاك المتراكم الحالي: " + _accumulated.ToString("N2") +
                "   |   إهلاك هذا القسط: " + amount.ToString("N2") + "\n" +
                "الإهلاك المتراكم بعد التسجيل: " + newAcc.ToString("N2") +
                "   |   صافي القيمة الدفترية: " + newNet.ToString("N2");

            if (amount <= 0m) _lblStatus.Text = "لا يوجد إهلاك مستحق لهذا الأصل (اكتمل الإهلاك أو بيانات غير صحيحة).";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            NpgsqlConnection? conn = null;
            NpgsqlTransaction? tx = null;
            try
            {
                conn = new NpgsqlConnection(GetCS());
                conn.Open();
                tx = conn.BeginTransaction();

                decimal cost, salvage, accumulated;
                int life;
                using (var cmd = new NpgsqlCommand(@"SELECT purchase_cost, COALESCE(salvage_value,0), useful_life_years,
                                                        COALESCE(accumulated_depreciation,0)
                                                FROM fixed_assets WHERE id=@id FOR UPDATE", conn, tx))
                {
                    cmd.Parameters.AddWithValue("id", _assetId);
                    using var r = cmd.ExecuteReader();
                    if (!r.Read()) { _lblStatus.Text = "الأصل غير موجود."; return; }
                    cost = BudgetAssetsDb.ToDecimal(r.GetValue(0));
                    salvage = BudgetAssetsDb.ToDecimal(r.GetValue(1));
                    life = BudgetAssetsDb.ToInt(r.GetValue(2));
                    accumulated = BudgetAssetsDb.ToDecimal(r.GetValue(3));
                }

                decimal baseAmount = Math.Max(0m, cost - salvage);
                decimal monthly = life > 0 ? Math.Round(baseAmount / life / 12m, 2, MidpointRounding.AwayFromZero) : 0m;
                decimal remaining = baseAmount - accumulated;
                if (monthly <= 0m || remaining <= 0.004m)
                {
                    _lblStatus.Text = "لا يوجد إهلاك مستحق لهذا الأصل.";
                    return;
                }
                decimal amount = Math.Round(Math.Min(monthly, remaining), 2, MidpointRounding.AwayFromZero);
                if (amount <= 0m) { _lblStatus.Text = "لا يوجد إهلاك مستحق لهذا الأصل."; return; }

                decimal newAcc = Math.Round(accumulated + amount, 2, MidpointRounding.AwayFromZero);
                decimal newNet = Math.Round(cost - newAcc, 2, MidpointRounding.AwayFromZero);
                string status = newAcc >= baseAmount - 0.005m ? "fully_depreciated" : "active";
                string notes = string.IsNullOrWhiteSpace(_txtNotes.Text) ? "إهلاك شهري بطريقة القسط الثابت" : _txtNotes.Text.Trim();

                var values = new List<KeyValuePair<string, object?>>
                {
                    new KeyValuePair<string, object?>("asset_id", _assetId),
                    new KeyValuePair<string, object?>("date", DateTime.Now),
                    new KeyValuePair<string, object?>("depreciation_amount", amount),
                    new KeyValuePair<string, object?>("accumulated_depreciation", newAcc),
                    new KeyValuePair<string, object?>("net_book_value", newNet),
                    new KeyValuePair<string, object?>("notes", notes),
                    new KeyValuePair<string, object?>("created_by", BudgetAssetsDb.CurrentUserId()),
                    new KeyValuePair<string, object?>("created_at", null)
                };
                var raw = new Dictionary<string, string> { ["created_at"] = "NOW()" };

                string insSql = BudgetAssetsDb.BuildInsertSql(conn, "depreciation_records", values, raw, out var insParams);
                int recordId;
                using (var ins = new NpgsqlCommand(insSql + " RETURNING id", conn, tx))
                {
                    foreach (var p in insParams) ins.Parameters.AddWithValue("@" + p.Key, p.Value ?? DBNull.Value);
                    recordId = Convert.ToInt32(ins.ExecuteScalar(), CultureInfo.InvariantCulture);
                }

                using (var upd = new NpgsqlCommand(@"UPDATE fixed_assets
                        SET accumulated_depreciation=@acc, net_book_value=@nbv, status=@st
                        WHERE id=@id", conn, tx))
                {
                    upd.Parameters.AddWithValue("acc", newAcc);
                    upd.Parameters.AddWithValue("nbv", newNet);
                    upd.Parameters.AddWithValue("st", status);
                    upd.Parameters.AddWithValue("id", _assetId);
                    upd.ExecuteNonQuery();
                }

                tx.Commit();
                tx = null;

                string details = "Asset=" + _assetCode + " - " + _assetName + ", depreciation=" + amount.ToString("N2") +
                                 ", accumulated=" + newAcc.ToString("N2") + ", net_book_value=" + newNet.ToString("N2") +
                                 ", status=" + status;
                AuditService.Log("CREATE", "DepreciationRecord", recordId, details);
                AuditService.Log("UPDATE", "FixedAsset", _assetId, details);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "خطأ: " + ex.Message;
            }
            finally
            {
                try { if (tx != null) { tx.Rollback(); tx.Dispose(); } } catch { }
                try { if (conn != null) conn.Dispose(); } catch { }
            }
        }
    }
}
