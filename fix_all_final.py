# fix_all_final.py - إصلاح شامل: ترميز XAML + appsettings + تنظيف
import os, shutil

PROJECT = r"D:\PROG\Accounting Prog\Techtoon Accounting C+"
VIEWS = os.path.join(PROJECT, "Techtoon.UI", "Views")
UI_DIR = os.path.join(PROJECT, "Techtoon.UI")

print("=" * 60)
print("  Techtoon - Final Complete Fix")
print("=" * 60)

# 1) إنشاء appsettings.json
print("\n[1/4] Ensuring appsettings.json exists...")
appsettings_path = os.path.join(UI_DIR, "appsettings.json")
with open(appsettings_path, "w", encoding="utf-8") as f:
    f.write('{\n  "ConnectionStrings": {\n    "DefaultConnection": "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=npg_K9nHVkY5OjRC;Database=neondb;SSL Mode=Require;Trust Server Certificate=true"\n  }\n}\n')
print(f"  OK: {appsettings_path}")

bin_dir = os.path.join(UI_DIR, "bin", "Debug", "net10.0-windows")
if os.path.isdir(bin_dir):
    shutil.copy2(appsettings_path, os.path.join(bin_dir, "appsettings.json"))
    print(f"  OK: copied to bin folder")
# 2) كتابة XAML بـ HTML entities (يضمن ظهور العربية بغض النظر عن الترميز)
print("\n[2/4] Writing InvoiceEditWindow.xaml with HTML entities...")
xaml_path = os.path.join(VIEWS, "InvoiceEditWindow.xaml")

