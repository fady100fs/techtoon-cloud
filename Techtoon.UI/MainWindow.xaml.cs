using System.Windows;
using System.Windows.Input;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            PasswordBox.KeyDown += PasswordBox_KeyDown;
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.Password = PasswordBox.Password;
                    vm.LoginCommand.Execute(null);
                }
            }
        }
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+N: فاتورة جديدة
            if (e.Key == Key.N && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (DataContext is ViewModels.MainViewModel vm && vm.IsLoggedIn)
                {
                    vm.NavigateCommand.Execute("Invoices");
                    e.Handled = true;
                }
            }
            // Ctrl+D: لوحة التحكم
            else if (e.Key == Key.D && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (DataContext is ViewModels.MainViewModel vm && vm.IsLoggedIn)
                {
                    vm.NavigateCommand.Execute("Dashboard");
                    e.Handled = true;
                }
            }
            // Ctrl+U: إدارة المستخدمين
            else if (e.Key == Key.U && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (DataContext is ViewModels.MainViewModel vm && vm.IsLoggedIn)
                {
                    vm.NavigateCommand.Execute("Users");
                    e.Handled = true;
                }
            }
            // F5: تحديث الصفحة الحالية
            else if (e.Key == Key.F5)
            {
                if (DataContext is ViewModels.MainViewModel vm && vm.CurrentView is ViewModels.ViewModelBase vmb)
                {
                    // محاولة استدعاء RefreshCommand لو موجود
                    var refreshCmd = vmb.GetType().GetProperty("RefreshCommand")?.GetValue(vmb) as ICommand;
                    refreshCmd?.Execute(null);
                    e.Handled = true;
                }
            }
        }
    }
}