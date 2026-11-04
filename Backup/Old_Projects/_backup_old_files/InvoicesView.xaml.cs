using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    public partial class InvoicesView : UserControl
    {
        public InvoicesView()
        {
            InitializeComponent();
            SetArabicTexts();
            DataContext = new InvoicesViewModel();
        }

        private void SetArabicTexts()
        {
            try { LblTitle.Text = "\u0627\u0644\u0641\u0648\u0627\u062A\u064A\u0631"; } catch { } // Ø§Ù„ÙÙˆØ§ØªÙŠØ±
            try { BtnRefresh.Content = "\uD83D\uDD04 \u062A\u062D\u062F\u064A\u062B"; } catch { } // ðŸ”„ ØªØ­Ø¯ÙŠØ«
            try { BtnDelete.Content = "\uD83D\uDDD1 \u062D\u0630\u0641"; } catch { } // ðŸ—‘ Ø­Ø°Ù
            try { BtnNew.Content = "+ \u0641\u0627\u062A\u0648\u0631\u0629 \u062C\u062F\u064A\u062F\u0629"; } catch { } // + ÙØ§ØªÙˆØ±Ø© Ø¬Ø¯ÙŠØ¯Ø©
            try { ColNum.Header = "\u0627\u0644\u0631\u0642\u0645"; } catch { } // Ø§Ù„Ø±Ù‚Ù…
            try { ColDate.Header = "\u0627\u0644\u062A\u0627\u0631\u064A\u062E"; } catch { } // Ø§Ù„ØªØ§Ø±ÙŠØ®
            try { ColType.Header = "\u0627\u0644\u0646\u0648\u0639"; } catch { } // Ø§Ù„Ù†ÙˆØ¹
            try { ColParty.Header = "\u0627\u0644\u0637\u0631\u0641"; } catch { } // Ø§Ù„Ø·Ø±Ù
            try { ColTotal.Header = "\u0625\u062C\u0645\u0627\u0644\u064A"; } catch { } // Ø§Ù„Ø¥Ø¬Ù…Ø§Ù„ÙŠ
            try { ColPaid.Header = "\u0645\u062F\u0641\u0648\u0639"; } catch { } // Ù…Ø¯ÙÙˆØ¹
            try { ColRem.Header = "\u0645\u062A\u0628\u0642\u064A"; } catch { } // Ù…ØªØ¨Ù‚ÙŠ
            try { ColStatus.Header = "\u062D\u0627\u0644\u0629"; } catch { } // Ø­Ø§Ù„Ø©
            try { ColDel.Header = "\u062D\u0630\u0641"; } catch { } // Ø­Ø°Ù
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is InvoicesViewModel vm) vm.LoadInvoices();
        }

        private void DgInvoices_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is InvoicesViewModel vm && vm.SelectedInvoice != null)
            {
                var win = new InvoiceEditWindow(vm.SelectedInvoice.Id);
                win.ShowDialog();
                vm.LoadInvoices();
            }
        }

        private void DgInvoices_KeyDown(object sender, KeyEventArgs e)
        {
            // Ø­Ø°Ù Ø¨Ù…ÙØªØ§Ø­ Delete
            if (e.Key == Key.Delete)
            {
                if (DataContext is InvoicesViewModel vm && vm.SelectedInvoice != null)
                {
                    var result = MessageBox.Show(
                        "\u0647\u0644 \u062A\u0631\u064A\u062F \u062D\u0630\u0641 \u0627\u0644\u0641\u0627\u062A\u0648\u0631\u0629 \u0631\u0642\u0645 " + vm.SelectedInvoice.InvoiceNumber + "?",
                        "\u062A\u0623\u0643\u064A\u062F \u0627\u0644\u062D\u0630\u0641",
                        MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        vm.DeleteInvoice(vm.SelectedInvoice.Id);
                    }
                    e.Handled = true;
                }
            }
        }
    }
}