using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Techtoon.UI.Services;
using Techtoon.UI.Views;

namespace Techtoon.UI.ViewModels
{
    public class UsersViewModel : ViewModelBase
    {
        public ObservableCollection<AuthService.UserWithRole> Users { get; set; }
        private AuthService.UserWithRole _selected;
        public AuthService.UserWithRole Selected { get => _selected; set { _selected = value; OnPropertyChanged("Selected"); } }

        private string _search = "";
        public string SearchText { get => _search; set { _search = value; OnPropertyChanged("SearchText"); } }

        public ICommand RefreshCommand { get; set; }
        public ICommand NewCommand { get; set; }
        public ICommand EditCommand { get; set; }
        public ICommand ToggleActiveCommand { get; set; }
        public ICommand ResetPasswordCommand { get; set; }
        public ICommand PermissionsCommand { get; set; }
        public ICommand SearchCommand { get; set; }

        public UsersViewModel()
        {
            Users = new ObservableCollection<AuthService.UserWithRole>();
            RefreshCommand = new UiRelayCommand(_ => Load());
            NewCommand = new UiRelayCommand(_ => NewUser());
            EditCommand = new UiRelayCommand(_ => EditUser(), _ => Selected != null);
            ToggleActiveCommand = new UiRelayCommand(_ => ToggleActive(), _ => Selected != null);
            ResetPasswordCommand = new UiRelayCommand(_ => ResetPassword(), _ => Selected != null);
            PermissionsCommand = new UiRelayCommand(_ => ManagePermissions(), _ => Selected != null);
            SearchCommand = new UiRelayCommand(_ => Load());
            Load();
        }

        public void Load()
        {
            Users.Clear();
            try
            {
                var list = AuthService.GetAllUsers(string.IsNullOrEmpty(SearchText) ? null : SearchText);
                foreach (var u in list) Users.Add(u);
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void NewUser()
        {
            try
            {
                var win = new UserEditWindow();
                win.Owner = Application.Current.MainWindow;
                if (win.ShowDialog() == true) Load();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void EditUser()
        {
            if (Selected == null) return;
            try
            {
                var win = new UserEditWindow();
                win.LoadForEdit(Selected.Id);
                win.Owner = Application.Current.MainWindow;
                if (win.ShowDialog() == true) Load();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void ToggleActive()
        {
            if (Selected == null) return;
            var action = Selected.IsActive ? "تعطيل" : "تفعيل";
            if (MessageBox.Show($"{action} حساب '{Selected.Username}'؟", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            try
            {
                AuthService.ToggleUserActive(Selected.Id);
                Load();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void ResetPassword()
        {
            if (Selected == null) return;
            var pwd = Microsoft.VisualBasic.Interaction.InputBox($"أدخل كلمة المرور الجديدة لـ '{Selected.Username}':", "إعادة تعيين كلمة المرور", "", -1, -1);
            if (string.IsNullOrEmpty(pwd)) return;
            try
            {
                AuthService.ResetPassword(Selected.Id, pwd);
                MessageBox.Show("تم إعادة تعيين كلمة المرور. سيُطلب من المستخدم تغييرها عند أول دخول.");
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void ManagePermissions()
        {
            if (Selected == null) return;
            try
            {
                var win = new UserPermissionsWindow();
                win.LoadForUser(Selected.Id, Selected.Username);
                win.Owner = Application.Current.MainWindow;
                win.ShowDialog();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }
    }
}