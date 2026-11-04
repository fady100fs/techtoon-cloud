using System.Windows;
using System.Windows.Controls;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    public partial class AccountsView : UserControl
    {
        public AccountsView()
        {
            InitializeComponent();
            SetArabicTexts();
            DataContext = new AccountsViewModel();
        }

        private void SetArabicTexts()
        {
            try { LblTitle.Text = "\u0634\u062C\u0631\u0629 \u0627\u0644\u062D\u0633\u0627\u0628\u0627\u062A"; } catch { }
            try { BtnAddRoot.Content = "+ \u0631\u0626\u064A\u0633\u064A"; } catch { }
            try { BtnAddChild.Content = "+ \u0641\u0631\u0639\u064A"; } catch { }
            try { BtnEdit.Content = "\u270E"; } catch { }
            try { BtnDelete.Content = "\uD83D\uDDD1"; } catch { }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e) { }

        private void TvAccounts_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is AccountsViewModel vm && e.NewValue is AccountNode node)
                vm.SelectedNode = node;
        }
    }
}
