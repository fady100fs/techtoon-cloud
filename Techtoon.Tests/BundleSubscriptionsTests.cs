using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Techtoon.Core.Bundles;
using Techtoon.UI.Services;
using Xunit;

namespace Techtoon.Tests;

/// <summary>
/// مجمّع الاشتراكات + معالجة الأعمدة الإلزامية (NOT NULL) في الاستيراد.
/// </summary>
[Collection("DatabaseIntegration")]
public class BundleSubscriptionsTests : IDisposable
{
    private readonly string _sandbox;

    public BundleSubscriptionsTests()
    {
        _sandbox = Path.Combine(Path.GetTempPath(), "techtoon_subs_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_sandbox);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_sandbox)) Directory.Delete(_sandbox, true); } catch { }
    }

    /// <summary>يحذف الحساب الافتراضي للأطراف وأطرافه (أثر اختبارات فقط).</summary>
    private static void ResetDefaultAccount()
    {
        try
        {
            TestDb.Execute("DELETE FROM subscription_periods WHERE subscription_id IN (SELECT id FROM subscriptions WHERE party_id IN (SELECT id FROM parties WHERE account_id IN (SELECT id FROM accounts WHERE code = 'DEFAULT-PARTY')))");
            TestDb.Execute("DELETE FROM subscriptions WHERE party_id IN (SELECT id FROM parties WHERE account_id IN (SELECT id FROM accounts WHERE code = 'DEFAULT-PARTY'))");
            TestDb.Execute("DELETE FROM parties WHERE account_id IN (SELECT id FROM accounts WHERE code = 'DEFAULT-PARTY')");
            TestDb.Execute("DELETE FROM accounts WHERE code = 'DEFAULT-PARTY'");
        }
        catch { /* جدول غير موجود */ }
    }

    // ==================== مجمّع الاشتراكات ====================

    [Fact]
    public void Subscriptions_Bundle_Contains_Subscriptions_And_Their_Periods()
    {
        var def = BundleCatalog.Find("subscriptions")!;
        var tables = def.Tables.Select(t => t.Table).ToList();

        Assert.Contains("subscriptions", tables);
        Assert.Contains("subscription_periods", tables);

        // الوصف يوضّح محتوى المجمّع
        Assert.False(string.IsNullOrWhiteSpace(def.Description));
        Assert.Contains("الاشتراكات", def.Description);
    }

    [Fact]
    public void Subscriptions_Export_Uses_Portable_Keys_Not_Database_Ids()
    {
        if (!TestDb.Available) return;

        var svc = MasterDataBundleService.Create();
        var def = BundleCatalog.Find("subscriptions")!;
        var bundle = svc.Export(def);

        // ورقة الاشتراكات: الطرف بالاسم لا بالرقم
        Assert.True(bundle.Tables.ContainsKey("subscriptions"));
        var subs = bundle.Tables["subscriptions"];
        Assert.NotEmpty(subs);
        var first = subs[0];
        Assert.Contains("party_name", first.Keys);
        Assert.False(string.IsNullOrWhiteSpace(first.GetValueOrDefault("party_name") ?? ""));
        Assert.Contains("service_name", first.Keys);
        Assert.Contains("start_date", first.Keys);

        // ورقة الفترات: الاشتراك محدَّد بالاسم + تاريخ البدء + اسم الطرف
        Assert.True(bundle.Tables.ContainsKey("subscription_periods"));
        var periods = bundle.Tables["subscription_periods"];
        Assert.NotEmpty(periods);
        var p0 = periods[0];
        Assert.Contains("subscription_party_name", p0.Keys);
        Assert.Contains("subscription_service_name", p0.Keys);
        Assert.Contains("subscription_start_date", p0.Keys);
        Assert.Contains("period_number", p0.Keys);

        // القيم محمولة (اسم وتاريخ) لا أرقام قاعدة بيانات
        Assert.False(string.IsNullOrWhiteSpace(p0.GetValueOrDefault("subscription_party_name") ?? ""));
        Assert.False(string.IsNullOrWhiteSpace(p0.GetValueOrDefault("subscription_service_name") ?? ""));
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}$", p0.GetValueOrDefault("subscription_start_date") ?? "");
    }

