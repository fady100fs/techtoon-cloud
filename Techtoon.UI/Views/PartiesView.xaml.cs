using System;
using System.Windows;
using System.Windows.Controls;

namespace Techtoon.UI.Views
{
    public partial class PartiesView : UserControl
    {
        public PartiesView()
        {
            InitializeComponent();
        }

        private void PartiesView_Loaded(object sender, RoutedEventArgs e)
        {
            try { if (DataContext is ViewModels.PartiesViewModel vm) { vm.RefreshCommand?.Execute(null); } } catch { }
        }

        private void Statement_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var win = new PartyStatementWindow();
                win.Owner = Window.GetWindow(this);
                win.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }
    }
}