using System.Windows.Controls;
using System.Windows.Input;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    public partial class WriteoffRequestsView : UserControl
    {
        public WriteoffRequestsView() { InitializeComponent(); DataContext = new WriteoffRequestsViewModel(); }
        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e) { if (DataContext is WriteoffRequestsViewModel vm) vm.Load(); }
        private void Dg_MouseDoubleClick(object sender, MouseButtonEventArgs e) { if (DataContext is WriteoffRequestsViewModel vm && vm.OpenCommand.CanExecute(null)) vm.OpenCommand.Execute(null); }
    }
}