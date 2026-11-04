using System.Windows.Controls;
using System.Windows.Input;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    public partial class StockCountSessionsView : UserControl
    {
        public StockCountSessionsView() { InitializeComponent(); DataContext = new StockCountSessionsViewModel(); }
        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e) { if (DataContext is StockCountSessionsViewModel vm) vm.Load(); }
        private void Dg_MouseDoubleClick(object sender, MouseButtonEventArgs e) { if (DataContext is StockCountSessionsViewModel vm && vm.OpenSessionCommand.CanExecute(null)) vm.OpenSessionCommand.Execute(null); }
    }
}