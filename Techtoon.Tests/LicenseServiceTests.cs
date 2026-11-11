using System;
using System.IO;
using System.Text;
using System.Text.Json;
using Techtoon.Core.Licensing;
using Xunit;

namespace Techtoon.Tests;

public class LicenseServiceTests : IDisposable
{
    private readonly string _sandbox;

    public LicenseServiceTests()
    {
        // كل الاختبارات تعمل داخل مجلد معزول — لا تلمس %APPDATA% الحقيقي.
        _sandbox = Path.Combine(Path.GetTempPath(), "techtoon_lic_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_sandbox);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_sandbox)) Directory.Delete(_sandbox, true); } catch { }
    }

    private string SandboxFile(string name) => Path.Combine(_sandbox, name);

    // ---------- التوقيع ----------

    [Fact]
    public void Sign_And_Verify_RoundTrips()
    {
        var data = new System.Collections.Generic.Dictionary<string, object?>
        {
            ["license_type"] = LicenseTypes.Yearly,
            ["issued_to"] = "شركة الأمل",
            ["machine_id"] = "",
            ["issued_at"] = "2026-01-01T00:00:00",
            ["expires_at"] = "2027-01-01T00:00:00"
        };
        var sig = LicenseService.Sign(data);
        Assert.False(string.IsNullOrWhiteSpace(sig));
        Assert.True(LicenseService.VerifySignature(data, sig));
    }

    [Fact]
    public void VerifySignature_Fails_After_Tampering()
    {
        var content = LicenseService.CreateLicenseFile(LicenseTypes.Yearly, "عميل", 365);
        var doc = JsonDocument.Parse(content);
        var raw = doc.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => (object?)p.Value.GetString());
        var sig = raw["signature"]?.ToString() ?? "";

        // العبث بقيمة
        raw["expires_at"] = "2099-01-01T00:00:00";
        Assert.False(LicenseService.VerifySignature(raw, sig));
    }

    // ---------- التحقق بدون كتابة ----------

    [Fact]
    public void ParseLicenseFile_Accepts_Signed_Portable_License()
    {
        var content = LicenseService.CreateLicenseFile(LicenseTypes.Yearly, "عميل", 365, machineId: "");
        var info = LicenseService.ParseLicenseFile(content);

        Assert.Equal(LicenseState.Valid, info.State);
        Assert.True(info.IsAllowed);
        Assert.Equal(LicenseTypes.Yearly, info.LicenseType);
        Assert.Equal("عميل", info.IssuedTo);
        Assert.False(info.IsMachineBound); // غير مربوط بجهاز
    }

    [Fact]
    public void ParseLicenseFile_Rejects_Tampered_Content()
    {
        var content = LicenseService.CreateLicenseFile(LicenseTypes.Lifetime, "عميل", 0);
        var tampered = content.Replace("\"issued_to\": \"عميل\"", "\"issued_to\": \"مخترق\"");

        var info = LicenseService.ParseLicenseFile(tampered);
        Assert.Equal(LicenseState.Invalid, info.State);
        Assert.False(info.IsAllowed);
    }

    [Fact]
    public void ParseLicenseFile_Rejects_Expired_License()
    {
        var expired = DateTime.Now.AddDays(-1);
        var content = LicenseService.CreateLicenseFile(LicenseTypes.Monthly, "عميل", 30, machineId: "", expiresAt: expired);

        var info = LicenseService.ParseLicenseFile(content);
        Assert.Equal(LicenseState.Expired, info.State);
        Assert.False(info.IsAllowed);
    }

    [Fact]
    public void ParseLicenseFile_Rejects_Garbage()
    {
        var info = LicenseService.ParseLicenseFile("this is not json");
        Assert.Equal(LicenseState.Invalid, info.State);
        Assert.False(info.IsAllowed);
    }

    [Fact]
    public void ParseLicenseFile_Rejects_Unknown_Type()
    {
        var content = LicenseService.CreateLicenseFile("weekly", "عميل", 7);
        var info = LicenseService.ParseLicenseFile(content);
        Assert.Equal(LicenseState.Invalid, info.State);
    }

    // ---------- الاستيراد الذرّي ----------

    [Fact]
    public void ValidateAndImport_Writes_File_And_Reports_Path()
    {
        var content = LicenseService.CreateLicenseFile(LicenseTypes.Lifetime, "عميل", 0);
        var target = SandboxFile("license.key");

        var result = LicenseService.ValidateAndImportToFile(content, target);

        Assert.True(result.Success, result.Message);
        Assert.Equal(target, result.WrittenTo);
        Assert.True(File.Exists(target));

        var reread = LicenseService.ParseLicenseFile(File.ReadAllText(target));
        Assert.Equal(LicenseState.Valid, reread.State);
    }

