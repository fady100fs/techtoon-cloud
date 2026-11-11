using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Techtoon.Core.Licensing;
using Techtoon.UI.Services;

namespace Techtoon.UI.Views
{
    /// <summary>P5: نافذة تفعيل البرنامج — إدخال مفتاح الترخيص وعرض معلومات الخادم والجهاز.</summary>
    public class LicenseActivationWindow : Window
    {
        private readonly TextBox _txtMachineId = new() { Margin = new Thickness(4), IsReadOnly = true };
        private readonly TextBox _txtLicenseKey = new() { Margin = new Thickness(4), Height = 60, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };
        private readonly TextBlock _lblStatus = new() { Foreground = System.Windows.Media.Brushes.Red, TextWrapping = TextWrapping.Wrap };
        private readonly TextBlock _lblInfo = new() { Foreground = System.Windows.Media.Brushes.DarkSlateGray, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(5) };

        public LicenseActivationWindow()
        {
            Title = "تفعيل البرنامج - Techtoon Accounting";
            Width = 560; Height = 520; WindowStartupLocation = WindowStartupLocation.CenterScreen;
            FlowDirection = FlowDirection.RightToLeft;

            var grid = new Grid { Margin = new Thickness(12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int i = 0; i < 8; i++) grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            Row(grid, 0, "المعرّف الفريد للجهاز (Machine ID):");
            var idPanel = new StackPanel { Orientation = Orientation.Horizontal };
            idPanel.Children.Add(_txtMachineId);
            var btnCopy = new Button { Content = "نسخ", Padding = new Thickness(10, 4, 10, 4), Margin = new Thickness(5, 0, 0, 0) };
            btnCopy.Click += BtnCopy_Click;
            idPanel.Children.Add(btnCopy);
            grid.Children.Add(_txtMachineId); Grid.SetRow(_txtMachineId, 0); Grid.SetColumn(_txtMachineId, 1);
            Grid.SetRow(idPanel, 0); grid.Children.Add(idPanel); Grid.SetColumn(idPanel, 1);

            Row(grid, 1, "مفتاح الترخيص:");
            grid.Children.Add(_txtLicenseKey); Grid.SetRow(_txtLicenseKey, 1); Grid.SetColumn(_txtLicenseKey, 1);

            var btnSave = new Button { Content = "تفعيل", Padding = new Thickness(20, 6, 20, 6), Margin = new Thickness(0, 10, 0, 0) };
            btnSave.Click += Save_Click;
            grid.Children.Add(btnSave); Grid.SetRow(btnSave, 2); Grid.SetColumn(btnSave, 1);

            Grid.SetRow(_lblStatus, 3); grid.Children.Add(_lblStatus); Grid.SetColumn(_lblStatus, 1);
            Grid.SetRow(_lblInfo, 4); grid.Children.Add(_lblInfo); Grid.SetColumn(_lblInfo, 1);

            Content = grid;

            LoadStatus();
        }

        private static void Row(Grid g, int i, string label)
        {
            g.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold });
            Grid.SetRow(g.Children[g.Children.Count - 1], i); Grid.SetColumn(g.Children[g.Children.Count - 1], 0);
        }

        private void LoadStatus()
        {
            try
            {
                var info = LicenseService.GetStatus();
                _txtMachineId.Text = info.MachineId;

                if (info.IsAllowed)
                {
                    _lblStatus.Text = "البرنامج مرخّص";
                    _lblInfo.Text = $"{LicenseTypes.Arabic(info.LicenseType)} - متبقي {info.DaysLeft} يوم";
                }
                else
                {
                    _lblStatus.Text = info.Message;
                    _lblInfo.Text = $"المعرّف: {info.MachineId}";
                }
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "خطأ في التحميل: " + ex.Message;
            }
        }

        private void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Clipboard.SetText(_txtMachineId.Text);
            _lblStatus.Text = "تم نسخ معرف الجهاز إلى الحافظة.";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtLicenseKey.Text))
            {
                _lblStatus.Text = "يرجى إدخال مفتاح الترخيص.";
                return;
            }

            try
            {
                var path = LicenseService.GetLicensePath();
                File.WriteAllText(path, _txtLicenseKey.Text.Trim(), new UTF8Encoding(false));

                var info = LicenseService.GetStatus();
                if (info.IsAllowed)
                {
                    _lblStatus.Text = "تم التفعيل بنجاح!";
                    _lblInfo.Text = $"{LicenseTypes.Arabic(info.LicenseType)} - متبقي {info.DaysLeft} يوم";
                    MessageBox.Show("تم تفعيل البرنامج بنجاح!", "تفعيل البرنامج", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    _lblStatus.Text = "فشل التفعيل: " + info.Message;
                }
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "خطأ: " + ex.Message;
            }
        }

        public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();
    }
}
