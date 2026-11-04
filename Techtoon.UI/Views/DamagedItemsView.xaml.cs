using System.Windows.Controls;
using System.Windows.Input;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    public partial class DamagedItemsView : UserControl
    {
        public DamagedItemsView() { InitializeComponent(); DataContext = new DamagedItemsViewModel(); }
        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e) { if (DataContext is DamagedItemsViewModel vm) vm.Load(); }
        private void Dg_MouseDoubleClick(object sender, MouseButtonEventArgs e) { if (DataContext is DamagedItemsViewModel vm && vm.OpenCommand.CanExecute(null)) vm.OpenCommand.Execute(null); }
    }
}