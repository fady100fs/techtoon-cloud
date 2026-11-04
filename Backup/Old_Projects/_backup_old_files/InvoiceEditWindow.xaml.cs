using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    public partial class InvoiceEditWindow : Window
    {
        private readonly InvoiceEditViewModel _vm;
        private readonly int? _editId;
        private bool _isArrowNav = false;
        private TextBox? _activeSearchBox;
        private Popup? _activePopup;
        private ListBox? _activeListBox;

        public InvoiceEditWindow(int? editId = null)
        {
            InitializeComponent();
            _editId = editId;
            _vm = new InvoiceEditViewModel();
            if (editId.HasValue) _vm.Id = editId.Value;
            DataContext = _vm;
            _vm.RequestClose += () => { DialogResult = _vm.DialogResult; Close(); };
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SetArabicTexts();
            FillTypeCombo();
            await _vm.LoadAsync();
            if (_editId.HasValue) await _vm.LoadFromInvoiceAsync(_editId.Value);
        }

        private void SetArabicTexts()
        {
            try { Title = "\u0641\u0627\u062A\u0648\u0631\u0629"; } catch { }
            try { HeaderIcon.Text = "\uD83E\uDDF9"; } catch { }
            try { HeaderTitle.Text = "\u0641\u0627\u062A\u0648\u0631\u0629"; } catch { }
            try { LblSerial.Text = "\u0627\u0644\u0631\u0642\u0645:"; } catch { }
            try { LblDate.Text = "\u0627\u0644\u062A\u0627\u0631\u064A\u062E:"; } catch { }
            try { LblType.Text = "\u0627\u0644\u0646\u0648\u0639:"; } catch { }
            try { LblParty.Text = "\u0627\u0644\u0637\u0631\u0641:"; } catch { }
            try { LblWarehouse.Text = "\u0627\u0644\u0645\u062E\u0632\u0646:"; } catch { }
            try { LblItems.Text = "\u0628\u0646\u0648\u062F \u0627\u0644\u0641\u0627\u062A\u0648\u0631\u0629"; } catch { }
            try { BtnAddLine.Content = "+ \u0625\u0636\u0627\u0641\u0629 \u0635\u0646\u0641"; } catch { }
            try { ColItem.Header = "\u0635\u0646\u0641 (\u0627\u0643\u062A\u0628 \u0644\u0644\u0628\u062D\u062B)"; } catch { }
            try { ColBarcode.Header = "\u0628\u0627\u0631\u0643\u0648\u062F"; } catch { }
            try { ColQty.Header = "\u0627\u0644\u0643\u0645\u064A\u0629"; } catch { }
            try { ColPrice.Header = "\u0627\u0644\u0633\u0639\u0631"; } catch { }
            try { ColDisc.Header = "\u062E\u0635\u0645"; } catch { }
            try { ColTotal.Header = "\u0625\u062C\u0645\u0627\u0644\u064A"; } catch { }
            try { ColDel.Header = "\u062D\u0630\u0641"; } catch { }
            try { LblPayments.Text = "\u0637\u0631\u0642 \u0627\u0644\u062F\u0641\u0639 \u0627\u0644\u0645\u062A\u0639\u062F\u062F\u0629"; } catch { }
            try { BtnAddPay.Content = "+ \u0625\u0636\u0627\u0641\u0629 \u062F\u0641\u0639\u0629"; } catch { }
            try { BtnPayFull.Content = "\uD83D\uDCB5 \u062F\u0641\u0639 \u0628\u0627\u0644\u0643\u0627\u0645\u0644"; } catch { }
            try { ColPayMethod.Header = "\u0637\u0631\u064A\u0642\u0629"; } catch { }
            try { ColPayAmount.Header = "\u0627\u0644\u0645\u0628\u0644\u063A"; } catch { }
            try { ColPayRef.Header = "\u0645\u0631\u062C\u0639"; } catch { }
            try { ColPayDate.Header = "\u062A\u0627\u0631\u064A\u062E"; } catch { }
            try { ColPayDel.Header = "\u062D\u0630\u0641"; } catch { }
            try { LblSubtotal.Text = "\u0627\u0644\u0645\u062C\u0645\u0648\u0639:"; } catch { }
            try { LblDisc.Text = "\u062E\u0635\u0645:"; } catch { }
            try { LblTotal.Text = "\u0625\u062C\u0645\u0627\u0644\u064A:"; } catch { }
            try { LblPaid.Text = "\u0627\u0644\u0645\u062F\u0641\u0648\u0639:"; } catch { }
            try { LblRemaining.Text = "\u0627\u0644\u0645\u062A\u0628\u0642\u064A:"; } catch { }
            try { BtnSave.Content = "\uD83D\uDCBE \u062D\u0641\u0638 \u0627\u0644\u0641\u0627\u062A\u0648\u0631\u0629"; } catch { }
            try { BtnCancel.Content = "\u2716 \u0625\u0644\u063A\u0627\u0621"; } catch { }
        }

        private void FillTypeCombo()
        {
            try
            {
                CmbType.Items.Clear();
                CmbType.Items.Add(new ComboBoxItem { Content = "\u0645\u0628\u064A\u0639\u0627\u062A", Tag = "sales" });
                CmbType.Items.Add(new ComboBoxItem { Content = "\u0645\u0634\u062A\u0631\u064A\u0627\u062A", Tag = "purchase" });
            }
            catch { }
        }

        // ============================================================
        //  البحث: TextBox + Popup (بدون ComboBox)
        // ============================================================
        
        // عند الكتابة في حقل البحث
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (sender is not TextBox tb) return;
                if (tb.DataContext is not InvoiceLineItem line) return;

                line.SearchText = tb.Text;
                
                // ابحث عن الـ Popup والـ ListBox المرتبط
                if (FindPopupAndListBox(tb) is (Popup popup, ListBox lb))
                {
                    lb.ItemsSource = line.FilteredItems;
                    if (line.FilteredItems.Count > 0 && !string.IsNullOrEmpty(tb.Text))
                    {
                        popup.IsOpen = true;
                        lb.SelectedIndex = -1;
                    }
                    else
                    {
                        popup.IsOpen = false;
                    }
                }
            }
            catch { }
        }

        // معالجة الأسهم في حقل البحث
        private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (sender is not TextBox tb) return;
                if (tb.DataContext is not InvoiceLineItem line) return;

                if (FindPopupAndListBox(tb) is (Popup popup, ListBox lb))
                {
                    switch (e.Key)
                    {
                        case Key.Down:
                            if (lb.Items.Count > 0)
                            {
                                popup.IsOpen = true;
                                _isArrowNav = true;
                                if (lb.SelectedIndex < 0) lb.SelectedIndex = 0;
                                else if (lb.SelectedIndex < lb.Items.Count - 1) lb.SelectedIndex++;
                                lb.ScrollIntoView(lb.SelectedItem);
                                _isArrowNav = false;
                            }
                            e.Handled = true;
                            break;
                        case Key.Up:
                            if (lb.Items.Count > 0)
                            {
                                popup.IsOpen = true;
                                _isArrowNav = true;
                                if (lb.SelectedIndex < 0) lb.SelectedIndex = lb.Items.Count - 1;
                                else if (lb.SelectedIndex > 0) lb.SelectedIndex--;
                                lb.ScrollIntoView(lb.SelectedItem);
                                _isArrowNav = false;
                            }
                            e.Handled = true;
                            break;
                        case Key.Enter:
                            if (popup.IsOpen && lb.SelectedIndex >= 0 && lb.Items[lb.SelectedIndex] is ItemSearchResult item)
                            {
                                line.SelectItem(item);
                                popup.IsOpen = false;
                                // انتقل لحقل الكمية
                                Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    try
                                    {
                                        LinesGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                                        LinesGrid.CommitEdit(DataGridEditingUnit.Row, true);
                                        if (LinesGrid.ItemContainerGenerator.ContainerFromItem(line) is DataGridRow row)
                                        {
                                            var qtyTb = FindDescendant<TextBox>(row, x => (x.Tag as string) == "qty");
                                            qtyTb?.Focus();
                                            qtyTb?.SelectAll();
                                        }
                                    }
                                    catch { }
                                }), System.Windows.Threading.DispatcherPriority.Input);
                            }
                            else
                            {
                                // Enter بدون اختيار - انتقل للكمية
                                popup.IsOpen = false;
                                Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    try
                                    {
                                        LinesGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                                        LinesGrid.CommitEdit(DataGridEditingUnit.Row, true);
                                        if (LinesGrid.ItemContainerGenerator.ContainerFromItem(line) is DataGridRow row)
                                        {
                                            var qtyTb = FindDescendant<TextBox>(row, x => (x.Tag as string) == "qty");
                                            qtyTb?.Focus();
                                            qtyTb?.SelectAll();
                                        }
                                    }
                                    catch { }
                                }), System.Windows.Threading.DispatcherPriority.Input);
                            }
                            e.Handled = true;
                            break;
                        case Key.Escape:
                            popup.IsOpen = false;
                            e.Handled = true;
                            break;
                    }
                }
            }
            catch { }
        }

        // اختيار صنف بالنقر بالماوس
        private void SearchList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is ListBox lb && lb.SelectedItem is ItemSearchResult item)
                {
                    if (FindInvoiceLineItem(lb) is InvoiceLineItem line)
                    {
                        line.SelectItem(item);
                        if (FindPopupAndListBoxFromListBox(lb) is Popup popup)
                        {
                            popup.IsOpen = false;
                        }
                        // انتقل للكمية
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            try
                            {
                                LinesGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                                LinesGrid.CommitEdit(DataGridEditingUnit.Row, true);
                                if (LinesGrid.ItemContainerGenerator.ContainerFromItem(line) is DataGridRow row)
                                {
                                    var qtyTb = FindDescendant<TextBox>(row, x => (x.Tag as string) == "qty");
                                    qtyTb?.Focus();
                                    qtyTb?.SelectAll();
                                }
                            }
                            catch { }
                        }), System.Windows.Threading.DispatcherPriority.Input);
                    }
                }
            }
            catch { }
        }

        // اختيار صنف بالنقر على عنصر
        private void SearchList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is ListBox lb)
                {
                    var item = lb.InputHitTest(e.GetPosition(lb)) as DependencyObject;
                    while (item != null && item != lb)
                    {
                        if (item is ListBoxItem lbi)
                        {
                            lb.SelectedItem = lbi.DataContext;
                            if (lb.SelectedItem is ItemSearchResult searchItem && FindInvoiceLineItem(lb) is InvoiceLineItem line)
                            {
                                line.SelectItem(searchItem);
                                if (FindPopupAndListBoxFromListBox(lb) is Popup popup)
                                {
                                    popup.IsOpen = false;
                                }
                                Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    try
                                    {
                                        LinesGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                                        LinesGrid.CommitEdit(DataGridEditingUnit.Row, true);
                                        if (LinesGrid.ItemContainerGenerator.ContainerFromItem(line) is DataGridRow row)
                                        {
                                            var qtyTb = FindDescendant<TextBox>(row, x => (x.Tag as string) == "qty");
                                            qtyTb?.Focus();
                                            qtyTb?.SelectAll();
                                        }
                                    }
                                    catch { }
                                }), System.Windows.Threading.DispatcherPriority.Input);
                            }
                            break;
                        }
                        item = VisualTreeHelper.GetParent(item);
                    }
                }
            }
            catch { }
        }

        // ============================================================
        //  دوال مساعدة للبحث عن Popup و ListBox
        // ============================================================
        private (Popup, ListBox)? FindPopupAndListBox(TextBox tb)
        {
            try
            {
                if (tb.Parent is Grid grid)
                {
                    foreach (var child in grid.Children)
                    {
                        if (child is Popup popup)
                        {
                            if (popup.Child is Border border && border.Child is ListBox lb)
                            {
                                return (popup, lb);
                            }
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        private Popup? FindPopupAndListBoxFromListBox(ListBox lb)
        {
            try
            {
                var parent = VisualTreeHelper.GetParent(lb);
                while (parent != null)
                {
                    if (parent is Popup popup) return popup;
                    parent = VisualTreeHelper.GetParent(parent);
                }
            }
            catch { }
            return null;
        }

        private InvoiceLineItem? FindInvoiceLineItem(ListBox lb)
        {
            try
            {
                var parent = VisualTreeHelper.GetParent(lb);
                while (parent != null)
                {
                    if (parent is Grid grid && grid.DataContext is InvoiceLineItem line)
                    {
                        return line;
                    }
                    parent = VisualTreeHelper.GetParent(parent);
                }
            }
            catch { }
            return null;
        }

        // تنقل Enter بين الخلايا
        private void Cell_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            if (sender is not TextBox tb) return;
            if (tb.Tag is not string tag) return;
            if (tb.DataContext is not InvoiceLineItem line) return;
            LinesGrid.CommitEdit(DataGridEditingUnit.Row, true);
            int idx = _vm.Lines.IndexOf(line);
            int last = _vm.Lines.Count - 1;
            if (idx >= 0 && idx < last)
            {
                if (LinesGrid.ItemContainerGenerator.ContainerFromIndex(idx + 1) is DataGridRow nextRow)
                {
                    var next = FindDescendant<TextBox>(nextRow, x => (x.Tag as string) == tag);
                    if (next != null) { next.Focus(); next.SelectAll(); }
                }
            }
            else if (idx == last)
            {
                _vm.AddLineCommand.Execute(null);
                LinesGrid.UpdateLayout();
                if (LinesGrid.ItemContainerGenerator.ContainerFromIndex(_vm.Lines.Count - 1) is DataGridRow newRow)
                {
                    var searchBox = FindDescendant<TextBox>(newRow, x => (x.Tag as string) == "search");
                    searchBox?.Focus();
                }
            }
            e.Handled = true;
        }

        private static T? FindDescendant<T>(DependencyObject? parent, Func<T, bool> predicate) where T : class
        {
            if (parent == null) return null;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T matched && predicate(matched)) return matched;
                var deep = FindDescendant(child, predicate);
                if (deep != null) return deep;
            }
            return null;
        }

        private void PaymentsGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit) _vm.RefreshTotals();
        }
    }
}