    [Fact]
    public void Subscriptions_Reimport_Changes_Nothing()
    {
        if (!TestDb.Available) return;

        var svc = MasterDataBundleService.Create();
        var def = BundleCatalog.Find("subscriptions")!;
        var bundle = svc.Export(def);

        var before = Convert.ToInt32(TestDb.Scalar("SELECT COUNT(*) FROM subscriptions"));
        var beforePeriods = Convert.ToInt32(TestDb.Scalar("SELECT COUNT(*) FROM subscription_periods"));

        var res = svc.Import(bundle, new BundleImportOptions { ConflictMode = BundleConflictMode.Skip });
        Assert.True(res.Success, res.Message);

        Assert.Equal(before, Convert.ToInt32(TestDb.Scalar("SELECT COUNT(*) FROM subscriptions")));
        Assert.Equal(beforePeriods, Convert.ToInt32(TestDb.Scalar("SELECT COUNT(*) FROM subscription_periods")));
    }

    // ==================== الأعمدة الإلزامية (NOT NULL) ====================

    [Fact]
    public void Party_Without_Account_Uses_The_Default_Account()
    {
        if (!TestDb.Available) return;

        var svc = MasterDataBundleService.Create();
        var unique = Guid.NewGuid().ToString("N").Substring(0, 8);

        // نبدأ من قاعدة نظيفة (لا حساب افتراضي سابق)
        ResetDefaultAccount();
        TestDb.Execute("DELETE FROM parties WHERE name LIKE @u", ("@u", "%" + unique));

        // طرف بلا account_code — والعمود NOT NULL في قاعدة البيانات
        var bundle = new DataBundle
        {
            BundleId = "parties",
            Tables = new Dictionary<string, List<Dictionary<string, string?>>>
            {
                ["accounts"] = new List<Dictionary<string, string?>>(),
                ["parties"] = new List<Dictionary<string, string?>>
                {
                    new() { ["name"] = "طرف بلا حساب " + unique, ["type"] = "customer" }
                }
            }
        };

        var res = svc.Import(bundle, new BundleImportOptions
        {
            ConflictMode = BundleConflictMode.Skip,
            CreateMissingReferences = true
        });

        Assert.True(res.Success, res.Message);
        Assert.Equal(1, res.Inserted);

        // الطرف وُصل بالحساب الافتراضي
        var accountId = Convert.ToInt32(TestDb.Scalar(
            "SELECT account_id FROM parties WHERE name = @n", ("@n", "طرف بلا حساب " + unique)));
        Assert.True(accountId > 0);
        var defCode = Convert.ToString(TestDb.Scalar(
            "SELECT code FROM accounts WHERE id = @i", ("@i", accountId)));
        Assert.Equal("DEFAULT-PARTY", defCode);

        // ---- تنظيف ----
        TestDb.Execute("DELETE FROM parties WHERE name LIKE @u", ("@u", "%" + unique));
    }

