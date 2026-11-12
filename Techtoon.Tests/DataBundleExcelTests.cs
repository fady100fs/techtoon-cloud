using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Techtoon.Core.Bundles;
using Techtoon.UI.Services;
using Xunit;

namespace Techtoon.Tests;

/// <summary>
/// استيراد/تصدير المُجمّعات عبر ملفات Excel (.xlsx).
/// كل جدول في ورقة مستقلة، والصف الأول رؤوس الأعمدة.
/// </summary>
[Collection("DatabaseIntegration")]
public class DataBundleExcelTests : IDisposable
{
    private readonly string _sandbox;

    public DataBundleExcelTests()
    {
        _sandbox = Path.Combine(Path.GetTempPath(), "techtoon_xl_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_sandbox);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_sandbox)) Directory.Delete(_sandbox, true); } catch { }
    }

    private string SandboxFile(string name) => Path.Combine(_sandbox, name);

    [Fact]
    public void Export_To_Excel_Creates_One_Sheet_Per_Table()
    {
        if (!TestDb.Available) return;

        var svc = MasterDataBundleService.Create();
        var def = BundleCatalog.Find("parties")!;
        var path = SandboxFile("parties.xlsx");

        var full = svc.ExportToExcelFile(def, path);
        Assert.True(File.Exists(full));
        Assert.True(new FileInfo(full).Length > 0);

        var bundle = DataBundleExcel.Read(full);
        Assert.Equal("parties", bundle.BundleId);
        Assert.Equal(def.Title, bundle.Title);

        // ورقة لكل جدول
        Assert.Contains("accounts", bundle.Tables.Keys);
        Assert.Contains("parties", bundle.Tables.Keys);

        // عدد الصفوف مطابق لقاعدة البيانات
        var accountsCount = Convert.ToInt32(TestDb.Scalar("SELECT COUNT(*) FROM accounts"));
        Assert.Equal(accountsCount, bundle.Tables["accounts"].Count);

        var partiesCount = Convert.ToInt32(TestDb.Scalar("SELECT COUNT(*) FROM parties"));
        Assert.Equal(partiesCount, bundle.Tables["parties"].Count);
    }

    [Fact]
    public void Excel_Export_Uses_Natural_Keys_Not_Database_Ids()
    {
        if (!TestDb.Available) return;

        var svc = MasterDataBundleService.Create();
        var def = BundleCatalog.Find("parties")!;
        var path = SandboxFile("parties_keys.xlsx");
        svc.ExportToExcelFile(def, path);

        var bundle = DataBundleExcel.Read(path);
        var party = bundle.Tables["parties"].FirstOrDefault();
        if (party == null) return;

        // المفاتيح الطبيعية موجودة
        Assert.Contains("name", party.Keys);
        Assert.Contains("type", party.Keys);
        // حساب الطرف مرتبط بكود الحساب لا برقمه
        Assert.Contains("account_code", party.Keys);
        // لا يوجد عمود id رقمي في الملف (المفتاح الطبيعي هو الهوية)
        Assert.False(party.ContainsKey("id"));
    }

    [Fact]
    public void Excel_RoundTrip_Preserves_All_Values()
    {
        if (!TestDb.Available) return;

        var svc = MasterDataBundleService.Create();
        var def = BundleCatalog.Find("parties")!;
        var path = SandboxFile("roundtrip.xlsx");
        svc.ExportToExcelFile(def, path);

        var first = DataBundleExcel.Read(path);
        var second = DataBundleExcel.Read(path);

        Assert.Equal(first.Tables.Count, second.Tables.Count);
        foreach (var (table, rows) in first.Tables)
        {
            Assert.Equal(rows.Count, second.Tables[table].Count);
            for (int i = 0; i < rows.Count; i++)
            {
                foreach (var kv in rows[i])
                {
                    var other = second.Tables[table][i];
                    Assert.True(other.ContainsKey(kv.Key), "عمود مفقود: " + kv.Key);
                    Assert.Equal(kv.Value ?? "", other[kv.Key] ?? "");
                }
            }
        }
    }

    [Fact]
    public void Excel_Import_Of_Exported_Bundle_Changes_Nothing()
    {
        if (!TestDb.Available) return;

        var svc = MasterDataBundleService.Create();
        var def = BundleCatalog.Find("parties")!;
        var path = SandboxFile("reimport.xlsx");
        svc.ExportToExcelFile(def, path);

        var accountsBefore = Convert.ToInt32(TestDb.Scalar("SELECT COUNT(*) FROM accounts"));
        var partiesBefore = Convert.ToInt32(TestDb.Scalar("SELECT COUNT(*) FROM parties"));

        var bundle = DataBundleExcel.Read(path);
        var res = svc.Import(bundle, new BundleImportOptions { ConflictMode = BundleConflictMode.Skip });

        Assert.True(res.Success, res.Message);
        Assert.Equal(0, res.Inserted);           // كل شيء موجود مسبقًا
        Assert.Equal(accountsBefore, Convert.ToInt32(TestDb.Scalar("SELECT COUNT(*) FROM accounts")));
        Assert.Equal(partiesBefore, Convert.ToInt32(TestDb.Scalar("SELECT COUNT(*) FROM parties")));
    }

    [Fact]
    public void Excel_Import_Creates_New_Rows_And_Links_Them()
    {
        if (!TestDb.Available) return;

        var svc = MasterDataBundleService.Create();
        var unique = Guid.NewGuid().ToString("N").Substring(0, 8);

        // نبني حزمة Excel يدويًا: حساب + عميل
        var bundle = new DataBundle
        {
            BundleId = "parties",
            Title = "اختبار Excel",
            Tables = new Dictionary<string, List<Dictionary<string, string?>>>
            {
                ["accounts"] = new List<Dictionary<string, string?>>
                {
                    new() { ["code"] = "X" + unique, ["name"] = "حساب " + unique, ["type"] = "LIABILITY", ["parent_id"] = null, ["parent_code"] = null }
                },
                ["parties"] = new List<Dictionary<string, string?>>
                {
                    new() { ["name"] = "عميل " + unique, ["type"] = "customer", ["account_id"] = null,
                            ["account_code"] = "X" + unique, ["phone"] = "0100", ["address"] = "عنوان",
                            ["credit_limit"] = "100", ["opening_balance"] = "0", ["is_active"] = "true" }
                }
            }
        };

        var path = SandboxFile("manual.xlsx");
        DataBundleExcel.Write(bundle, path);

        // نقرأ الملف ثم نستورده
        var loaded = DataBundleExcel.Read(path);
        var plan = svc.Analyze(loaded);
        Assert.Equal(0, plan.TotalErrors);
        Assert.Equal(2, plan.TotalInsert);

        var res = svc.Import(loaded, new BundleImportOptions { ConflictMode = BundleConflictMode.Skip });
        Assert.True(res.Success, res.Message);
        Assert.Equal(2, res.Inserted);

        // العميل أُنشئ مرتبطًا بالحساب المستورد
        var accountId = Convert.ToInt32(TestDb.Scalar(
            "SELECT account_id FROM parties WHERE name=@n", ("@n", "عميل " + unique)));
        Assert.True(accountId > 0);
        Assert.Equal(1, Convert.ToInt32(TestDb.Scalar(
            "SELECT COUNT(*) FROM accounts WHERE id=@i", ("@i", accountId))));

        // ---- تنظيف ----
        TestDb.Execute("DELETE FROM parties WHERE name=@n", ("@n", "عميل " + unique));
        TestDb.Execute("DELETE FROM accounts WHERE code=@c", ("@c", "X" + unique));
    }

    [Fact]
    public void Excel_File_Can_Be_Opened_And_Edited_By_The_User()
    {
        if (!TestDb.Available) return;

        var svc = MasterDataBundleService.Create();
        var def = BundleCatalog.Find("parties")!;
        var path = SandboxFile("editable.xlsx");
        svc.ExportToExcelFile(def, path);

        // نعدّل الملف (كما سيفعل المستخدم في Excel): نضيف عميلًا جديدًا
        var bundle = DataBundleExcel.Read(path);
        var unique = Guid.NewGuid().ToString("N").Substring(0, 8);
        bundle.Tables["parties"].Add(new Dictionary<string, string?>
        {
            ["name"] = "عميل معدَّل " + unique,
            ["type"] = "customer",
            ["account_code"] = bundle.Tables["parties"].FirstOrDefault()?.GetValueOrDefault("account_code") ?? ""
        });
        DataBundleExcel.Write(bundle, path);

        // نعيد القراءة ونستورد
        var loaded = DataBundleExcel.Read(path);
        var res = svc.Import(loaded, new BundleImportOptions { ConflictMode = BundleConflictMode.Skip });
        Assert.True(res.Success, res.Message);
        Assert.Equal(1, res.Inserted);   // العميل الجديد فقط

        TestDb.Execute("DELETE FROM parties WHERE name=@n", ("@n", "عميل معدَّل " + unique));
    }
}
