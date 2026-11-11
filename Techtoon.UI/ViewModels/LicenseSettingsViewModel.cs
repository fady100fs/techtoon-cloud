using System;
using System.IO;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Techtoon.Core.Licensing;
using Techtoon.UI.Services;

namespace Techtoon.UI.ViewModels
{
    /// <summary>
    /// نموذج عرض تبويب «تفعيل البرنامج» داخل الإعدادات.
    /// يستورد ملف ترخيص (*.key / *.lic) ويتحقق منه قبل الحفظ — ترخيص غير صالح لا يستبدل ترخيصًا صالحًا.
    /// </summary>
    public class LicenseSettingsViewModel : ViewModelBase
    {
        private readonly IDialogService _dialog;

        private string _statusMessage = "";
        private string _licenseStateText = "";
        private string _licenseTypeText = "";
        private string _licenseOwnerText = "";
        private string _licenseExpiryText = "";
        private string _licensePathText = "";
        private bool _isLicensed;
        private string _importedFileName = "";

        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
        public string LicenseStateText { get => _licenseStateText; set => SetProperty(ref _licenseStateText, value); }
        public string LicenseTypeText { get => _licenseTypeText; set => SetProperty(ref _licenseTypeText, value); }
        public string LicenseOwnerText { get => _licenseOwnerText; set => SetProperty(ref _licenseOwnerText, value); }
        public string LicenseExpiryText { get => _licenseExpiryText; set => SetProperty(ref _licenseExpiryText, value); }
        public string LicensePathText { get => _licensePathText; set => SetProperty(ref _licensePathText, value); }
        public bool IsLicensed { get => _isLicensed; set => SetProperty(ref _isLicensed, value); }
        public string ImportedFileName { get => _importedFileName; set => SetProperty(ref _importedFileName, value); }

        public ICommand ImportLicenseFileCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand OpenLicenseFolderCommand { get; }

        public LicenseSettingsViewModel(IDialogService? dialog = null)
        {
            _dialog = dialog ?? new WpfDialogService();
            ImportLicenseFileCommand = new RelayCommand(ImportLicenseFile);
            RefreshCommand = new RelayCommand(LoadStatus);
            OpenLicenseFolderCommand = new RelayCommand(OpenLicenseFolder);
            LoadStatus();
        }

        public void LoadStatus()
        {
            try
            {
                var info = LicenseService.GetStatus();
                IsLicensed = info.IsAllowed;
                LicenseStateText = info.StateDisplay;
                LicenseTypeText = LicenseTypes.Arabic(info.LicenseType);
                LicenseOwnerText = string.IsNullOrWhiteSpace(info.IssuedTo) ? "—" : info.IssuedTo;
                LicenseExpiryText = info.ExpiresAt.HasValue
                    ? info.ExpiresAt.Value.ToString("yyyy-MM-dd")
                    : (info.LicenseType == LicenseTypes.Lifetime ? "مدى الحياة" : "—");
                LicensePathText = LicenseService.GetLicensePath();
                StatusMessage = info.Message;
            }
            catch (Exception ex)
            {
                StatusMessage = "خطأ في قراءة حالة الترخيص: " + ex.Message;
            }
        }

        private void ImportLicenseFile()
        {
            string? path = LicenseFileDialog.PickLicenseFile();
            if (path == null) { StatusMessage = "لم يتم اختيار ملف."; return; }

            var result = LicenseService.ValidateAndImportFile(path);
            ImportedFileName = Path.GetFileName(path);
            var actionMessage = result.Success
                ? "تم تفعيل البرنامج بنجاح: " + result.Info.Summary
                : "لم يتم التفعيل: " + result.Message;

            if (result.Success)
            {
                _dialog.ShowInfo(
                    "تم تفعيل البرنامج بنجاح.\n\n" + result.Info.Summary,
                    "تفعيل البرنامج");
            }
            else
            {
                _dialog.ShowWarning(
                    "لم يتم التفعيل:\n" + result.Message,
                    "تفعيل البرنامج");
            }

            // نحدّث حالة العرض ثم نُبقي رسالة العملية (أهم للمستخدم من حالة الملف)
            LoadStatus();
            StatusMessage = actionMessage;
        }

        private void OpenLicenseFolder()
        {
            try
            {
                Directory.CreateDirectory(LicenseService.UserDataDirectory);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = LicenseService.UserDataDirectory,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                StatusMessage = "تعذّر فتح المجلد: " + ex.Message;
            }
        }
    }
}
