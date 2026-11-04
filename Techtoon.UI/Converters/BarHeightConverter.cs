using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Techtoon.UI.Converters
{
    /// <summary>
    /// يحسب ارتفاع شريط الرسم البياني. يستخدم ارتفاعًا ثابتًا (180px) بدلاً من ActualHeight
    /// لتفادي مشاكل layout الدائرية.
    /// القيم: [0] = القيمة الحالية, [1] = أقصى قيمة
    /// </summary>
    public class BarHeightConverter : IMultiValueConverter
    {
        // ارتفاع منطقة الرسم الثابت (بدون التسميات)
        private const double ChartUsableHeight = 160.0;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2) return 10.0;
            try
            {
                double current = 0;
                double max = 1;

                if (values[0] != null && values[0] != DependencyProperty.UnsetValue)
                    current = System.Convert.ToDouble(values[0]);
                if (values[1] != null && values[1] != DependencyProperty.UnsetValue)
                    max = System.Convert.ToDouble(values[1]);

                // معالجة قيم غير صالحة
                if (double.IsNaN(current) || double.IsInfinity(current)) current = 0;
                if (double.IsNaN(max) || double.IsInfinity(max) || max <= 0) max = 1;
                if (current < 0) current = 0;

                double ratio = current / max;
                double height = ratio * ChartUsableHeight;

                // ضمان حد أدنى وأقصى
                if (height < 4) height = 4;
                if (height > ChartUsableHeight) height = ChartUsableHeight;

                return height;
            }
            catch
            {
                return 10.0;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}