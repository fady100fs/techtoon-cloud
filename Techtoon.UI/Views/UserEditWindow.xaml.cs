using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Techtoon.UI.Services;

namespace Techtoon.UI.Views
{
    public partial class UserEditWindow : Window
    {
        private int _userId = 0;
        public bool Saved { get; private set; }

        public ObservableCollection<AuthService.RoleOption> Roles { get; } = new ObservableCollection<AuthService.RoleOption>();

        public UserEditWindow()
        {
            InitializeComponent();
            DataContext = this;
            LoadRoles();
        }

        private void LoadRoles()
        {
            Roles.Clear();
            foreach (var r in AuthService.GetAllRoles()) Roles.Add(r);
            cmbRole.ItemsSource = Roles;
        }

        public void LoadForEdit(int userId)
        {
            _userId = userId;
            var users = AuthService.GetAllUsers();
            var u = users.Find(x => x.Id == userId);
            if (u == null) return;

            lblTitle.Text = "تعديل مستخدم: " + u.Username;
            txtUsername.Text = u.Username;
            txtUsername.IsEnabled = false; // لا يمكن تعديل اسم المستخدم
            txtFullName.Text = u.FullName;
            txtEmail.Text = u.Email;
            cmbRole.SelectedValue = u.RoleId ?? 0;
            lblPassword.Visibility = Visibility.Collapsed;
            txtPassword.Visibility = Visibility.Collapsed;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text)) { MessageBox.Show("اسم المستخدم مطلوب"); return; }
            if (string.IsNullOrWhiteSpace(txtFullName.Text)) { MessageBox.Show("الاسم الظاهر مطلوب"); return; }
            if (cmbRole.SelectedValue == null) { MessageBox.Show("اختر الدور"); return; }

            try
            {
                int roleId = (int)cmbRole.SelectedValue;
                if (_userId == 0)
                {
                    // إنشاء جديد
                    if (string.IsNullOrWhiteSpace(txtPassword.Text)) { MessageBox.Show("كلمة المرور مطلوبة للمستخدم الجديد"); return; }
                    string roleName = "";
                    foreach (var r in Roles) if (r.Id == roleId) { roleName = r.Name; break; }

                    bool success = AuthService.CreateUser(txtUsername.Text.Trim(), txtPassword.Text, txtFullName.Text.Trim(), roleName);
                    if (!success) { MessageBox.Show("فشل الإنشاء - اسم المستخدم可能 مكرر"); return; }
                    
                    // ربط role_id
                    var newUsers = AuthService.GetAllUsers();
                    var newU = newUsers.Find(x => x.Username == txtUsername.Text.Trim());
                    if (newU != null)
                    {
                        AuthService.UpdateUser(newU.Id, txtFullName.Text.Trim(), txtEmail.Text.Trim(), roleId);
                    }
                    Saved = true;
                    MessageBox.Show("تم إنشاء المستخدم. سيُطلب منه تغيير كلمة المرور عند أول دخول.");
                }
                else
                {
                    // تعديل
                    AuthService.UpdateUser(_userId, txtFullName.Text.Trim(), txtEmail.Text.Trim(), roleId);
                    Saved = true;
                    MessageBox.Show("تم تحديث البيانات");
                }
                DialogResult = true;
                Close();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
    }
}