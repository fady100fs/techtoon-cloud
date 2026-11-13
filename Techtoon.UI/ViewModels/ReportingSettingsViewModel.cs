namespace Techtoon.UI.ViewModels
{
    /// <summary>
    /// صفحة «إعدادات التقارير».
    /// <para>الصفحة تعمل بالربط المباشر من code-behind (تخزين الإعدادات في reporting_settings)،
    /// وهذا الـViewModel هو مفتاح التنقّل الذي يربطه MainWindow بالصفحة عبر DataTemplate.</para>
    /// </summary>
    public class ReportingSettingsViewModel : ViewModelBase
    {
        private string _statusMessage = "";
        /// <summary>رسالة الحالة (حُفظت الإعدادات أم لا).</summary>
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        /// <summary>عنوان الصفحة.</summary>
        public string Title => "إعدادات التقارير";

        /// <summary>هل المقارنة مع الفترة السابقة مفعّلة؟</summary>
        private bool _enableComparison = true;
        public bool EnableComparison { get => _enableComparison; set => SetProperty(ref _enableComparison, value); }

        /// <summary>عدد المقارنة (0 = مقارنة بالسنة السابقة).</summary>
        private int _comparisonPeriods = 1;
        public int ComparisonPeriods { get => _comparisonPeriods; set => SetProperty(ref _comparisonPeriods, value); }
    }
}
