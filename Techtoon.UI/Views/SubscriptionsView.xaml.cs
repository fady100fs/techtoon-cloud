using System.Windows.Controls;

namespace Techtoon.UI.Views
{
    /// <summary>P5: تبويب الاشتراكات داخل صفحة الفواتير.</summary>
    public partial class SubscriptionsView : UserControl
    {
        public SubscriptionsView()
        {
            InitializeComponent();
            DataContext = new ViewModels.SubscriptionsViewModel();
        }
    }
}
