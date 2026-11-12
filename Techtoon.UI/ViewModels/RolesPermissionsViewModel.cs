using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Techtoon.Core.Permissions;
using Techtoon.UI.Services;
using Techtoon.UI.Views;

namespace Techtoon.UI.ViewModels
{
    /// <summary>صلاحية واحدة داخل مصفوفة الأدوار.</summary>
    public class PermissionItemViewModel : ViewModelBase
    {
        private bool _isChecked;
        public string Key { get; set; } = "";
        public string Label { get; set; } = "";
        public string Module { get; set; } = "";
        public bool IsChecked { get => _isChecked; set => SetProperty(ref _isChecked, value); }
    }

    /// <summary>مجموعة صلاحيات (حسب الوحدة) داخل المصفوفة.</summary>
    public class PermissionGroupViewModel
    {
        public string ModuleLabel { get; set; } = "";
        public string Module { get; set; } = "";
        public ObservableCollection<PermissionItemViewModel> Items { get; set; } = new();
    }

    /// <summary>حالة استثناء صلاحية لمستخدم: 0=تورث، 1=منح، 2=منع.</summary>
    public class OverrideStateOption
    {
        public int Value { get; set; }
        public string DisplayName { get; set; } = "";
        public override string ToString() => DisplayName;
    }

    /// <summary>استثناء صلاحية لمستخدم.</summary>
    public class UserOverrideItemViewModel : ViewModelBase
    {
        private int _state;
        public string Key { get; set; } = "";
        public string Label { get; set; } = "";
        public string Module { get; set; } = "";
        public int State { get => _state; set => SetProperty(ref _state, value); }
    }

    public class UserOverrideGroupViewModel
    {
        public string ModuleLabel { get; set; } = "";
        public string Module { get; set; } = "";
        public ObservableCollection<UserOverrideItemViewModel> Items { get; set; } = new();
    }

    /// <summary>
    /// شاشة «الأدوار والصلاحيات»:
    /// 1) مصفوفة صلاحيات لكل دور (من_clusters الأدوار).
    /// 2) توزيع الأدوار على المستخدمين.
    /// 3) استثناءات لكل مستخدم فوق دوره (منح/منع).
    /// </summary>
    public class RolesPermissionsViewModel : ViewModelBase
    {
        private RoleRecord? _selectedRole;
        private AuthService.UserWithRole? _selectedUser;
        private AuthService.RoleOption? _selectedUserRole;
        private string _statusMessage = "";
        private int _selectedTab;

        public ObservableCollection<RoleRecord> Roles { get; } = new();
        public ObservableCollection<PermissionGroupViewModel> PermissionGroups { get; } = new();
        public ObservableCollection<AuthService.UserWithRole> Users { get; } = new();
        public ObservableCollection<AuthService.RoleOption> AssignableRoles { get; } = new();
        public ObservableCollection<UserOverrideGroupViewModel> UserOverrideGroups { get; } = new();
        public ObservableCollection<OverrideStateOption> OverrideStates { get; } = new();

        public RoleRecord? SelectedRole
        {
            get => _selectedRole;
            set { if (SetProperty(ref _selectedRole, value)) LoadRolePermissions(); }
        }

