using System.Windows.Controls;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    public partial class UsersView : UserControl
    {
        public UsersView() { InitializeComponent(); DataContext = new UsersViewModel(); }
        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e) { if (DataContext is UsersViewModel vm) vm.Load(); }
    }
}