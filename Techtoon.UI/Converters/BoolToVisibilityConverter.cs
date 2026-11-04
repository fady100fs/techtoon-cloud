using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Techtoon.UI.Converters
{
    public sealed class BoolToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = false;
            if (value is bool) { b = (bool)value; }
            else
            {
                bool? nb = value as bool?;
                if (nb.HasValue) { b = nb.Value; }
                else
                {
                    string sb = value as string;
                    if (sb != null) { bool.TryParse(sb, out b); }
                }
            }

            string p = parameter as string;
            bool inv = Invert || string.Equals(p, "Invert", StringComparison.OrdinalIgnoreCase);
            if (inv) { b = !b; }

            Visibility whenFalse = Visibility.Collapsed;
            if (string.Equals(p, "Hidden", StringComparison.OrdinalIgnoreCase)) { whenFalse = Visibility.Hidden; }

            return b ? Visibility.Visible : whenFalse;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility v = value is Visibility ? (Visibility)value : Visibility.Collapsed;
            bool b = (v == Visibility.Visible);
            string p = parameter as string;
            bool inv = Invert || string.Equals(p, "Invert", StringComparison.OrdinalIgnoreCase);
            if (inv) { b = !b; }
            return b;
        }
    }
}