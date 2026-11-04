using System.Windows.Controls;
using System.Windows.Input;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    public partial class LoansView : UserControl
    {
        public LoansView() { InitializeComponent(); DataContext = new LoansViewModel(); }
        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e) { if (DataContext is LoansViewModel vm) vm.Load(); }
        private void Dg_MouseDoubleClick(object sender, MouseButtonEventArgs e) { if (DataContext is LoansViewModel vm && vm.EditCommand.CanExecute(null)) vm.EditCommand.Execute(null); }
    }
}