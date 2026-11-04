using System.Windows;
using System.Windows.Controls;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    public partial class ItemsView : UserControl
    {
        public ItemsView()
        {
            InitializeComponent();
            SetArabicTexts();
            DataContext = new ItemsViewModel();
        }

        private void SetArabicTexts()
        {
            try { LblTitle.Text = "\u0627\u0644\u0623\u0635\u0646\u0627\u0641"; } catch { }
            try { BtnRefresh.Content = "\uD83D\uDD04"; } catch { }
            try { BtnAdd.Content = "+ \u0635\u0646\u0641"; } catch { }
            try { BtnEdit.Content = "\u270E \u062A\u0639\u062F\u064A\u0644"; } catch { }
            try { BtnMoveWarehouse.Content = "\uD83D\uDCE6 \u0646\u0642\u0644"; } catch { }
            try { BtnToggleActive.Content = "\u2611 \u062A\u0641\u0639\u064A\u0644"; } catch { }
            try { BtnExport.Content = "\uD83D\uDCC4 \u062A\u0635\u062F\u064A\u0631"; } catch { }
            try { BtnImport.Content = "\uD83D\uDCC4 \u0627\u0633\u062A\u064A\u0631\u0627\u062F"; } catch { }
            try { BtnDelete.Content = "\uD83D\uDDD1"; } catch { }
            try { LblSearch.Text = "\u0628\u062D\u062B:"; } catch { }
            try { ChkSelectAll.Content = "\u062A\u062D\u062F\u064A\u062F \u0627\u0644\u0643\u0644"; } catch { }
            try { ColName.Header = "\u0627\u0644\u0627\u0633\u0645"; } catch { }
            try { ColBarcode.Header = "\u0628\u0627\u0631\u0643\u0648\u062F"; } catch { }
            try { ColStock.Header = "\u0627\u0644\u0631\u0635\u064A\u062F"; } catch { }
            try { ColSell.Header = "\u0633\u0639\u0631 \u0627\u0644\u0628\u064A\u0639"; } catch { }
            try { ColCost.Header = "\u0627\u0644\u062A\u0643\u0644\u0641\u0629"; } catch { }
            try { ColValue.Header = "\u0627\u0644\u0642\u064A\u0645\u0629"; } catch { }
            try { ColStatus.Header = "\u0627\u0644\u062D\u0627\u0644\u0629"; } catch { }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ItemsViewModel vm) vm.LoadItems();
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.DataContext is ItemRow item)
                item.IsSelected = cb.IsChecked ?? false;
        }

        private void DgItems_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DgItems.SelectedItem is ItemRow row && DataContext is ItemsViewModel vm)
            {
                var editMethod = vm.GetType().GetMethod("EditItem", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (editMethod != null) editMethod.Invoke(vm, new object[] { row });
            }
        }
    }
}