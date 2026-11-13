namespace Techtoon.UI.ViewModels
{
    /// <summary>
    /// صفحة «الاعتراف بالإيراد».
    /// <para>الصفحة تعمل بالربط المباشر من code-behind (Elements + خدمات)، وهذا الـViewModel
    /// هو مفتاح التنقّل الذي يربطه MainWindow بالصفحة عبر DataTemplate — ويوفّر حالتها العامة.</para>
    /// </summary>
    public class RevenueRecognitionViewModel : ViewModelBase
    {
        private string _statusMessage = "";
        /// <summary>رسالة الحالة المعروضة أسفل الصفحة.</summary>
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        /// <summary>عنوان الصفحة.</summary>
        public string Title => "الاعتراف بالإيراد";

        /// <summary>عدد الفواتير المعرَّفة (يُحدَّث بعد تنفيذ العملية).</summary>
        private int _recognizedCount;
        public int RecognizedCount { get => _recognizedCount; set => SetProperty(ref _recognizedCount, value); }
    }

    /// <summary>
    /// صفحة «استيراد كشوف البنك».
    /// <para>نفس الملاحظة: الصفحة مرتبطة مباشرة من code-behind، وهذا الـViewModel مفتاح التنقّل.</para>
    /// </summary>
    public class BankStatementImportViewModel : ViewModelBase
    {
        private string _statusMessage = "";
        /// <summary>رسالة الحالة (نتيجة الاستيراد / المطابقة).</summary>
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        /// <summary>عنوان الصفحة.</summary>
        public string Title => "استيراد كشوف البنك";

        /// <summary>عدد السطور المستوردة.</summary>
        private int _importedLines;
        public int ImportedLines { get => _importedLines; set => SetProperty(ref _importedLines, value); }

        /// <summary>عدد السطور التي طابقت مدفوعات موجودة.</summary>
        private int _matchedLines;
        public int MatchedLines { get => _matchedLines; set => SetProperty(ref _matchedLines, value); }
    }
}
