using System.Windows;
using System.Windows.Controls;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    public partial class ReportsView : UserControl
    {
        public ReportsView()
        {
            InitializeComponent();
        }

        private void ReportType_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ReportsViewModel vm)
            {
                if (rbSales.IsChecked == true)
                {
                    vm.ReportType = "sales";
                    dgSales.Visibility = Visibility.Visible;
                    dgInventory.Visibility = Visibility.Collapsed;
                }
                else
                {
                    vm.ReportType = "inventory";
                    dgSales.Visibility = Visibility.Collapsed;
                    dgInventory.Visibility = Visibility.Visible;
                }
                vm.LoadReport();
            }
        }
    }
}