    [Fact]
    public void ValidateAndImport_Invalid_File_Does_Not_Overwrite_Existing_Valid_License()
    {
        var good = LicenseService.CreateLicenseFile(LicenseTypes.Lifetime, "عميل", 0);
        var target = SandboxFile("license.key");
        Assert.True(LicenseService.ValidateAndImportToFile(good, target).Success);

        var before = File.ReadAllText(target);

        // محاولة استيراد ملف تالف
        var bad = LicenseService.ValidateAndImportToFile("{\"license_type\":\"lifetime\"}", target);
        Assert.False(bad.Success);

        // الملف الأصلي لم يتغيّر
        Assert.Equal(before, File.ReadAllText(target));
        Assert.Equal(LicenseState.Valid, LicenseService.ParseLicenseFile(File.ReadAllText(target)).State);
    }

    [Fact]
    public void ValidateAndImport_Expired_File_Does_Not_Overwrite_Existing_Valid_License()
    {
        var good = LicenseService.CreateLicenseFile(LicenseTypes.Lifetime, "عميل", 0);
        var target = SandboxFile("license.key");
        Assert.True(LicenseService.ValidateAndImportToFile(good, target).Success);
        var before = File.ReadAllText(target);

        var expired = LicenseService.CreateLicenseFile(LicenseTypes.Monthly, "آخر", 30, machineId: "", expiresAt: DateTime.Now.AddDays(-2));
        var res = LicenseService.ValidateAndImportToFile(expired, target);

        Assert.False(res.Success);
        Assert.Equal(before, File.ReadAllText(target));
    }

    [Fact]
    public void ValidateAndImportFile_Reads_From_Disk()
    {
        var content = LicenseService.CreateLicenseFile(LicenseTypes.Trial, "تجريبي", 10);
        var file = SandboxFile("trial.key");
        File.WriteAllText(file, content, new UTF8Encoding(false));

        var target = SandboxFile("license.key");
        var result = LicenseService.ValidateAndImportFileTo(file, target);

        Assert.True(result.Success, result.Message);
        Assert.Equal(LicenseState.Trial, LicenseService.ParseLicenseFile(File.ReadAllText(target)).State);
    }

    [Fact]
    public void ValidateAndImportFile_Missing_File_Fails_Without_Throw()
    {
        var result = LicenseService.ValidateAndImportFileTo(SandboxFile("nope.key"), SandboxFile("license.key"));
        Assert.False(result.Success);
        Assert.False(File.Exists(SandboxFile("license.key")));
    }

    // ---------- الأنواع ----------

    [Theory]
    [InlineData(LicenseTypes.Lifetime)]
    [InlineData(LicenseTypes.Yearly)]
    [InlineData(LicenseTypes.Monthly)]
    [InlineData(LicenseTypes.Trial)]
    public void All_License_Types_Produce_Valid_Licenses(string type)
    {
        var content = LicenseService.CreateLicenseFile(type, "عميل", type == LicenseTypes.Trial ? 10 : 0);
        var info = LicenseService.ParseLicenseFile(content);
        Assert.True(info.IsAllowed);
        Assert.Equal(type, info.LicenseType);
    }

    [Fact]
    public void Lifetime_License_Has_No_Expiry_Countdown()
    {
        var content = LicenseService.CreateLicenseFile(LicenseTypes.Lifetime, "عميل", 0);
        var info = LicenseService.ParseLicenseFile(content);
        Assert.Equal(LicenseState.Valid, info.State);
        Assert.Null(info.DaysLeft);
    }

    [Fact]
    public void Trial_License_Reports_Days_Left()
    {
        var content = LicenseService.CreateLicenseFile(LicenseTypes.Trial, "تجريبي", 10);
        var info = LicenseService.ParseLicenseFile(content);
        Assert.Equal(LicenseState.Trial, info.State);
        Assert.NotNull(info.DaysLeft);
        Assert.InRange(info.DaysLeft!.Value, 1, 10);
    }

    // ---------- الترخيص غير المربوط بجهاز ----------

    [Fact]
    public void Portable_License_Is_Not_Bound_To_Machine()
    {
        var content = LicenseService.CreateLicenseFile(LicenseTypes.Yearly, "عميل", 365, machineId: "");
        var info = LicenseService.ParseLicenseFile(content);
        Assert.False(info.IsMachineBound);
        Assert.True(info.IsAllowed);
        // لا يوجد أي اعتماد على معرّف الجهاز في القرار
        Assert.NotEqual(LicenseState.MachineMismatch, info.State);
    }

    [Fact]
    public void Machine_Bound_License_With_Foreign_Id_Is_Rejected()
    {
        var content = LicenseService.CreateLicenseFile(LicenseTypes.Yearly, "عميل", 365, machineId: "DEADBEEFDEADBEEF");
        var info = LicenseService.ParseLicenseFile(content);
        Assert.True(info.IsMachineBound);
        Assert.Equal(LicenseState.MachineMismatch, info.State);
    }
}
