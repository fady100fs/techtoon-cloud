using System.Windows;
using System.Windows.Controls;

namespace Techtoon.UI.Views
{
    public partial class PettyCashView : UserControl
    {
        public PettyCashView()
        {
            InitializeComponent();
        }

        /// <summary>P4: من العهدة إلى سلفة الموظف (مرتبط بالحساب) — حلقة مغلقة بين الصفحتين.</summary>
        private void BtnAdvance_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var win = new EmployeeAdvanceWindow(0);
                win.ShowDialog();
                if (DataContext is ViewModels.PettyCashViewModel vm) vm.LoadCustodies();
            }
            catch (Exception ex) { MessageBox.Show("خطأ: " + ex.Message); }
        }
    }
}
