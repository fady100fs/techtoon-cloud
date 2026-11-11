using System.Windows;

namespace Techtoon.UI.Views
{
    /// <summary>
    /// نافذة إدخال نص (بديل WPF عن Microsoft.VisualBasic.Interaction.InputBox
    /// الذي يتطلب System.Windows.Forms ولا يعمل داخل تطبيق WPF — كان يُسقط
    /// الصفحة بخطأ PlatformNotSupportedException عند الضغط).
    /// </summary>
    public partial class InputDialog : Window
    {
        private InputDialog()
        {
            InitializeComponent();
        }

        /// <summary>النص المُدخَل (يقرأ من الحقل المناسب: نص أو كلمة مرور).</summary>
        public string InputText => TxtPassword.Visibility == Visibility.Visible ? TxtPassword.Password : TxtInput.Text;

        /// <summary>
        /// يعرض نافذة إدخال ويعيد النص المُدخَل، أو null عند الإلغاء/الإغلاق.
        /// </summary>
        public static string? Show(Window? owner, string prompt, string title,
            string defaultValue = "", bool isPassword = false)
        {
            var dlg = new InputDialog { Title = title ?? "إدخال" };
            dlg.LblPrompt.Text = prompt ?? "";
            if (isPassword)
            {
                dlg.TxtInput.Visibility = Visibility.Collapsed;
                dlg.TxtPassword.Visibility = Visibility.Visible;
                dlg.TxtPassword.Password = defaultValue ?? "";
            }
            else
            {
                dlg.TxtInput.Text = defaultValue ?? "";
            }

            dlg.Owner = owner ?? Application.Current?.MainWindow;
            dlg.WindowStartupLocation = (dlg.Owner != null && dlg.Owner.IsVisible)
                ? WindowStartupLocation.CenterOwner
                : WindowStartupLocation.CenterScreen;

            dlg.Loaded += (_, _) =>
            {
                if (isPassword) { dlg.TxtPassword.Focus(); dlg.TxtPassword.SelectAll(); }
                else { dlg.TxtInput.Focus(); dlg.TxtInput.SelectAll(); }
            };

            return dlg.ShowDialog() == true ? dlg.InputText : null;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