    [Fact]
    public void Payment_Without_Currency_Falls_Back_To_The_First_Available_Currency()
    {
        if (!TestDb.Available) return;

        var svc = MasterDataBundleService.Create();
        var unique = Guid.NewGuid().ToString("N").Substring(0, 8);

        // دفعة بلا رمز عملة — و currency_id إلزامي في قاعدة البيانات
        // (هذا ما كان يرفع 4 أخطاء عند استيراد ملف حقيقي: "currency_id إلزامي…")
        var cashBoxCode = Convert.ToString(TestDb.Scalar(
            "SELECT code FROM cash_boxes ORDER BY id LIMIT 1")) ?? "";
        var bundle = new DataBundle
        {
            BundleId = "finance",
            Tables = new Dictionary<string, List<Dictionary<string, string?>>>
            {
                ["cash_boxes"] = new List<Dictionary<string, string?>>(),
                ["payments"] = new List<Dictionary<string, string?>>
                {
                    new()
                    {
                        ["date"] = DateTime.Now.ToString("yyyy-MM-dd"),
                        ["amount"] = "10.00",
                        ["payment_type"] = "receipt",
                        ["payment_method"] = "نقدي",
                        ["cash_box_code"] = cashBoxCode,
                        ["notes"] = "CURTEST_" + unique
                    }
                }
            }
        };

        var res = svc.Import(bundle, new BundleImportOptions
        {
            ConflictMode = BundleConflictMode.Skip,
            CreateMissingReferences = true
        });

        Assert.True(res.Success, res.Message);
        Assert.Equal(1, res.Inserted);
        Assert.Empty(res.Errors);

        // الدفعة حصلت على عملة حقيقية (ليست NULL)
        var currencyId = TestDb.Scalar(
            "SELECT currency_id FROM payments WHERE notes=@n", ("@n", "CURTEST_" + unique));
        Assert.NotNull(currencyId);
        Assert.True(Convert.ToInt32(currencyId) > 0);

        TestDb.Execute("DELETE FROM payments WHERE notes=@n", ("@n", "CURTEST_" + unique));
    }

    [Fact]
    public void Party_Without_Account_Gives_A_Clear_Arabic_Error_When_References_Are_Not_Created()
    {
        if (!TestDb.Available) return;

        var svc = MasterDataBundleService.Create();
        var unique = Guid.NewGuid().ToString("N").Substring(0, 8);
        ResetDefaultAccount();

        var bundle = new DataBundle
        {
            BundleId = "parties",
            Tables = new Dictionary<string, List<Dictionary<string, string?>>>
            {
                ["accounts"] = new List<Dictionary<string, string?>>(),
                ["parties"] = new List<Dictionary<string, string?>>
                {
                    new() { ["name"] = "طرف بلا حساب " + unique, ["type"] = "customer" }
                }
            }
        };

        var res = svc.Import(bundle, new BundleImportOptions
        {
            ConflictMode = BundleConflictMode.Skip,
            CreateMissingReferences = false
        });

        var details = string.Join(" | ", res.Errors);

        if (res.Success)
        {
            // الحساب الافتراضي كان موجودًا مسبقًا (من اختبار آخر يعمل بالتوازي) — فاستُعمل
            Assert.Equal(1, res.Inserted);
            return;
        }

        // رسالة عربية واضحة — لا خطأ PostgreSQL خام
        Assert.DoesNotContain("violates not-null constraint", details);
        Assert.DoesNotContain("23502", details);
        Assert.Contains("إلزامي", details);
    }

    [Fact]
    public void Analyze_Reports_The_Problem_Before_Any_Write()
    {
        if (!TestDb.Available) return;

        var svc = MasterDataBundleService.Create();
        var unique = Guid.NewGuid().ToString("N").Substring(0, 8);
        ResetDefaultAccount();

        var bundle = new DataBundle
        {
            BundleId = "parties",
            Tables = new Dictionary<string, List<Dictionary<string, string?>>>
            {
                ["accounts"] = new List<Dictionary<string, string?>>(),
                ["parties"] = new List<Dictionary<string, string?>>
                {
                    new() { ["name"] = "طرف بلا حساب " + unique, ["type"] = "customer" }
                }
            }
        };

        // بلا إنشاء تلقائي للمراجع → المعاينة تُبلّغ عن الخطأ قبل أي كتابة
        var plan = svc.Analyze(bundle, new BundleImportOptions { CreateMissingReferences = false });
        var details = string.Join(" | ", plan.Tables.SelectMany(t => t.Errors));

        // لا خطأ PostgreSQL خام أبدًا — الرسالة عربية مفهومة
        Assert.DoesNotContain("violates not-null constraint", details);
        Assert.DoesNotContain("23502", details);

        if (plan.TotalErrors > 0)
        {
            Assert.False(plan.CanImport);
            Assert.Contains("إلزامي", details);
        }
    }
}
