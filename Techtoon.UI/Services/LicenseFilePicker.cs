using System;

namespace Techtoon.UI.Services
{
    /// <summary>
    /// اختيار ملف الترخيص من القرص.
    /// واجهة قابلة للاستبدال (اختبارات آلية / مسار استيراد مباشر).
    /// </summary>
    public interface ILicenseFilePicker
    {
        /// <summary>يفتح نافذة اختيار الملف ويعيد المسار المختار، أو null عند الإلغاء.</summary>
        string? Pick();
    }

    public class LicenseFilePicker : ILicenseFilePicker
    {
        public string? Pick()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "اختر ملف الترخيص",
                Filter = "License files (*.key;*.lic;*.json)|*.key;*.lic;*.json|All files (*.*)|*.*",
                DefaultExt = ".key",
                CheckFileExists = true
            };
            return dlg.ShowDialog() == true ? dlg.FileName : null;
        }
    }

    /// <summary>محدد ملفات ثابت — يسمح للاختبارات بتوجيه مسار ملف معروف.</summary>
    public class FixedLicenseFilePicker : ILicenseFilePicker
    {
        private readonly string? _path;
        public FixedLicenseFilePicker(string? path) { _path = path; }
        public string? Pick() => _path;
    }

    /// <summary>مصدر المحدد المستخدم حاليًا (قابل للاستبدال أثناء الاختبار).</summary>
    public static class LicenseFileDialog
    {
        private static ILicenseFilePicker _picker = new LicenseFilePicker();

        public static ILicenseFilePicker Picker => _picker;

        /// <summary>يستبدل المحدد مؤقتًا (للاختبار) — يعيد حارسًا لاستعادة المحدد الأصلي.</summary>
        public static IDisposable Use(ILicenseFilePicker picker)
        {
            var previous = _picker;
            _picker = picker;
            return new RestoreScope(() => _picker = previous);
        }

        public static string? PickLicenseFile() => _picker.Pick();

        private sealed class RestoreScope : IDisposable
        {
            private readonly Action _restore;
            private bool _done;
            public RestoreScope(Action restore) { _restore = restore; }
            public void Dispose()
            {
                if (_done) return;
                _done = true;
                _restore();
            }
        }
    }
}
