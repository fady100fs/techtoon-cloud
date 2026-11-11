using System.Windows.Controls;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    /// <summary>صفحة الاستيراد والتصدير المُجمَّع (Bundles).</summary>
    public partial class DataExchangeView : UserControl
    {
        public DataExchangeView()
        {
            InitializeComponent();

            var vm = new DataExchangeViewModel();
            DataContext = vm;

            // أوضاع التعامل مع الصفوف الموجودة
            RbSkip.Checked += (_, _) => vm.ConflictModeIndex = 0;
            RbUpdate.Checked += (_, _) => vm.ConflictModeIndex = 1;
            RbInsert.Checked += (_, _) => vm.ConflictModeIndex = 2;
        }
    }
}
