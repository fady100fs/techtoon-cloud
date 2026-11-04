# -*- coding: utf-8 -*-
"""fix_arrows_textbox_popup.py - استبدال ComboBox بـ TextBox + Popup للتحكم الكامل في الأسهم"""
import os, re

PROJECT = r"D:\PROG\Accounting Prog\Techtoon Accounting C+"
XAML_FILE = os.path.join(PROJECT, "Techtoon.UI", "Views", "InvoiceEditWindow.xaml")
CS_FILE = os.path.join(PROJECT, "Techtoon.UI", "Views", "InvoiceEditWindow.xaml.cs")

print("=" * 60)
print("  Replacing ComboBox with TextBox + Popup")
print("=" * 60)

# ============================================================
# 1) تعديل XAML: استبدال ComboBox بـ TextBox + Popup
# ============================================================
print("\n[1/2] Patching XAML...")
with open(XAML_FILE, "r", encoding="ascii") as f:
    xaml = f.read()

# النمط القديم (ComboBox)
old_combo_pattern = r'<ComboBox x:Name="ItemSearchCombo".*?/>'
# النمط الجديد (TextBox + Popup + ListBox)
new_textbox_popup = """<Grid>
                                        <Popup x:Name="SearchPopup" IsOpen="False" Placement="Bottom" PlacementTarget="{Binding ElementName=SearchBoxInner}" StaysOpen="False" AllowsTransparency="True" PopupAnimation="Slide">
                                            <Border Background="White" BorderBrush="#00897B" BorderThickness="1" CornerRadius="2" MaxHeight="260" MinWidth="300">
                                                <ListBox x:Name="SearchList" ItemsSource="{Binding FilteredItems}" BorderThickness="0" MaxHeight="260" PreviewMouseLeftButtonDown="SearchList_PreviewMouseLeftButtonDown"/>
                                            </Border>
                                        </Popup>
                                        <TextBox x:Name="SearchBoxInner" Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged, Mode=TwoWay}" Tag="search" Padding="6,4" BorderBrush="#80CBC4" BorderThickness="1,1,1,2" Background="White" TextChanged="SearchBox_TextChanged" PreviewKeyDown="SearchBox_PreviewKeyDown"/>
                                    </Grid>"""

xaml_new = re.sub(old_combo_pattern, new_textbox_popup, xaml, count=1, flags=re.DOTALL)

if xaml_new != xaml:
    with open(XAML_FILE, "w", encoding="ascii") as f:
        f.write(xaml_new)
    print("  OK: XAML updated (ComboBox -> TextBox + Popup)")
else:
    print("  WARN: ComboBox pattern not found")

# ============================================================
# 2) تعديل Code-behind: تحديث معالجات الأحداث
# ============================================================
print("\n[2/2] Patching Code-behind...")
with open(CS_FILE, "r", encoding="utf-8-sig") as f:
    cs = f.read()

# استبدال كل دوال البحث القديمة بدوال جديدة تتعامل مع TextBox + Popup
old_methods_pattern = r'private void ItemSearchCombo_TextChanged.*?private void PaymentsGrid_CellEditEnding'
new_methods = r'''private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (sender is not TextBox tb) return;
                if (tb.DataContext is not InvoiceLineItem line) return;

                line.SearchText = tb.Text;
                UpdatePopup(tb, line);
            }
            catch { }
        }

        private void UpdatePopup(TextBox tb, InvoiceLineItem line)
        {
            try
            {
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

        private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (sender is not TextBox tb) return;
                if (tb.DataContext is not InvoiceLineItem line) return;

                if (FindPopupAndListBox(tb) is not (Popup popup, ListBox lb)) return;

                switch (e.Key)
                {
                    case Key.Down:
                        if (lb.Items.Count > 0)
                        {
                            popup.IsOpen = true;
                            if (lb.SelectedIndex < 0) lb.SelectedIndex = 0;
                            else if (lb.SelectedIndex < lb.Items.Count - 1) lb.SelectedIndex++;
                            lb.ScrollIntoView(lb.SelectedItem);
                        }
                        e.Handled = true;
                        break;
                    case Key.Up:
                        if (lb.Items.Count > 0)
                        {
                            popup.IsOpen = true;
                            if (lb.SelectedIndex < 0) lb.SelectedIndex = lb.Items.Count - 1;
                            else if (lb.SelectedIndex > 0) lb.SelectedIndex--;
                            lb.ScrollIntoView(lb.SelectedItem);
                        }
                        e.Handled = true;
                        break;
                    case Key.Enter:
                        if (popup.IsOpen && lb.SelectedIndex >= 0 && lb.Items[lb.SelectedIndex] is ItemSearchResult item)
                        {
                            line.SelectItem(item);
                            popup.IsOpen = false;
                            FocusQuantity(line);
                        }
                        else
                        {
                            popup.IsOpen = false;
                            FocusQuantity(line);
                        }
                        e.Handled = true;
                        break;
                    case Key.Escape:
                        popup.IsOpen = false;
                        e.Handled = true;
                        break;
                }
            }
            catch { }
        }

        private void SearchList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is ListBox lb)
                {
                    var hit = lb.InputHitTest(e.GetPosition(lb)) as DependencyObject;
                    while (hit != null && hit != lb)
                    {
                        if (hit is ListBoxItem lbi)
                        {
                            lb.SelectedItem = lbi.DataContext;
                            if (lb.SelectedItem is ItemSearchResult item)
                            {
                                if (FindInvoiceLineItem(lb) is InvoiceLineItem line)
                                {
                                    line.SelectItem(item);
                                    if (FindPopupAndListBoxFromListBox(lb) is Popup popup) popup.IsOpen = false;
                                    FocusQuantity(line);
                                }
                            }
                            break;
                        }
                        hit = VisualTreeHelper.GetParent(hit);
                    }
                }
            }
            catch { }
        }

        private void FocusQuantity(InvoiceLineItem line)
        {
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

        private void LinesGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // فارغ عمداً - دع SearchBox_PreviewKeyDown يعالج الأسهم
        }

        private (Popup, ListBox)? FindPopupAndListBox(TextBox tb)
        {
            try
            {
                if (tb.Parent is Grid grid)
                {
                    foreach (var child in grid.Children)
                    {
                        if (child is Popup popup && popup.Child is Border border && border.Child is ListBox lb)
                        {
                            return (popup, lb);
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

        private void PaymentsGrid_CellEditEnding'''

cs_new = re.sub(old_methods_pattern, new_methods, cs, count=1, flags=re.DOTALL)

if cs_new != cs:
    with open(CS_FILE, "w", encoding="utf-8-sig") as f:
        f.write(cs_new)
    print("  OK: Code-behind updated (TextBox + Popup handlers)")
else:
    print("  ERROR: Could not patch code-behind")

print("\n" + "=" * 60)
print("  DONE! Now: dotnet build Techtoon.UI")
print("=" * 60)