XAML = '''<?xml version="1.0" encoding="utf-8"?>
<Window x:Class="Techtoon.UI.Views.InvoiceEditWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="&#1601;&#1575;&#1578;&#1608;&#1585;&#1577; &#1580;&#1583;&#1610;&#1583;&#1577;" Height="820" Width="1180"
        WindowStartupLocation="CenterScreen" FlowDirection="RightToLeft"
        Background="#F5F7FA" Loaded="Window_Loaded" FontFamily="Tahoma">
    <Window.Resources>
        <Style x:Key="HeaderLabel" TargetType="TextBlock">
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
            <Setter Property="Foreground" Value="#37474F"/>
            <Setter Property="Margin" Value="0,0,6,0"/>
            <Setter Property="VerticalAlignment" Value="Center"/>
        </Style>
        <Style x:Key="InputBox" TargetType="TextBox">
            <Setter Property="Padding" Value="6,4"/>
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="BorderBrush" Value="#B0BEC5"/>
            <Setter Property="BorderThickness" Value="1"/>
            <Setter Property="Background" Value="White"/>
        </Style>
        <Style x:Key="InputCombo" TargetType="ComboBox">
            <Setter Property="Padding" Value="6,4"/>
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="BorderBrush" Value="#B0BEC5"/>
            <Setter Property="BorderThickness" Value="1"/>
            <Setter Property="Background" Value="White"/>
        </Style>
        <Style x:Key="AddBtn" TargetType="Button">
            <Setter Property="Background" Value="#26A69A"/>
            <Setter Property="Foreground" Value="White"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="Padding" Value="14,6"/>
            <Setter Property="BorderThickness" Value="0"/>
            <Setter Property="Cursor" Value="Hand"/>
        </Style>
    </Window.Resources>
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        <Border Grid.Row="0" Background="White" Padding="16,10" BorderBrush="#E0E0E0" BorderThickness="0,0,0,1">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>
                <StackPanel Grid.Column="0" Orientation="Horizontal" VerticalAlignment="Center">
                    <TextBlock Text="&#1595;" FontSize="28" Margin="0,0,10,0" VerticalAlignment="Center"/>
                    <StackPanel>
                        <TextBlock Text="&#1601;&#1575;&#1578;&#1608;&#1585;&#1577;" FontSize="18" FontWeight="Bold" Foreground="#00695C"/>
                        <TextBlock Text="{Binding InvoiceTypeDisplay}" FontSize="12" Foreground="#78909C"/>
                    </StackPanel>
                </StackPanel>
                <Grid Grid.Column="1" Margin="20,0,0,0">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto"/><ColumnDefinition Width="140"/>
                        <ColumnDefinition Width="18"/><ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="150"/><ColumnDefinition Width="18"/>
                        <ColumnDefinition Width="Auto"/><ColumnDefinition Width="120"/>
                    </Grid.ColumnDefinitions>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/><RowDefinition Height="8"/><RowDefinition Height="Auto"/>
                    </Grid.RowDefinitions>
                    <TextBlock Grid.Row="0" Grid.Column="0" Text="&#1575;&#1604;&#1585;&#1602;&#1605;:" Style="{StaticResource HeaderLabel}"/>
                    <TextBox Grid.Row="0" Grid.Column="1" Text="{Binding SerialNumber}" Style="{StaticResource InputBox}" IsReadOnly="True"/>
                    <TextBlock Grid.Row="0" Grid.Column="3" Text="&#1575;&#1604;&#1578;&#1575;&#1585;&#1610;&#1582;:" Style="{StaticResource HeaderLabel}"/>
                    <DatePicker Grid.Row="0" Grid.Column="4" SelectedDate="{Binding InvoiceDate}" FontSize="13"/>
                    <TextBlock Grid.Row="0" Grid.Column="6" Text="&#1575;&#1604;&#1606;&#1608;&#1593;:" Style="{StaticResource HeaderLabel}"/>
                    <ComboBox Grid.Row="0" Grid.Column="7" Style="{StaticResource InputCombo}" SelectedValue="{Binding InvoiceType}" SelectedValuePath="Tag">
                        <ComboBoxItem Content="&#1605;&#1576;&#1610;&#1593;&#1575;&#1578;" Tag="sales"/>
                        <ComboBoxItem Content="&#1605;&#1588;&#1578;&#1585;&#1610;&#1575;&#1578;" Tag="purchase"/>
                    </ComboBox>
                    <TextBlock Grid.Row="2" Grid.Column="0" Text="&#1575;&#1604;&#1591;&#1585;&#1601;:" Style="{StaticResource HeaderLabel}"/>
                    <ComboBox Grid.Row="2" Grid.Column="1" Grid.ColumnSpan="4" Style="{StaticResource InputCombo}" ItemsSource="{Binding Parties}" DisplayMemberPath="Name" SelectedValuePath="Id" SelectedValue="{Binding PartyId}"/>
                    <TextBlock Grid.Row="2" Grid.Column="6" Text="&#1575;&#1604;&#1605;&#1582;&#1586;&#1606;:" Style="{StaticResource HeaderLabel}"/>
                    <ComboBox Grid.Row="2" Grid.Column="7" Style="{StaticResource InputCombo}" ItemsSource="{Binding Warehouses}" DisplayMemberPath="Name" SelectedValuePath="Id" SelectedValue="{Binding WarehouseId}"/>
                </Grid>
            </Grid>
        </Border>
        <Border Grid.Row="1" Background="White" Margin="10,8,10,4" Padding="8" BorderBrush="#E0E0E0" BorderThickness="1" CornerRadius="4">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/><RowDefinition Height="*"/>
                </Grid.RowDefinitions>
                <Grid Grid.Row="0" Margin="0,0,0,6">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/><ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    <TextBlock Grid.Column="0" Text="&#1576;&#1606;&#1608;&#1583; &#1575;&#1604;&#1601;&#1575;&#1578;&#1608;&#1585;&#1577;" FontSize="15" FontWeight="Bold" Foreground="#37474F" VerticalAlignment="Center"/>
                    <Button Grid.Column="1" Content="+ &#1573;&#1590;&#1575;&#1601;&#1577; &#1589;&#1606;&#1601;" Style="{StaticResource AddBtn}" Command="{Binding AddLineCommand}"/>
                </Grid>
                <DataGrid x:Name="LinesGrid" Grid.Row="1" ItemsSource="{Binding Lines}" AutoGenerateColumns="False" CanUserAddRows="False" HeadersVisibility="Column" GridLinesVisibility="Horizontal" HorizontalGridLinesBrush="#ECEFF1" BorderBrush="#E0E0E0" BorderThickness="1" Background="White" FontSize="13" MinHeight="180">
                    <DataGrid.Columns>
                        <DataGridTemplateColumn Header="&#1589;&#1606;&#1601; (&#1575;&#1603;&#1578;&#1576; &#1604;&#1604;&#1576;&#1581;&#1579;)" Width="320">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <Grid>
                                        <Popup IsOpen="{Binding IsPopupOpen}" Placement="Bottom" PlacementTarget="{Binding ElementName=SearchBoxInner}" StaysOpen="False" AllowsTransparency="True" PopupAnimation="Slide">
                                            <Border Background="White" BorderBrush="#00897B" BorderThickness="1" CornerRadius="2" MaxHeight="260" MinWidth="300">
                                                <ListBox x:Name="SearchList" ItemsSource="{Binding FilteredItems}" SelectedIndex="{Binding SearchSelectedIndex}" SelectionChanged="SearchList_SelectionChanged" BorderThickness="0">
                                                    <ListBox.ItemTemplate>
                                                        <DataTemplate>
                                                            <StackPanel Margin="2">
                                                                <TextBlock Text="{Binding Name}" FontWeight="SemiBold" Foreground="#263238"/>
                                                                <StackPanel Orientation="Horizontal">
                                                                    <TextBlock Text="&#1576;&#1575;&#1585;&#1603;&#1608;&#1583;: " Foreground="#78909C" FontSize="11"/>
                                                                    <TextBlock Text="{Binding Barcode}" Foreground="#00897B" FontSize="11"/>
                                                                    <TextBlock Text="  |  &#1605;&#1582;&#1586;&#1608;&#1606;: " Foreground="#78909C" FontSize="11"/>
                                                                    <TextBlock Text="{Binding CurrentStock}" Foreground="#EF6C00" FontSize="11"/>
                                                                </StackPanel>
                                                            </StackPanel>
                                                        </DataTemplate>
                                                    </ListBox.ItemTemplate>
                                                </ListBox>
                                            </Border>
                                        </Popup>
                                        <TextBox x:Name="SearchBoxInner" Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}" Tag="search" Padding="6,4" BorderBrush="#80CBC4" BorderThickness="1,1,1,2" Background="White" KeyDown="SearchBox_KeyDown"/>
                                    </Grid>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTextColumn Header="&#1576;&#1575;&#1585;&#1603;&#1608;&#1583;" Width="110" Binding="{Binding Barcode}" IsReadOnly="True"/>
                        <DataGridTextColumn Header="&#1575;&#1604;&#1608;&#1581;&#1583;&#1577;" Width="70" Binding="{Binding Unit}" IsReadOnly="True"/>
                        <DataGridTemplateColumn Header="&#1575;&#1604;&#1603;&#1605;&#1610;&#1577;" Width="90">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <TextBox Text="{Binding Quantity, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" Tag="qty" Padding="4,3" BorderBrush="#E0E0E0" KeyDown="Cell_KeyDown"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTemplateColumn Header="&#1575;&#1604;&#1587;&#1593;&#1585;" Width="100">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <TextBox Text="{Binding Price, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" Tag="price" Padding="4,3" BorderBrush="#E0E0E0" KeyDown="Cell_KeyDown"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTemplateColumn Header="&#1582;&#1589;&#1605;" Width="80">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <TextBox Text="{Binding Discount, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" Tag="discount" Padding="4,3" BorderBrush="#E0E0E0" KeyDown="Cell_KeyDown"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTextColumn Header="&#1573;&#1580;&#1605;&#1575;&#1604;&#1610;" Width="110" Binding="{Binding Total, StringFormat=N2}" IsReadOnly="True">
                            <DataGridTextColumn.ElementStyle>
                                <Style TargetType="TextBlock">
                                    <Setter Property="HorizontalAlignment" Value="Right"/>
                                    <Setter Property="FontWeight" Value="SemiBold"/>
                                    <Setter Property="Foreground" Value="#00695C"/>
                                </Style>
                            </DataGridTextColumn.ElementStyle>
                        </DataGridTextColumn>
                        <DataGridTemplateColumn Header="X" Width="60">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <Button Content="X" Background="Transparent" BorderThickness="0" FontSize="14" Cursor="Hand" Command="{Binding DataContext.DeleteLineCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" CommandParameter="{Binding}"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                    </DataGrid.Columns>
                </DataGrid>
            </Grid>
        </Border>
        <Border x:Name="PaymentsSection" Grid.Row="2" Background="White" Margin="10,4,10,4" Padding="10" BorderBrush="#80CBC4" BorderThickness="1" CornerRadius="4">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/><RowDefinition Height="Auto"/>
                </Grid.RowDefinitions>
                <Grid Grid.Row="0" Margin="0,0,0,6">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/><ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    <TextBlock Grid.Column="0" Text="&#1591;&#1585;&#1602; &#1575;&#1604;&#1583;&#1601;&#1593; &#1575;&#1604;&#1605;&#1578;&#1593;&#1583;&#1583;&#1577;" FontSize="15" FontWeight="Bold" Foreground="#00695C" VerticalAlignment="Center"/>
                    <Button Grid.Column="1" Content="+ &#1573;&#1590;&#1575;&#1601;&#1577; &#1583;&#1601;&#1593;&#1577;" Style="{StaticResource AddBtn}" Command="{Binding AddPaymentCommand}"/>
                </Grid>
                <DataGrid x:Name="PaymentsGrid" Grid.Row="1" ItemsSource="{Binding Payments}" AutoGenerateColumns="False" CanUserAddRows="False" HeadersVisibility="Column" GridLinesVisibility="Horizontal" HorizontalGridLinesBrush="#ECEFF1" BorderBrush="#E0E0E0" BorderThickness="1" Background="White" FontSize="13" MaxHeight="140" CellEditEnding="PaymentsGrid_CellEditEnding">
                    <DataGrid.Columns>
                        <DataGridTemplateColumn Header="&#1591;&#1585;&#1610;&#1602;&#1577; &#1575;&#1604;&#1583;&#1601;&#1593;" Width="140">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <ComboBox SelectedValue="{Binding Method, UpdateSourceTrigger=PropertyChanged}" SelectedValuePath="Tag" Padding="4,2">
                                        <ComboBoxItem Content="&#1606;&#1602;&#1583;&#1610;" Tag="cash"/>
                                        <ComboBoxItem Content="&#1576;&#1591;&#1575;&#1602;&#1577;" Tag="card"/>
                                        <ComboBoxItem Content="&#1578;&#1581;&#1608;&#1610;&#1604; &#1576;&#1606;&#1603;&#1610;" Tag="bank"/>
                                        <ComboBoxItem Content="&#1588;&#1610;&#1603;" Tag="check"/>
                                        <ComboBoxItem Content="&#1570;&#1580;&#1604;" Tag="credit"/>
                                    </ComboBox>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTemplateColumn Header="&#1575;&#1604;&#1605;&#1576;&#1604;&#1594;" Width="140">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <TextBox Text="{Binding Amount, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" Padding="4,3" BorderBrush="#E0E0E0" TextAlignment="Right"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTextColumn Header="&#1605;&#1585;&#1580;&#1593;" Width="180" Binding="{Binding Reference, UpdateSourceTrigger=PropertyChanged}"/>
                        <DataGridTemplateColumn Header="&#1578;&#1575;&#1585;&#1610;&#1582; &#1575;&#1604;&#1583;&#1601;&#1593;" Width="150">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <DatePicker SelectedDate="{Binding PaymentDate}" FontSize="12"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTextColumn Header="&#1605;&#1604;&#1575;&#1581;&#1592;&#1575;&#1578;" Width="*" Binding="{Binding Notes, UpdateSourceTrigger=PropertyChanged}"/>
                        <DataGridTemplateColumn Header="X" Width="60">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <Button Content="X" Background="Transparent" BorderThickness="0" FontSize="14" Cursor="Hand" Command="{Binding DataContext.DeletePaymentCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" CommandParameter="{Binding}"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                    </DataGrid.Columns>
                </DataGrid>
            </Grid>
        </Border>
        <Border Grid.Row="3" Background="White" Padding="16,10" BorderBrush="#E0E0E0" BorderThickness="0,1,0,0">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/><ColumnDefinition Width="300"/>
                </Grid.ColumnDefinitions>
                <StackPanel Grid.Column="0" VerticalAlignment="Center">
                    <TextBlock x:Name="StatusText" Text="{Binding StatusMessage}" FontSize="12" Foreground="#00897B" TextWrapping="Wrap"/>
                </StackPanel>
                <Grid Grid.Column="1">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/><ColumnDefinition Width="110"/>
                    </Grid.ColumnDefinitions>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/>
                    </Grid.RowDefinitions>
                    <TextBlock Grid.Row="0" Grid.Column="0" Text="&#1575;&#1604;&#1605;&#1580;&#1605;&#1608;&#1593;:" FontSize="13" Foreground="#546E7A" Padding="0,2"/>
                    <TextBlock Grid.Row="0" Grid.Column="1" Text="{Binding SubtotalDisplay}" FontSize="13" FontWeight="SemiBold" Foreground="#37474F" TextAlignment="Left" Padding="0,2"/>
                    <StackPanel Grid.Row="1" Grid.Column="0" Orientation="Horizontal">
                        <TextBlock Text="&#1582;&#1589;&#1605;:" FontSize="13" Foreground="#546E7A" VerticalAlignment="Center"/>
                        <TextBox Text="{Binding DiscountAmount, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" Width="80" Margin="6,0,0,0" Padding="3,2" FontSize="12" BorderBrush="#E0E0E0" TextAlignment="Right"/>
                    </StackPanel>
                    <TextBlock Grid.Row="1" Grid.Column="1" Text="{Binding DiscountAmount, StringFormat=N2}" FontSize="13" Foreground="#E53935" TextAlignment="Left"/>
                    <TextBlock Grid.Row="2" Grid.Column="0" Text="&#1573;&#1580;&#1605;&#1575;&#1604;&#1610;:" FontSize="14" FontWeight="Bold" Foreground="#00695C" Padding="0,2"/>
                    <TextBlock Grid.Row="2" Grid.Column="1" Text="{Binding TotalDisplay}" FontSize="15" FontWeight="Bold" Foreground="#00695C" TextAlignment="Left" Padding="0,2"/>
                    <TextBlock Grid.Row="3" Grid.Column="0" Text="&#1575;&#1604;&#1605;&#1583;&#1601;&#1608;&#1593;:" FontSize="13" Foreground="#2E7D32" Padding="0,2"/>
                    <TextBlock Grid.Row="3" Grid.Column="1" Text="{Binding TotalPaidDisplay}" FontSize="13" FontWeight="SemiBold" Foreground="#2E7D32" TextAlignment="Left" Padding="0,2"/>
                    <TextBlock Grid.Row="4" Grid.Column="0" Text="&#1575;&#1604;&#1605;&#1578;&#1576;&#1602;&#1610;:" FontSize="14" FontWeight="Bold" Foreground="#E65100" Padding="0,2"/>
                    <TextBlock Grid.Row="4" Grid.Column="1" Text="{Binding RemainingDisplay}" FontSize="14" FontWeight="Bold" Foreground="#E65100" TextAlignment="Left" Padding="0,2"/>
                </Grid>
            </Grid>
        </Border>
    </Grid>
</Window>
'''

