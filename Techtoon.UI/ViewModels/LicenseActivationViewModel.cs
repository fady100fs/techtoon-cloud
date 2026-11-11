using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Npgsql;
using Techtoon.Core.Licensing;
using Techtoon.UI.Services;

namespace Techtoon.UI.ViewModels
{
    /// <summary>P5: نموذج عرض إعدادات الترخيص.</summary>
    public class LicenseActivationViewModel : ViewModelBase
    {
        private string _licenseKey = "";
        private string _statusMessage = "";
        private string _licenseInfo = "";
        private bool _isLicensed;
        private string _machineId = "";

        public string MachineId => _machineId;
        public string LicenseKey
        {
            get => _licenseKey;
            set => SetProperty(ref _licenseKey, value);
        }
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }
        public string LicenseInfo
        {
            get => _licenseInfo;
            set => SetProperty(ref _licenseInfo, value);
        }
        public bool IsLicensed
        {
            get => _isLicensed;
            set => SetProperty(ref _isLicensed, value);
        }

        public ICommand ActivateCommand { get; }
        public ICommand CopyMachineIdCommand { get; }
        public ICommand ImportLicenseFileCommand { get; }

        public LicenseActivationViewModel()
        {
            ActivateCommand = new UiRelayCommand(_ => Activate());
            CopyMachineIdCommand = new UiRelayCommand(_ => CopyMachineId());
            ImportLicenseFileCommand = new UiRelayCommand(_ => ImportLicenseFile());
            LoadStatus();
        }

        private void LoadStatus()
        {
            try
            {
                var info = LicenseService.GetStatus();
                _machineId = info.MachineId;
                OnPropertyChanged(nameof(MachineId));

                if (info.IsAllowed)
                {
                    _isLicensed = true;
                    StatusMessage = "البرنامج مرخّص";
                    _licenseInfo = $"{LicenseTypes.Arabic(info.LicenseType)} - متبقي {info.DaysLeft} يوم";
                }
                else
                {
                    _isLicensed = false;
                    StatusMessage = info.Message;
                    _licenseInfo = $"المعرّف: {info.MachineId}";
                }
                OnPropertyChanged(nameof(_isLicensed));
                OnPropertyChanged(nameof(_licenseInfo));
            }
            catch (Exception ex)
            {
                StatusMessage = "خطأ في التحميل: " + ex.Message;
            }
        }

        private void Activate()
        {
            if (string.IsNullOrWhiteSpace(_licenseKey))
            {
                StatusMessage = "يرجى إدخال مفتاح الترخيص.";
                return;
            }

            try
            {
                var path = LicenseService.GetLicensePath();
                File.WriteAllText(path, _licenseKey.Trim(), new UTF8Encoding(false));

                var info = LicenseService.GetStatus();
                if (info.IsAllowed)
                {
                    _isLicensed = true;
                    _licenseInfo = $"{LicenseTypes.Arabic(info.LicenseType)} - متبقي {info.DaysLeft} يوم";
                    StatusMessage = "تم التفعيل بنجاح!";
                    OnPropertyChanged(nameof(_isLicensed));
                    OnPropertyChanged(nameof(_licenseInfo));
                    MessageBox.Show("تم تفعيل البرنامج بنجاح!", "تفعيل البرنامج", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    StatusMessage = "فشل التفعيل: " + info.Message;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "خطأ: " + ex.Message;
            }
        }

        private void ImportLicenseFile()
        {
            // License key is entered directly in the text box; file import not supported from VM
            StatusMessage = "يرجى إدخال مفتاح الترخيص يدويًا في الحقل المخصص.";
        }

        private void CopyMachineId()
        {
            System.Windows.Clipboard.SetText(_machineId);
            _statusMessage = "تم نسخ معرف الجهاز إلى الحافظة.";
            OnPropertyChanged(nameof(StatusMessage));
        }
    }
}
