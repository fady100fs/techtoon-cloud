using System.Windows;
using System.Windows.Controls;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    public partial class PaymentsView : UserControl
    {
        public PaymentsView()
        {
            InitializeComponent();
            SetArabicTexts();
            DataContext = new PaymentsViewModel();
        }

        private void SetArabicTexts()
        {
            try { LblTitle.Text = "\u0627\u0644\u0645\u062F\u0641\u0648\u0639\u0627\u062A \u0648\u0627\u0644\u0633\u0646\u062F\u0627\u062A"; } catch { }
            try { BtnRefresh.Content = "\uD83D\uDD04 \u062A\u062D\u062F\u064A\u062B"; } catch { }
            try { BtnReceipt.Content = "+ \u0633\u0646\u062F \u0642\u0628\u0636"; } catch { }
            try { BtnPayment.Content = "+ \u0633\u0646\u062F \u0635\u0631\u0641"; } catch { }
            try { ColVoucher.Header = "\u0631\u0642\u0645 \u0627\u0644\u0633\u0646\u062F"; } catch { }
            try { ColDate.Header = "\u0627\u0644\u062A\u0627\u0631\u064A\u062E"; } catch { }
            try { ColParty.Header = "\u0627\u0644\u0637\u0631\u0641"; } catch { }
            try { ColType.Header = "\u0627\u0644\u0646\u0648\u0639"; } catch { }
            try { ColAmount.Header = "\u0627\u0644\u0645\u0628\u0644\u063A"; } catch { }
            try { ColMethod.Header = "\u0627\u0644\u0637\u0631\u064A\u0642\u0629"; } catch { }
            try { ColDel.Header = "\u062D\u0630\u0641"; } catch { }
            try { ColEdit.Header = "\u062A\u0639\u062F\u064A\u0644"; } catch { }
        }

        
        private void DgPayments_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataContext is PaymentsViewModel vm && vm.SelectedPayment != null)
            {
                vm.EditCommand.Execute(vm.SelectedPayment);
            }
        }
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is PaymentsViewModel vm) vm.LoadPayments();
        }
    }
}