with open(xaml_path, "w", encoding="utf-8-sig") as f:
    f.write(XAML)
print(f"  OK: {xaml_path}")
# 3) كتابة InvoiceEditWindow.xaml.cs
print("\n[3/4] Writing InvoiceEditWindow.xaml.cs...")
cs_path = os.path.join(VIEWS, "InvoiceEditWindow.xaml.cs")

CS = '''using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    public partial class InvoiceEditWindow : Window
    {
        private readonly InvoiceEditViewModel _vm;
        private readonly int? _editId;

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
            await _vm.LoadAsync();
            if (_editId.HasValue) await _vm.LoadFromInvoiceAsync(_editId.Value);
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox box) return;
            if (box.DataContext is not InvoiceLineItem line) return;
            switch (e.Key)
            {
                case Key.Down:
                    if (line.SearchSelectedIndex < line.FilteredItems.Count - 1) line.SearchSelectedIndex++;
                    e.Handled = true; break;
                case Key.Up:
                    if (line.SearchSelectedIndex > 0) line.SearchSelectedIndex--;
                    e.Handled = true; break;
                case Key.Enter:
                    if (line.IsPopupOpen && line.FilteredItems.Count > 0)
                    {
                        if (line.SearchSelectedIndex < 0 || line.SearchSelectedIndex >= line.FilteredItems.Count) line.SearchSelectedIndex = 0;
                        line.SelectByIndex();
                    }
                    FocusQuantity(line); e.Handled = true; break;
                case Key.Escape:
                    line.IsPopupOpen = false; e.Handled = true; break;
            }
        }

        private void SearchList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox lb && lb.DataContext is InvoiceLineItem line && lb.SelectedItem is ItemSearchResult it)
            {
                line.SelectItem(it); FocusQuantity(line);
            }
        }

        private void FocusQuantity(InvoiceLineItem line)
        {
            try
            {
                if (LinesGrid.ItemContainerGenerator.ContainerFromItem(line) is DataGridRow row)
                {
                    var tb = FindDescendant<TextBox>(row, x => (x.Tag as string) == "qty");
                    tb?.Focus(); tb?.SelectAll();
                }
            }
            catch { }
        }

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
                    var next = FindDescendant<TextBox>(newRow, x => (x.Tag as string) == "search");
                    next?.Focus();
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
'''

with open(cs_path, "w", encoding="utf-8-sig") as f:
    f.write(CS)
print(f"  OK: {cs_path}")

# 4) تنظيف ملفات obj
print("\n[4/4] Cleaning obj folder...")
obj_dir = os.path.join(UI_DIR, "obj", "Debug", "net10.0-windows")
if os.path.isdir(obj_dir):
    for fname in os.listdir(obj_dir):
        if "InvoiceEditWindow" in fname:
            try:
                fp = os.path.join(obj_dir, fname)
                if os.path.isfile(fp):
                    os.remove(fp)
                    print(f"  Removed: {fname}")
            except: pass

print("\n" + "=" * 60)
print("  DONE! Now run:")
print("  dotnet build Techtoon.UI")
print("  dotnet run --project Techtoon.UI")
print("=" * 60)