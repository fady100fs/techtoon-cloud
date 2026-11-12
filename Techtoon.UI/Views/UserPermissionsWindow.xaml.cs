using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Techtoon.Core.Permissions;
using Techtoon.UI.Services;

namespace Techtoon.UI.Views
{
    /// <summary>
    /// نافذة استثناءات صلاحيات مستخدم واحد (فوق دوره).
    /// تُستخدم من شاشة المستخدمين — أما مصفوفة صلاحيات الأدوار فتوجد في
    /// صفحة «الأدوار والصلاحيات».
    /// </summary>
    public partial class UserPermissionsWindow : Window
    {
        private int _userId;
        private string _username = "";
        private Dictionary<string, bool> _permissions = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        public UserPermissionsWindow()
        {
            InitializeComponent();
        }

        public void LoadForUser(int userId, string username)
        {
            _userId = userId;
            _username = username;
            lblUser.Text = "المستخدم: " + username;
            _permissions = AuthService.GetUserPermissions(userId);
            BuildPermissionsUI();
        }

        private void BuildPermissionsUI()
        {
            pnlPermissions.Children.Clear();

            // الصلاحيات الفعلية من الدور (الأساس الذي نتجاوزه)
            var rolePerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var users = AuthService.GetAllUsers();
                var u = users.Find(x => x.Id == _userId);
                if (u?.RoleId != null) rolePerms = RoleService.GetRolePermissions(u.RoleId.Value);
                else if (!string.IsNullOrEmpty(u?.Role)) { var r = RoleService.FindByName(u.Role); if (r != null) rolePerms = RoleService.GetRolePermissions(r.Id); }
            }
            catch { /* نكمل بالاستثناءات فقط */ }

            foreach (var group in PermissionCatalog.ByModule)
            {
                var header = new TextBlock
                {
                    Text = PermissionCatalog.ModuleLabelOf(group.Key),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = System.Windows.Media.Brushes.DarkSlateGray,
                    Margin = new Thickness(0, 10, 0, 5)
                };
                pnlPermissions.Children.Add(header);

                foreach (var p in group)
                {
                    bool? state;
                    if (_permissions.TryGetValue(p.Key, out var granted))
                        state = granted;                       // استثناء صريح: منح أو منع
                    else
                        state = null;                          // يرث من الدور

                    var cb = new CheckBox
                    {
                        Content = p.Label,
                        IsThreeState = true,
                        IsChecked = state,
                        Margin = new Thickness(20, 3, 0, 3),
                        Tag = p.Key
                    };
                    System.Windows.Controls.ToolTipService.SetToolTip(cb,
                        (state == null
                            ? "موروث من الدور (" + (rolePerms.Contains(p.Key) ? "ممنوح" : "غير ممنوح") + ") — انقر لتغييره"
                            : (state == true ? "استثناء: ممنوح فوق الدور" : "استثناء: ممنوع رغم الدور")));
                    pnlPermissions.Children.Add(cb);
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var newPerms = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var child in pnlPermissions.Children)
            {
                if (child is CheckBox cb && cb.Tag is string key)
                {
                    // null = يرث من الدور (لا استثناء) | true = منح | false = منع
                    if (cb.IsChecked.HasValue) newPerms[key] = cb.IsChecked.Value;
                }
            }
            try
            {
                AuthService.SetUserPermissions(_userId, newPerms);
                if (AuthService.CurrentUser?.Id == _userId) AuthService.ReloadPermissions();
                MessageBox.Show("تم حفظ استثناءات الصلاحيات (" + newPerms.Count + " استثناء)");
                DialogResult = true;
                Close();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
    }
}
