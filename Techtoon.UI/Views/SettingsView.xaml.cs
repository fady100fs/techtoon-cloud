using System.Windows.Controls;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            try { LblTitle.Text = "الإعدادات"; } catch { }
            try { LblBackup.Text = "النسخ الاحتياطي"; } catch { }
            try { BtnBackup.Content = "💾 إنشاء نسخة احتياطية"; } catch { }
            try { BtnRestore.Content = "🔄 استعادة نسخة"; } catch { }
            try { LblLicenseHeader.Text = "تفعيل البرنامج"; } catch { }
            try { LblLicenseHint.Text = "اختر ملف الترخيص (.key / .lic) الذي أرسله لك المورد. يُتحقق من التوقيع الرقمي قبل الحفظ — ملف غير صالح لن يستبدل ترخيصًا ساريًا."; } catch { }
            try { BtnImportLicense.Content = "📄 استيراد ملف الترخيص"; } catch { }
            try { BtnRefreshLicense.Content = "🔄 تحديث حالة الترخيص"; } catch { }
            if (DataContext == null) DataContext = new SettingsViewModel();
        }
    }
}
