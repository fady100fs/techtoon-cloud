using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Techtoon.UI.Services;

namespace Techtoon.UI.Views
{
    public partial class UserPermissionsWindow : Window
    {
        private int _userId;
        private Dictionary<string, bool> _permissions = new Dictionary<string, bool>();

        public UserPermissionsWindow()
        {
            InitializeComponent();
        }

        public void LoadForUser(int userId, string username)
        {
            _userId = userId;
            lblUser.Text = "المستخدم: " + username;
            _permissions = AuthService.GetUserPermissions(userId);
            BuildPermissionsUI();
        }

        private string TranslateKey(string key)
        {
            var translations = new Dictionary<string, string>
            {
                {"sales", "المبيعات"},
                {"returns", "المرتجعات"},
                {"discounts", "الخصومات"},
                {"edit_price", "تعديل الأسعار"},
                {"cancel_invoice", "إلغاء الفواتير"},
                {"expenses", "المصروفات"},
                {"profit_reports", "تقارير الأرباح"},
                {"backup", "النسخ الاحتياطي"},
                {"settings", "الإعدادات"},
                {"manage_users", "إدارة المستخدمين"},
                {"stock_adjustment", "تسوية المخزون (قديم)"},
                {"inventory.count.create", "إنشاء جلسة جرد"},
                {"inventory.count.count", "إدخال نتائج العد"},
                {"inventory.count.review", "مراجعة فروق الجرد"},
                {"inventory.count.approve", "اعتماد الجرد"},
                {"inventory.adjustment.create", "إنشاء تسوية"},
                {"inventory.adjustment.approve", "اعتماد التسوية"},
                {"inventory.adjustment.execute", "تنفيذ التسوية"},
                {"inventory.damaged.create", "تسجيل الأصناف التالفة"},
                {"inventory.damaged.inspect", "فحص التالف"},
                {"inventory.writeoff.approve", "اعتماد الشطب"},
                {"inventory.writeoff.execute", "تنفيذ الشطب"},
                {"inventory.reports.view", "تقارير المخزون"},
                {"inventory.audit.view", "سجل تدقيق المخزون"}
            };
            return translations.ContainsKey(key) ? translations[key] : key;
        }

        private void BuildPermissionsUI()
        {
            pnlPermissions.Children.Clear();
            var allKeys = AuthService.GetAllPermissionKeys();
            var grouped = new Dictionary<string, List<string>>
            {
                { "المبيعات والمشتريات", new List<string>() },
                { "المالية", new List<string>() },
                { "المستخدمين", new List<string>() },
                { "المخزون - جرد", new List<string>() },
                { "المخزون - تسويات", new List<string>() },
                { "المخزون - تالف", new List<string>() },
                { "المخزون - شطب", new List<string>() },
                { "التقارير", new List<string>() }
            };

            foreach (var key in allKeys)
            {
                if (key.StartsWith("inventory.count.")) grouped["المخزون - جرد"].Add(key);
                else if (key.StartsWith("inventory.adjustment.")) grouped["المخزون - تسويات"].Add(key);
                else if (key.StartsWith("inventory.damaged.")) grouped["المخزون - تالف"].Add(key);
                else if (key.StartsWith("inventory.writeoff.")) grouped["المخزون - شطب"].Add(key);
                else if (key.StartsWith("inventory.")) grouped["التقارير"].Add(key);
                else if (new[] { "sales", "returns", "discounts", "edit_price", "cancel_invoice" }.Contains(key)) grouped["المبيعات والمشتريات"].Add(key);
                else if (new[] { "expenses", "profit_reports", "backup", "settings" }.Contains(key)) grouped["المالية"].Add(key);
                else if (key == "manage_users") grouped["المستخدمين"].Add(key);
                else grouped["المالية"].Add(key);
            }

            foreach (var group in grouped)
            {
                if (group.Value.Count == 0) continue;
                var header = new TextBlock { Text = group.Key, FontSize = 14, FontWeight = FontWeights.Bold, Foreground = System.Windows.Media.Brushes.DarkSlateGray, Margin = new Thickness(0, 10, 0, 5) };
                pnlPermissions.Children.Add(header);

                foreach (var key in group.Value)
                {
                    var cb = new CheckBox
                    {
                        Content = TranslateKey(key),
                        IsChecked = _permissions.ContainsKey(key) && _permissions[key],
                        Margin = new Thickness(20, 3, 0, 3),
                        Tag = key
                    };
                    pnlPermissions.Children.Add(cb);
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var newPerms = new Dictionary<string, bool>();
            foreach (var child in pnlPermissions.Children)
            {
                if (child is CheckBox cb && cb.Tag is string key)
                {
                    newPerms[key] = cb.IsChecked ?? false;
                }
            }
            try
            {
                AuthService.SetUserPermissions(_userId, newPerms);
                MessageBox.Show("تم حفظ الصلاحيات");
                DialogResult = true;
                Close();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
    }
}