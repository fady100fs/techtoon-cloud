using System.Windows;
using System.Windows.Controls;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
            SetArabicTexts();
            DataContext = new DashboardViewModel();
        }

        private void SetArabicTexts()
        {
            try { LblTitle.Text = "\u0644\u0648\u062D\u0629 \u0627\u0644\u062A\u062D\u0643\u0645"; } catch { }
            try { BtnRefresh.Content = "\uD83D\uDD04 \u062A\u062D\u062F\u064A\u062B"; } catch { }
            try { LblTopItems.Text = "\u0623\u0641\u0636\u0644 \u0627\u0644\u0623\u0635\u0646\u0627\u0641"; } catch { }
            try { LblLowStock.Text = "\u0623\u0635\u0646\u0627\u0641 \u0645\u0646\u062E\u0641\u0636\u0629 \u0627\u0644\u0645\u062E\u0632\u0648\u0646"; } catch { }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is DashboardViewModel vm) vm.LoadDashboard();
        }
    }
}
