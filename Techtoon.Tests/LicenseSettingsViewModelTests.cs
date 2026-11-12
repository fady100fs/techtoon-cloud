using System;
using System.IO;
using System.Text;
using Techtoon.Core.Licensing;
using Techtoon.UI.Services;
using Techtoon.UI.ViewModels;
using Xunit;

namespace Techtoon.Tests;

/// <summary>
/// تفعيل البرنامج من داخل الإعدادات — اختبار منطق الواجهة (اختيار ملف → تحقق → حفظ → تحديث الحالة).
/// يستخدم محدد ملفات ثابتًا وخدمة رسائل مسجِّلة حتى لا تُفتح نوافذ أثناء الاختبار.
/// </summary>
[Collection("DatabaseIntegration")]
public class LicenseSettingsViewModelTests : IDisposable
{
    private readonly string _sandbox;
    private readonly string _targetLicensePath;
    private readonly string _backupLicensePath;
    private readonly bool _hadExistingLicense;

    public LicenseSettingsViewModelTests()
    {
        _sandbox = Path.Combine(Path.GetTempPath(), "techtoon_licui_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_sandbox);

        // نحفظ الترخيص الموجود (إن وُجد) ونستعيده بعد الاختبار
        _targetLicensePath = LicenseService.GetWritableLicensePath();
        _backupLicensePath = Path.Combine(_sandbox, "license.backup");
        _hadExistingLicense = File.Exists(_targetLicensePath);
        if (_hadExistingLicense) File.Copy(_targetLicensePath, _backupLicensePath, true);
    }

    public void Dispose()
    {
        try
        {
            if (_hadExistingLicense && File.Exists(_backupLicensePath))
                File.Copy(_backupLicensePath, _targetLicensePath, true);
            else if (File.Exists(_targetLicensePath))
                File.Delete(_targetLicensePath);
            if (Directory.Exists(_sandbox)) Directory.Delete(_sandbox, true);
        }
        catch { }
    }

    private string SandboxFile(string name) => Path.Combine(_sandbox, name);

    [Fact]
    public void Import_Valid_License_File_Activates_And_Updates_Status()
    {
        // ملف رخصة حقيقي موقّع
        var licenseFile = SandboxFile("License_Lifetime.key");
        File.WriteAllText(licenseFile,
            LicenseService.CreateLicenseFile(LicenseTypes.Lifetime, "عميل الاختبار", 0),
            new UTF8Encoding(false));

        var dialog = new RecordingDialogService();
        var vm = new LicenseSettingsViewModel(dialog);

        using (LicenseFileDialog.Use(new FixedLicenseFilePicker(licenseFile)))
        {
            vm.ImportLicenseFileCommand.Execute(null);
        }

        // رسالة نجاح
        Assert.Equal("info", dialog.LastKind);
        Assert.Contains(dialog.Messages, m => m.Contains("تم تفعيل البرنامج بنجاح"));

        // الملف حُفظ فعليًا
        Assert.True(File.Exists(_targetLicensePath));

        // الحالة تحدّثت في الواجهة
        Assert.True(vm.IsLicensed);
        Assert.Equal("مرخّص", vm.LicenseStateText);
        Assert.Equal("رخصة مدى الحياة", vm.LicenseTypeText);
        Assert.Equal("عميل الاختبار", vm.LicenseOwnerText);
        Assert.Equal("مدى الحياة", vm.LicenseExpiryText);
        Assert.Contains("تم تفعيل البرنامج بنجاح", vm.StatusMessage);
    }

    [Fact]
    public void Import_Tampered_File_Fails_And_Keeps_Previous_License()
    {
        // ترخيص صالح عروف
        var good = SandboxFile("good.key");
        File.WriteAllText(good, LicenseService.CreateLicenseFile(LicenseTypes.Lifetime, "العميل الأصلي", 0), new UTF8Encoding(false));

        var dialog = new RecordingDialogService();
        var vm = new LicenseSettingsViewModel(dialog);

        using (LicenseFileDialog.Use(new FixedLicenseFilePicker(good)))
        {
            vm.ImportLicenseFileCommand.Execute(null);
        }
        Assert.Equal("info", dialog.LastKind);

        // ملف معدَّل (توقيع غير صالح)
        var tampered = SandboxFile("tampered.key");
        var content = File.ReadAllText(good).Replace("العميل الأصلي", "مخترق");
        File.WriteAllText(tampered, content, new UTF8Encoding(false));

        var dialog2 = new RecordingDialogService();
        var vm2 = new LicenseSettingsViewModel(dialog2);
        using (LicenseFileDialog.Use(new FixedLicenseFilePicker(tampered)))
        {
            vm2.ImportLicenseFileCommand.Execute(null);
        }

        // فشل + رسالة تحذير
        Assert.Equal("warning", dialog2.LastKind);
        Assert.Contains(dialog2.Messages, m => m.Contains("لم يتم التفعيل"));

        // الترخيص السابق لم يتغيّر
        var current = LicenseService.ParseLicenseFile(File.ReadAllText(_targetLicensePath));
        Assert.Equal(LicenseState.Valid, current.State);
        Assert.Equal("العميل الأصلي", current.IssuedTo);

        // الحالة في الواجهة ما زالت سارية
        Assert.True(vm2.IsLicensed);
        Assert.Equal("العميل الأصلي", vm2.LicenseOwnerText);
    }

    [Fact]
    public void Import_Missing_File_Fails_Without_Throw()
    {
        // نبدأ من حالة معروفة: ترخيص صالح موجود
        var good = SandboxFile("good2.key");
        File.WriteAllText(good, LicenseService.CreateLicenseFile(LicenseTypes.Lifetime, "العميل السليم", 0), new UTF8Encoding(false));
        var setup = new LicenseSettingsViewModel(new RecordingDialogService());
        using (LicenseFileDialog.Use(new FixedLicenseFilePicker(good)))
        {
            setup.ImportLicenseFileCommand.Execute(null);
        }
        var before = File.ReadAllText(_targetLicensePath);

        var dialog = new RecordingDialogService();
        var vm = new LicenseSettingsViewModel(dialog);

        using (LicenseFileDialog.Use(new FixedLicenseFilePicker(SandboxFile("nope.key"))))
        {
            vm.ImportLicenseFileCommand.Execute(null);
        }

        // فشل نظيف + رسالة تحذير
        Assert.Equal("warning", dialog.LastKind);
        Assert.Contains("لم يتم التفعيل", vm.StatusMessage);

        // لم يتغيّر الترخيص القائم
        Assert.Equal(before, File.ReadAllText(_targetLicensePath));
    }

    [Fact]
    public void Cancelling_File_Picker_Does_Nothing()
    {
        var dialog = new RecordingDialogService();
        var vm = new LicenseSettingsViewModel(dialog);

        using (LicenseFileDialog.Use(new FixedLicenseFilePicker(null)))
        {
            vm.ImportLicenseFileCommand.Execute(null);
        }

        Assert.Empty(dialog.Messages);
        Assert.Equal("لم يتم اختيار ملف.", vm.StatusMessage);
    }

    [Fact]
    public void Refresh_Command_Reloads_Status()
    {
        var licenseFile = SandboxFile("yearly.key");
        File.WriteAllText(licenseFile,
            LicenseService.CreateLicenseFile(LicenseTypes.Yearly, "عميل سنوي", 365),
            new UTF8Encoding(false));

        var vm = new LicenseSettingsViewModel(new RecordingDialogService());
        using (LicenseFileDialog.Use(new FixedLicenseFilePicker(licenseFile)))
        {
            vm.ImportLicenseFileCommand.Execute(null);
        }

        Assert.Equal("رخصة سنوية", vm.LicenseTypeText);

        // تحديث الحالة
        vm.RefreshCommand.Execute(null);
        Assert.Equal("رخصة سنوية", vm.LicenseTypeText);
        Assert.True(vm.IsLicensed);
    }
}
