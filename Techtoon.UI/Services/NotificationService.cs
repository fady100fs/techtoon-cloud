using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Techtoon.UI.Services
{
    public static class NotificationService
    {
        private static Window _popup;
        private static TextBlock _textBlock;
        private static DispatcherTimer _timer;

        public static void Show(string message, NotificationType type = NotificationType.Info, int durationSeconds = 3)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                // إذا كان هناك إشعار معروض، أغلقه أولًا
                _popup?.Close();

                _popup = new Window
                {
                    Width = 350,
                    Height = 60,
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = new SolidColorBrush(Colors.Transparent),
                    Topmost = true,
                    ShowInTaskbar = false,
                    ResizeMode = ResizeMode.NoResize
                };

                var border = new Border
                {
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(15),
                    Margin = new Thickness(10),
                    Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 15, Opacity = 0.3, ShadowDepth = 2 }
                };

                switch (type)
                {
                    case NotificationType.Success: border.Background = new SolidColorBrush(Color.FromRgb(46, 125, 50)); break;
                    case NotificationType.Error: border.Background = new SolidColorBrush(Color.FromRgb(198, 40, 40)); break;
                    case NotificationType.Warning: border.Background = new SolidColorBrush(Color.FromRgb(245, 124, 0)); break;
                    default: border.Background = new SolidColorBrush(Color.FromRgb(21, 101, 192)); break;
                }

                _textBlock = new TextBlock
                {
                    Text = message,
                    Foreground = Brushes.White,
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap
                };
                border.Child = _textBlock;
                _popup.Content = border;

                // الموضع: أسفل اليمين
                var workArea = SystemParameters.WorkArea;
                _popup.Left = workArea.Right - _popup.Width - 20;
                _popup.Top = workArea.Bottom - _popup.Height - 20;

                _popup.Show();

                // أنيميشن الظهور
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3));
                _popup.BeginAnimation(UIElement.OpacityProperty, fadeIn);

                // مؤقت الإغلاق
                _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(durationSeconds) };
                _timer.Tick += (s, e) =>
                {
                    _timer.Stop();
                    var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.5));
                    fadeOut.Completed += (sender, args) => _popup?.Close();
                    _popup.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                };
                _timer.Start();
            });
        }
    }

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }
}