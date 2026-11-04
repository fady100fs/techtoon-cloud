using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Techtoon.UI.Services;

namespace Techtoon.UI.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public ICommand BackupCommand { get; }
        public ICommand RestoreCommand { get; }

        public SettingsViewModel()
        {
            BackupCommand = new RelayCommand(CreateBackup);
            RestoreCommand = new RelayCommand(RestoreBackup);
        }

        private void CreateBackup()
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "SQL backup (*.sql)|*.sql",
                    DefaultExt = ".sql",
                    FileName = "techtoon_backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss")
                };
                if (dialog.ShowDialog() != true) return;

                string filePath = BackupService.CreateBackup(System.IO.Path.GetDirectoryName(dialog.FileName));
                StatusMessage = "\u062A\u0645 \u0627\u0644\u0646\u0633\u062E \u0627\u0644\u0627\u062D\u062A\u064A\u0627\u0637\u064A: " + System.IO.Path.GetFileName(filePath);
                System.Windows.MessageBox.Show("\u062A\u0645 \u0627\u0644\u0646\u0633\u062E \u0627\u0644\u0627\u062D\u062A\u064A\u0627\u0637\u064A \u0628\u0646\u062C\u0627\u062D\n" + filePath, "\u0646\u0633\u062E \u0627\u062D\u062A\u064A\u0627\u0637\u064A", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = "\u062E\u0637\u0623: " + ex.Message;
                System.Windows.MessageBox.Show("\u062E\u0637\u0623: " + ex.Message, "\u062E\u0637\u0623", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void RestoreBackup()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "SQL backup (*.sql)|*.sql" };
                if (dialog.ShowDialog() != true) return;

                if (!BackupService.VerifyBackup(dialog.FileName))
                {
                    System.Windows.MessageBox.Show("\u0627\u0644\u0645\u0644\u0641 \u063A\u064A\u0631 \u0635\u0627\u0644\u062D", "\u062A\u062D\u0642\u0642", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                var result = System.Windows.MessageBox.Show(
                    "\u062A\u062D\u0630\u064A\u0631: \u0627\u0633\u062A\u0639\u0627\u062F\u0629 \u0627\u0644\u0646\u0633\u062E\u0629 \u0633\u062A\u0643\u062A\u0628 \u0639\u0644\u0649 \u0627\u0644\u0628\u064A\u0627\u0646\u0627\u062A \u0627\u0644\u062D\u0627\u0644\u064A\u0629.\n\n\u0647\u0644 \u062A\u0631\u064A\u062F \u0627\u0644\u0645\u062A\u0627\u0628\u0639\u0629\u061F",
                    "\u062A\u0623\u0643\u064A\u062F \u0627\u0644\u0627\u0633\u062A\u0639\u0627\u062F\u0629",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (result != System.Windows.MessageBoxResult.Yes) return;

                bool success = BackupService.RestoreBackup(dialog.FileName, "techtoon_test");
                StatusMessage = success ? "\u062A\u0645 \u0627\u0644\u0627\u0633\u062A\u0639\u0627\u062F\u0629" : "\u0641\u0634\u0644 \u0627\u0644\u0627\u0633\u062A\u0639\u0627\u062F\u0629";
            }
            catch (Exception ex) { StatusMessage = "\u062E\u0637\u0623: " + ex.Message; }
        }
    }
}
