using System.Windows.Controls;
using System.Windows.Input;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    public partial class StockAdjustmentsView : UserControl
    {
        public StockAdjustmentsView() { InitializeComponent(); DataContext = new StockAdjustmentsViewModel(); }
        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e) { if (DataContext is StockAdjustmentsViewModel vm) vm.Load(); }
        private void Dg_MouseDoubleClick(object sender, MouseButtonEventArgs e) { if (DataContext is StockAdjustmentsViewModel vm && vm.OpenCommand.CanExecute(null)) vm.OpenCommand.Execute(null); }
    }
}