        public AuthService.UserWithRole? SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (SetProperty(ref _selectedUser, value))
                {
                    SelectedUserRole = value?.RoleId != null
                        ? AssignableRoles.FirstOrDefault(r => r.Id == value.RoleId)
                        : null;
                    LoadUserOverrides();
                }
            }
        }

        public AuthService.RoleOption? SelectedUserRole
        {
            get => _selectedUserRole;
            set
            {
                if (!SetProperty(ref _selectedUserRole, value)) return;
                if (value == null || SelectedUser == null) return;
                AssignRole(value);
            }
        }

        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public int SelectedTab { get => _selectedTab; set => SetProperty(ref _selectedTab, value); }

        public ICommand RefreshCommand { get; }
        public ICommand SaveRolePermissionsCommand { get; }
        public ICommand SelectAllCommand { get; }
        public ICommand ClearAllCommand { get; }
        public ICommand NewRoleCommand { get; }
        public ICommand EditRoleCommand { get; }
        public ICommand DeleteRoleCommand { get; }
        public ICommand SaveUserOverridesCommand { get; }

        public RolesPermissionsViewModel()
        {
            OverrideStates.Add(new OverrideStateOption { Value = 0, DisplayName = "تورث من الدور" });
            OverrideStates.Add(new OverrideStateOption { Value = 1, DisplayName = "منح (إضافي)" });
            OverrideStates.Add(new OverrideStateOption { Value = 2, DisplayName = "منع (سحب)" });

            RefreshCommand = new UiRelayCommand(_ => Refresh());
            SaveRolePermissionsCommand = new UiRelayCommand(_ => SaveRolePermissions());
            SelectAllCommand = new UiRelayCommand(_ => SetAllChecked(true));
            ClearAllCommand = new UiRelayCommand(_ => SetAllChecked(false));
            NewRoleCommand = new UiRelayCommand(_ => NewRole());
            EditRoleCommand = new UiRelayCommand(_ => EditRole(), _ => SelectedRole != null);
            DeleteRoleCommand = new UiRelayCommand(_ => DeleteRole(), _ => SelectedRole != null && !SelectedRole.IsSystemRole);
            SaveUserOverridesCommand = new UiRelayCommand(_ => SaveUserOverrides());

            Refresh();
        }

        public void Refresh()
        {
            try
            {
                var keepRole = SelectedRole?.Id;
                var keepUser = SelectedUser?.Id;

                Roles.Clear();
                foreach (var r in RoleService.GetRoles()) Roles.Add(r);

                AssignableRoles.Clear();
                foreach (var r in Roles.Where(r => r.IsActive)) AssignableRoles.Add(new AuthService.RoleOption
                {
                    Id = r.Id,
                    Name = r.Name,
                    DisplayName = r.DisplayName,
                    Description = r.Description,
                    IsSystemRole = r.IsSystemRole
                });

                Users.Clear();
                foreach (var u in AuthService.GetAllUsers()) Users.Add(u);

                SelectedRole = keepRole.HasValue ? Roles.FirstOrDefault(r => r.Id == keepRole.Value) : Roles.FirstOrDefault();
                SelectedUser = keepUser.HasValue ? Users.FirstOrDefault(u => u.Id == keepUser.Value) : Users.FirstOrDefault();
                StatusMessage = "تم التحديث — " + Roles.Count + " دور، " + Users.Count + " مستخدم.";
            }
            catch (Exception ex)
            {
                StatusMessage = "خطأ: " + ex.Message;
            }
        }

        // ---------- مصفوفة صلاحيات الدور ----------

        private void LoadRolePermissions()
        {
            PermissionGroups.Clear();
            var granted = SelectedRole != null ? RoleService.GetRolePermissions(SelectedRole.Id) : new HashSet<string>();

            foreach (var group in PermissionCatalog.ByModule)
            {
                var g = new PermissionGroupViewModel
                {
                    Module = group.Key,
                    ModuleLabel = PermissionCatalog.ModuleLabelOf(group.Key)
                };
                foreach (var p in group)
                {
                    g.Items.Add(new PermissionItemViewModel
                    {
                        Key = p.Key,
                        Label = p.Label,
                        Module = p.Module,
                        IsChecked = granted.Contains(p.Key)
                    });
                }
                PermissionGroups.Add(g);
            }
        }

        private void SetAllChecked(bool value)
        {
            foreach (var g in PermissionGroups)
                foreach (var item in g.Items)
                    item.IsChecked = value;
        }

        private void SaveRolePermissions()
        {
            if (SelectedRole == null) { StatusMessage = "اختر دورًا أولًا."; return; }
            try
            {
                var keys = PermissionGroups.SelectMany(g => g.Items)
                                            .Where(i => i.IsChecked)
                                            .Select(i => i.Key)
                                            .ToList();
                RoleService.SetRolePermissions(SelectedRole.Id, keys);
                AuthService.LogAudit("SET_ROLE_PERMISSIONS", "role", SelectedRole.Id,
                    "Role " + SelectedRole.Name + " → " + keys.Count + " permissions");
                StatusMessage = "تم حفظ صلاحيات «" + SelectedRole.DisplayName + "» — " + keys.Count + " صلاحية.";
                Refresh();
            }
            catch (Exception ex)
            {
                StatusMessage = "فشل الحفظ: " + ex.Message;
            }
        }

        // ---------- إدارة الأدوار ----------

        private void NewRole()
        {
            var name = InputDialog.Show(Application.Current?.MainWindow,
                "اسم الدور (إنجليزي، بدون مسافات):", "دور جديد", "");
            if (string.IsNullOrWhiteSpace(name)) return;
            var display = InputDialog.Show(Application.Current?.MainWindow,
                "الاسم المعروض (عربي):", "دور جديد", name.Trim().ToUpperInvariant());
            try
            {
                int id = RoleService.CreateRole(name.Trim(), display ?? name.Trim(), "");
                StatusMessage = "تم إنشاء الدور.";
                Refresh();
                SelectedRole = Roles.FirstOrDefault(r => r.Id == id);
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void EditRole()
        {
            if (SelectedRole == null) return;
            var display = InputDialog.Show(Application.Current?.MainWindow,
                "الاسم المعروض (عربي):", "تعديل الدور", SelectedRole.DisplayName);
            if (display == null) return;
            try
            {
                RoleService.UpdateRole(SelectedRole.Id, display.Trim(), SelectedRole.Description, SelectedRole.IsActive);
                StatusMessage = "تم تحديث الدور.";
                Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteRole()
        {
            if (SelectedRole == null) return;
            if (MessageBox.Show($"حذف الدور «{SelectedRole.DisplayName}»؟\nالمستخدمون المرتبطون به سينتقلون إلى «مطّلع (عرض فقط)».",
                    "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            try
            {
                RoleService.DeleteRole(SelectedRole.Id);
                StatusMessage = "تم حذف الدور.";
                SelectedRole = null;
                Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ---------- المستخدمون ----------

        private void AssignRole(AuthService.RoleOption role)
        {
            if (SelectedUser == null) return;
            try
            {
                RoleService.SetUserRole(SelectedUser.Id, role.Id);
                AuthService.LogAudit("SET_USER_ROLE", "user", SelectedUser.Id, "Role → " + role.Name);
                SelectedUser.Role = role.Name;
                SelectedUser.RoleId = role.Id;
                SelectedUser.RoleDisplayName = role.DisplayName;
                StatusMessage = $"تم إعطاء «{SelectedUser.Username}» دور «{role.DisplayName}».";
                LoadUserOverrides();
            }
            catch (Exception ex)
            {
                StatusMessage = "فشل تعيين الدور: " + ex.Message;
            }
        }

        private void LoadUserOverrides()
        {
            UserOverrideGroups.Clear();
            if (SelectedUser == null) return;
            var overrides = RoleService.GetUserOverrides(SelectedUser.Id);

            foreach (var group in PermissionCatalog.ByModule)
            {
                var g = new UserOverrideGroupViewModel
                {
                    Module = group.Key,
                    ModuleLabel = PermissionCatalog.ModuleLabelOf(group.Key)
                };
                foreach (var p in group)
                {
                    int state = 0;
                    if (overrides.TryGetValue(p.Key, out var granted))
                        state = granted ? 1 : 2;
                    g.Items.Add(new UserOverrideItemViewModel
                    {
                        Key = p.Key,
                        Label = p.Label,
                        Module = p.Module,
                        State = state
                    });
                }
                UserOverrideGroups.Add(g);
            }
        }

        private void SaveUserOverrides()
        {
            if (SelectedUser == null) { StatusMessage = "اختر مستخدمًا أولًا."; return; }
            try
            {
                var map = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                foreach (var g in UserOverrideGroups)
                    foreach (var item in g.Items)
                        if (item.State == 1) map[item.Key] = true;
                        else if (item.State == 2) map[item.Key] = false;

                RoleService.SetUserOverrides(SelectedUser.Id, map);
                AuthService.LogAudit("SET_USER_OVERRIDES", "user", SelectedUser.Id,
                    "Overrides: " + map.Count);
                StatusMessage = "تم حفظ استثناءات «" + SelectedUser.Username + "» — " + map.Count + " استثناء.";

                // إذا كان المستخدم الحالي متأثرًا → أعد تحميل صلاحياته
                if (AuthService.CurrentUser?.Id == SelectedUser.Id)
                    AuthService.ReloadPermissions();
            }
            catch (Exception ex)
            {
                StatusMessage = "فشل الحفظ: " + ex.Message;
            }
        }
    }
}
