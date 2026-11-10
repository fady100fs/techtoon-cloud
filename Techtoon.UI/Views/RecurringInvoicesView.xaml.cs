using System.Windows.Controls;

namespace Techtoon.UI.Views
{
    /// <summary>P5: الفواتير المتكررة (قوالب + توليد فواتير فعلية).</summary>
    public partial class RecurringInvoicesView : UserControl
    {
        public RecurringInvoicesView()
        {
            InitializeComponent();
            DataContext = new ViewModels.RecurringInvoicesViewModel();
        }
    }
}
