using System;
using System.IO;
using System.Text;
using Techtoon.Core.Licensing;
using Techtoon.UI.Services;
using Xunit;

namespace Techtoon.Tests;

/// <summary>
/// دورة حياة رخصة العميل: إصدار ← تفعيل ← ترقية/تجديد ← رفض غير صالح.
/// (المحاكاة على مستوى الخدمة — نفس الكود الذي تستدعيه صفحة الإعدادات.)
/// </summary>
[Collection("DatabaseIntegration")]
public class CustomerLicenseLifecycleTests : IDisposable
{
    private readonly string _sandbox;
    private readonly string _targetPath;
    private readonly string _backupPath;
    private readonly bool _hadLicense;

    public CustomerLicenseLifecycleTests()
    {
        _sandbox = Path.Combine(Path.GetTempPath(), "techtoon_liclife_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_sandbox);
        _targetPath = LicenseService.GetWritableLicensePath();
        _backupPath = Path.Combine(_sandbox, "license.backup");
        _hadLicense = File.Exists(_targetPath);
        if (_hadLicense) File.Copy(_targetPath, _backupPath, true);
    }

    public void Dispose()
    {
        try
        {
            if (_hadLicense && File.Exists(_backupPath)) File.Copy(_backupPath, _targetPath, true);
            else if (File.Exists(_targetPath)) File.Delete(_targetPath);
            if (Directory.Exists(_sandbox)) Directory.Delete(_sandbox, true);
        }
        catch { }
    }

    private string SandboxFile(string name) => Path.Combine(_sandbox, name);

    [Fact]
    public void Customer_Activates_Their_License_And_Sees_Their_Name()
    {
        // المورد يُصدر رخصة باسم العميل
        var licenseFile = SandboxFile("customer.key");
        File.WriteAllText(licenseFile,
            LicenseService.CreateLicenseFile(LicenseTypes.Yearly, "شركة الأمل للتجارة", 365),
            new UTF8Encoding(false));

        // العميل يستوردها
        var result = LicenseService.ValidateAndImportFile(licenseFile);
        Assert.True(result.Success, result.Message);
        Assert.Equal("شركة الأمل للتجارة", result.Info.IssuedTo);
        Assert.Equal(LicenseTypes.Yearly, result.Info.LicenseType);
        Assert.False(result.Info.IsMachineBound);   // تعمل على أي جهاز
        Assert.True(result.Info.IsAllowed);

        // الحالة المعروضة للمستخدم
        var status = LicenseService.ReadExistingLicenseInfo();
        Assert.NotNull(status);
        Assert.Equal("شركة الأمل للتجارة", status!.IssuedTo);
        Assert.Equal("رخصة سنوية", LicenseTypes.Arabic(status.LicenseType));
    }

    [Fact]
    public void Customer_Upgrades_From_Monthly_To_Yearly_And_Keeps_Access()
    {
        // 1) يبدأ برخصة شهرية
        var monthly = SandboxFile("monthly.key");
        File.WriteAllText(monthly,
            LicenseService.CreateLicenseFile(LicenseTypes.Monthly, "مؤسسة النور", 30),
            new UTF8Encoding(false));
        var r1 = LicenseService.ValidateAndImportFile(monthly);
        Assert.True(r1.Success, r1.Message);
        Assert.Equal(LicenseTypes.Monthly, r1.Info.LicenseType);

        // 2) يرقّي لرخصة سنوية (نفس العميل)
        var yearly = SandboxFile("yearly.key");
        File.WriteAllText(yearly,
            LicenseService.CreateLicenseFile(LicenseTypes.Yearly, "مؤسسة النور", 365),
            new UTF8Encoding(false));
        var r2 = LicenseService.ValidateAndImportFile(yearly);
        Assert.True(r2.Success, r2.Message);
        Assert.Equal(LicenseTypes.Yearly, r2.Info.LicenseType);

        // 3) الحالة النهائية: سنوية — الطلب محفوظ
        var status = LicenseService.ReadExistingLicenseInfo();
        Assert.NotNull(status);
        Assert.Equal(LicenseTypes.Yearly, status!.LicenseType);
        Assert.Equal("مؤسسة النور", status.IssuedTo);
        Assert.True(status.IsAllowed);
        Assert.True(status.DaysLeft > 300);
    }

    [Fact]
    public void Expired_License_Does_Not_Replace_A_Valid_One()
    {
        // ترخيص سنوي صالح
        var good = SandboxFile("good.key");
        File.WriteAllText(good,
            LicenseService.CreateLicenseFile(LicenseTypes.Lifetime, "عميل محفوظ", 0),
            new UTF8Encoding(false));
        Assert.True(LicenseService.ValidateAndImportFile(good).Success);

        // المحاول استيراد رخصة منتهية — يجب ألا تمسّ الرخصة السارية
        var expired = SandboxFile("expired.key");
        File.WriteAllText(expired,
            LicenseService.CreateLicenseFile(LicenseTypes.Monthly, "عميل محفوظ", 30,
                machineId: "", expiresAt: DateTime.Now.AddDays(-1)),
            new UTF8Encoding(false));
        var res = LicenseService.ValidateAndImportFile(expired);
        Assert.False(res.Success);

        // الرخصة الأصلية سليمة
        var status = LicenseService.ReadExistingLicenseInfo();
        Assert.Equal(LicenseTypes.Lifetime, status!.LicenseType);
        Assert.Equal("عميل محفوظ", status.IssuedTo);
    }

    [Fact]
    public void Tampered_License_Is_Rejected_And_Existing_One_Survives()
    {
        var good = SandboxFile("good2.key");
        File.WriteAllText(good,
            LicenseService.CreateLicenseFile(LicenseTypes.Lifetime, "العميل الأصلي", 0),
            new UTF8Encoding(false));
        Assert.True(LicenseService.ValidateAndImportFile(good).Success);

        // عبث باسم العميل (التوقيع لم يعد يطابق)
        var tampered = SandboxFile("tampered.key");
        var content = File.ReadAllText(good, Encoding.UTF8)
            .Replace("العميل الأصلي", "المخترق");
        File.WriteAllText(tampered, content, new UTF8Encoding(false));

        var res = LicenseService.ValidateAndImportFile(tampered);
        Assert.False(res.Success);

        var status = LicenseService.ReadExistingLicenseInfo();
        Assert.Equal("العميل الأصلي", status!.IssuedTo);
    }

    [Fact]
    public void License_Works_On_Any_Machine_Because_No_Machine_Id_Is_Bound()
    {
        // الرخصة المولّدة بلا --machine تحتوي machine_id فارغًا
        var content = LicenseService.CreateLicenseFile(LicenseTypes.Monthly, "عميل متنقّل", 30);
        Assert.Contains("\"machine_id\": \"\"", content);

        var info = LicenseService.ParseLicenseFile(content);
        Assert.True(info.IsAllowed);
        Assert.False(info.IsMachineBound);
        Assert.Equal("عميل متنقّل", info.IssuedTo);
    }

    [Fact]
    public void Every_License_Type_Keeps_Its_Own_Duration_And_Name()
    {
        var cases = new (string type, int days, string arabic)[]
        {
            (LicenseTypes.Trial, 10, "فترة تجريبية"),
            (LicenseTypes.Monthly, 30, "رخصة شهرية"),
            (LicenseTypes.Yearly, 365, "رخصة سنوية"),
            (LicenseTypes.Lifetime, 0, "رخصة مدى الحياة"),
        };

        foreach (var (type, days, arabic) in cases)
        {
            var content = LicenseService.CreateLicenseFile(type, "عميل " + arabic, days);
            var info = LicenseService.ParseLicenseFile(content);

            Assert.True(info.IsAllowed, type + " يجب أن يكون صالحًا");
            Assert.Equal(type, info.LicenseType);
            Assert.Equal(arabic, LicenseTypes.Arabic(info.LicenseType));
            Assert.Equal("عميل " + arabic, info.IssuedTo);

            if (type == LicenseTypes.Lifetime)
            {
                Assert.Null(info.DaysLeft);
                Assert.Null(info.ExpiresAt);
            }
            else
            {
                Assert.NotNull(info.DaysLeft);
                Assert.InRange(info.DaysLeft!.Value, 1, days + 1);
            }
        }
    }
}
