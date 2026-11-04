using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    public partial class InvoicesView : UserControl
    {
        public InvoicesView() { InitializeComponent(); SetArabicTexts(); DataContext = new InvoicesViewModel(); }
        private void SetArabicTexts()
        {
            try { LblTitle.Text = "\u0627\u0644\u0641\u0648\u0627\u062A\u064A\u0631"; } catch { }
            try { BtnRefresh.Content = "\uD83D\uDD04 \u062A\u062D\u062F\u064A\u062B"; } catch { }
            try { BtnDelete.Content = "\uD83D\uDDD1 \u062D\u0630\u0641"; } catch { }
            try { BtnNew.Content = "+ \u0641\u0627\u062A\u0648\u0631\u0629 \u062C\u062F\u064A\u062F\u0629"; } catch { }
            try { ColNum.Header = "\u0627\u0644\u0631\u0642\u0645"; } catch { }
            try { ColDate.Header = "\u0627\u0644\u062A\u0627\u0631\u064A\u062E"; } catch { }
            try { ColType.Header = "\u0627\u0644\u0646\u0648\u0639"; } catch { }
            try { ColParty.Header = "\u0627\u0644\u0637\u0631\u0641"; } catch { }
            try { ColTotal.Header = "\u0625\u062C\u0645\u0627\u0644\u064A"; } catch { }
            try { ColPaid.Header = "\u0645\u062F\u0641\u0648\u0639"; } catch { }
            try { ColRem.Header = "\u0645\u062A\u0628\u0642\u064A"; } catch { }
            try { ColStatus.Header = "\u062D\u0627\u0644\u0629"; } catch { }
        }
        private void UserControl_Loaded(object sender, RoutedEventArgs e) { if (DataContext is InvoicesViewModel vm) vm.LoadInvoices(); }
        private void DgInvoices_MouseDoubleClick(object sender, MouseButtonEventArgs e) { if (DataContext is InvoicesViewModel vm && vm.SelectedInvoice != null) { var win = new InvoiceEditWindow(vm.SelectedInvoice.Id); win.ShowDialog(); vm.LoadInvoices(); } }
        private void DgInvoices_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Delete && DataContext is InvoicesViewModel vm && vm.SelectedInvoice != null) { vm.DeleteInvoiceCommand.Execute(null); e.Handled = true; } }
        private void BtnReturn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var win = new ReturnInvoiceWindow();
                win.Owner = Window.GetWindow(this);
                win.ShowDialog();
                // Refresh list after return
                if (DataContext is ViewModels.InvoicesViewModel vm)
                    vm.LoadInvoices();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }
    }
}
