using System;
using System.Globalization;
using System.Windows.Data;

namespace Techtoon.UI.Converters
{
    public class ConverterStub : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) { return value; }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { return value; }
    }

}
