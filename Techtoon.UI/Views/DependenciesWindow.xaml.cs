using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Techtoon.UI.Views;

public class DependencyItem
{
    public string Type { get; set; }
    public string Display { get; set; }
    public string TargetWindow { get; set; }
    public int TargetId { get; set; }
}

public partial class DependenciesWindow : Window
{
    public DependencyItem? SelectedItem { get; private set; }

    public DependenciesWindow(List<DependencyItem> items)
    {
        InitializeComponent();
        LstDeps.ItemsSource = items;
    }

    private void BtnOpenDep_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is DependencyItem dep)
        {
            SelectedItem = dep;
            if (string.IsNullOrEmpty(dep.TargetWindow))
            {
                System.Windows.MessageBox.Show("This record exists but no edit window is available for it yet.", "Info", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }
            DialogResult = true;
            Close();
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}