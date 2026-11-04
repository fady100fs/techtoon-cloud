using System.Windows.Controls;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    public partial class StockLevelsView : UserControl
    {
        public StockLevelsView() { InitializeComponent(); DataContext = new StockLevelsViewModel(); }
        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e) { if (DataContext is StockLevelsViewModel vm) vm.Load(); }
    }
}