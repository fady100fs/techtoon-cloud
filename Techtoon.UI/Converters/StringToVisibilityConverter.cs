using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Techtoon.UI.Converters
{
    public sealed class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool has = !(string.IsNullOrWhiteSpace(value as string));
            string p = parameter as string;
            if (string.Equals(p, "Invert", StringComparison.OrdinalIgnoreCase)) { has = !has; }
            return has ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}