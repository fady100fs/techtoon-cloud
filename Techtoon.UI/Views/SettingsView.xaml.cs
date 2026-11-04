using System.Windows.Controls;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            try { LblTitle.Text = "\u0627\u0644\u0625\u0639\u062F\u0627\u062F\u0627\u062A"; } catch { }
            try { LblBackup.Text = "\u0627\u0644\u0646\u0633\u062E \u0627\u0644\u0627\u062D\u062A\u064A\u0627\u0637\u064A"; } catch { }
            try { BtnBackup.Content = "\uD83D\uDCBE \u0625\u0646\u0634\u0627\u0621 \u0646\u0633\u062E\u0629 \u0627\u062D\u062A\u064A\u0627\u0637\u064A\u0629"; } catch { }
            try { BtnRestore.Content = "\uD83D\uDD04 \u0627\u0633\u062A\u0639\u0627\u062F\u0629 \u0646\u0633\u062E\u0629"; } catch { }
            DataContext = new SettingsViewModel();
        }
    }
}
