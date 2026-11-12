using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Techtoon.Core.Bundles;
using Techtoon.UI.Services;
using Xunit;

namespace Techtoon.Tests;

/// <summary>
/// استيراد/تصدير المجمّعات (Bundles) — اختبار تكامل على قاعدة الاختبار المعزولة.
/// يثبت: تصدير → استيراد في قاعدة أخرى/معدّلة → تطابق البيانات.
/// </summary>
[Collection("DatabaseIntegration")]
public class DataBundleIntegrationTests
{
    // ---------- التصدير ----------

    [Fact]
    public void Export_Parties_Produces_Bundle_With_Natural_Keys()
    {
        if (!TestDb.Available) return;

        var svc = MasterDataBundleService.Create();
        var def = BundleCatalog.Find("parties")!;

        var bundle = svc.Export(def);

        Assert.Equal("parties", bundle.BundleId);
        Assert.True(bundle.Tables.ContainsKey("accounts"));
        Assert.True(bundle.Tables.ContainsKey("parties"));

        // المفاتيح طبيعية (لا أرقام)
        var party = bundle.Tables["parties"].FirstOrDefault();
        if (party != null)
        {
            Assert.Contains("name", party.Keys);
            Assert.Contains("type", party.Keys);
            Assert.DoesNotContain("id", party.Keys);
            // حساب الطرف يُصدَّر بكوده لا برقمه
            Assert.Contains("account_code", party.Keys);
        }

        // الملف قابل للتحليل كـ JSON
        var json = bundle.ToJson();
        var round = DataBundle.FromJson(json);
        Assert.Equal(bundle.Tables.Count, round.Tables.Count);
    }

    [Fact]
    public void Export_Writes_Json_File()
    {
        if (!TestDb.Available) return;
        var svc = MasterDataBundleService.Create();
        var def = BundleCatalog.Find("items")!;
        var path = Path.Combine(Path.GetTempPath(), "techtoon_bundle_" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var full = svc.ExportToFile(def, path);
            Assert.True(File.Exists(full));

            var bundle = DataBundle.FromJson(File.ReadAllText(full, Encoding.UTF8));
            Assert.Equal("items", bundle.BundleId);
            Assert.Contains("items", bundle.Tables.Keys);
        }
        finally
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }
    }

    [Fact]
    public void Every_Bundle_Definition_Is_Well_Formed()
    {
        foreach (var def in BundleCatalog.All())
        {
            Assert.NotEmpty(def.Id);
            Assert.NotEmpty(def.Title);
            Assert.NotEmpty(def.Tables);

            var seen = new HashSet<string>();
            foreach (var spec in def.Tables)
            {
                Assert.NotEmpty(spec.Table);
                // لا تكرار للجداول داخل المجمّع
                Assert.True(seen.Add(spec.Table), "جدول مكرر في المجمّع " + def.Id + ": " + spec.Table);
                Assert.NotEmpty(spec.KeyColumns);
                foreach (var kc in spec.KeyColumns)
                    Assert.Contains(spec.Columns, c => c.Db == kc);
                // المراجع تشير إلى جداول موجودة
                foreach (var r in spec.References)
                {
                    Assert.Contains(r.TargetTable, def.Tables.Select(t => t.Table));
                    Assert.Equal(r.TargetKeys.Count, r.ExportKeys.Count);
                }
            }
        }
    }

    [Fact]
    public void Every_Bundle_Column_Exists_In_Real_Schema()
    {
        if (!TestDb.Available) return;

        // الدرع: كل عمود مُعرَّف في الكتالوج يجب أن يوجد فعلًا في المخطط الحقيقي
        // (وإلا فالتصدير سيفشل بصمت على عمود مفقود).
        using var conn = TestDb.Open();
        var schema = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        using (var cmd = new Npgsql.NpgsqlCommand(
            "SELECT table_name, column_name FROM information_schema.columns WHERE table_schema='public'", conn))
        using (var r = cmd.ExecuteReader())
        {
            while (r.Read())
            {
                var t = r.GetString(0);
                if (!schema.TryGetValue(t, out var set))
                {
                    set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    schema[t] = set;
                }
                set.Add(r.GetString(1));
            }
        }

        foreach (var def in BundleCatalog.All())
            foreach (var spec in def.Tables)
            {
                Assert.True(schema.ContainsKey(spec.Table), "جدول غير موجود: " + spec.Table);
                foreach (var c in spec.Columns)
                    Assert.True(schema[spec.Table].Contains(c.Db),
                        "عمود غير موجود: " + spec.Table + "." + c.Db + " (المجمّع " + def.Id + ")");
            }
    }

    // ---------- الاستيراد ----------

    [Fact]
    public void Import_Creates_Parties_And_Its_Account_Atomically()
    {
        if (!TestDb.Available) return;

        var svc = MasterDataBundleService.Create();
        var unique = Guid.NewGuid().ToString("N").Substring(0, 8);
        var partyName = "عميل اختبار " + unique;
        var accountCode = "T" + unique;

        try
        {
            // حزمة يدوية: حساب + عميل
            var bundle = new DataBundle
            {
                BundleId = "parties",
                Title = "اختبار",
                Tables = new Dictionary<string, List<Dictionary<string, string?>>>
                {
                    ["accounts"] = new List<Dictionary<string, string?>>
                    {
                        new Dictionary<string, string?>
                        {
                            ["code"] = accountCode,
                            ["name"] = "حساب اختبار " + unique,
                            ["type"] = "LIABILITY",
                            ["parent_id"] = null
                        }
                    },
                    ["parties"] = new List<Dictionary<string, string?>>
                    {
                        new Dictionary<string, string?>
                        {
                            ["name"] = partyName,
                            ["type"] = "customer",
                            ["account_id"] = null,
                            ["account_code"] = accountCode,
                            ["phone"] = "0100000000",
                            ["address"] = "عنوان",
                            ["credit_limit"] = "5000",
                            ["opening_balance"] = "0",
                            ["is_active"] = "true"
                        }
                    }
                }
            };

            // 1) المعاينة (لا تكتب)
            var plan = svc.Analyze(bundle);
            Assert.Equal(0, plan.TotalErrors);
            Assert.Equal(2, plan.TotalInsert);

            // لا شيء كُتب بعد
            Assert.Equal(0, CountRows("accounts", "code = '" + accountCode + "'"));

            // 2) الاستيراد
            var res = svc.Import(bundle, new BundleImportOptions { ConflictMode = BundleConflictMode.Skip });
            Assert.True(res.Success, res.Message);
            Assert.Equal(2, res.Inserted);

            // 3) التحقق
            Assert.Equal(1, CountRows("accounts", "code = '" + accountCode + "'"));
            Assert.Equal(1, CountRows("parties", "name = '" + partyName + "'"));

            // حساب العميل مرتبط بالحساب المستورد
            var accountId = ScalarInt("SELECT account_id FROM parties WHERE name = @n", ("@n", partyName));
            Assert.True(accountId > 0);
            Assert.Equal(1, CountRows("accounts", "id = " + accountId));
        }
        finally { Cleanup(unique); }
    }

    [Fact]
    public void Import_Skip_Mode_Does_Not_Duplicate()
    {
        if (!TestDb.Available) return;

        var svc = MasterDataBundleService.Create();
        var unique = Guid.NewGuid().ToString("N").Substring(0, 8);
        var partyName = "عميل " + unique;
        var accountCode = "S" + unique;

        try
        {
            var bundle = new DataBundle
            {
                BundleId = "parties",
                Tables = new Dictionary<string, List<Dictionary<string, string?>>>
                {
                    ["accounts"] = new List<Dictionary<string, string?>>
                    {
                        new() { ["code"] = accountCode, ["name"] = "حساب " + unique, ["type"] = "ASSET" }
                    },
                    ["parties"] = new List<Dictionary<string, string?>>
                    {
                        new() { ["name"] = partyName, ["type"] = "customer", ["account_code"] = accountCode }
                    }
                }
            };

            var r1 = svc.Import(bundle, new BundleImportOptions { ConflictMode = BundleConflictMode.Skip });
            Assert.True(r1.Success, r1.Message);
            Assert.Equal(2, r1.Inserted);

            // استيراد نفس الحزمة مرة أخرى → تخطّي
            var r2 = svc.Import(bundle, new BundleImportOptions { ConflictMode = BundleConflictMode.Skip });
            Assert.True(r2.Success, r2.Message);
            Assert.Equal(2, r2.Skipped);
            Assert.Equal(0, r2.Inserted);

            // عدد الصفوف لم يتضاعف
            Assert.Equal(1, CountRows("parties", "name = '" + partyName + "'"));
        }
        finally { Cleanup(unique); }
    }

    [Fact]
    public void Import_Update_Mode_Modifies_Existing_Row()
    {
        if (!TestDb.Available) return;

        var svc = MasterDataBundleService.Create();
        var unique = Guid.NewGuid().ToString("N").Substring(0, 8);
        var partyName = "عميل " + unique;
        var accountCode = "U" + unique;

        try
        {
            var bundle = new DataBundle
            {
                BundleId = "parties",
                Tables = new Dictionary<string, List<Dictionary<string, string?>>>
                {
                    ["accounts"] = new List<Dictionary<string, string?>>
                    {
                        new() { ["code"] = accountCode, ["name"] = "حساب " + unique, ["type"] = "ASSET" }
                    },
                    ["parties"] = new List<Dictionary<string, string?>>
                    {
                        new() { ["name"] = partyName, ["type"] = "customer", ["account_code"] = accountCode, ["phone"] = "0111111111" }
                    }
                }
            };

            svc.Import(bundle, new BundleImportOptions { ConflictMode = BundleConflictMode.Skip });

            // تعديل الملف ثم استيراد بوضع التحديث
            bundle.Tables["parties"][0]["phone"] = "0222222222";
            var res = svc.Import(bundle, new BundleImportOptions { ConflictMode = BundleConflictMode.Update });
            Assert.True(res.Success, res.Message);
            Assert.True(res.Updated >= 1, "كان متوقعًا تحديث صف واحد على الأقل");

            var phone = TestDb.Scalar("SELECT phone FROM parties WHERE name=@n", ("@n", partyName));
            Assert.Equal("0222222222", phone?.ToString());
        }
        finally { Cleanup(unique); }
    }

    [Fact]
    public void Import_Creates_Missing_Reference_Automatically()
    {
        if (!TestDb.Available) return;

        var svc = MasterDataBundleService.Create();
        var unique = Guid.NewGuid().ToString("N").Substring(0, 8);
        var partyName = "عميل مرجعي " + unique;
        var accountCode = "A" + unique;

        try
        {
            // حزمة تشير إلى حساب غير موجود
            var bundle = new DataBundle
            {
                BundleId = "parties",
                Tables = new Dictionary<string, List<Dictionary<string, string?>>>
                {
                    ["accounts"] = new List<Dictionary<string, string?>>(),
                    ["parties"] = new List<Dictionary<string, string?>>
                    {
                        new() { ["name"] = partyName, ["type"] = "customer", ["account_code"] = accountCode }
                    }
                }
            };

            var res = svc.Import(bundle, new BundleImportOptions
            {
                ConflictMode = BundleConflictMode.Skip,
                CreateMissingReferences = true
            });

            Assert.True(res.Success, res.Message);
            Assert.Contains(res.CreatedReferences, r => r.Contains("accounts"));

            // الحساب أُنشئ
            Assert.Equal(1, CountRows("accounts", "code = '" + accountCode + "'"));
            // والعميل أُنشئ مربوطًا به
            Assert.Equal(1, CountRows("parties", "name = '" + partyName + "'"));
        }
        finally { Cleanup(unique); }
    }

    [Fact]
    public void Import_Reports_Missing_Required_Reference_And_Writes_Nothing()
    {
        if (!TestDb.Available) return;

        var svc = MasterDataBundleService.Create();
        var unique = Guid.NewGuid().ToString("N").Substring(0, 8);

        try
        {
            // مرجع مطلوب غير موجود + تعطيل الإنشاء التلقائي
            var bundle = new DataBundle
            {
                BundleId = "parties",
                Tables = new Dictionary<string, List<Dictionary<string, string?>>>
                {
                    ["accounts"] = new List<Dictionary<string, string?>>(),
                    ["parties"] = new List<Dictionary<string, string?>>
                    {
                        new() { ["name"] = "عميل ناقص " + unique, ["type"] = "customer", ["account_code"] = "X" + unique }
                    }
                }
            };

            var res = svc.Import(bundle, new BundleImportOptions
            {
                ConflictMode = BundleConflictMode.Skip,
                CreateMissingReferences = false
            });

            Assert.False(res.Success);
            Assert.NotEmpty(res.Errors);
            // لم يُكتب أي صف (ذرّية)
            Assert.Equal(0, CountRows("parties", "name = 'عميل ناقص " + unique + "'"));
        }
        finally { Cleanup(unique); }
    }

    [Fact]
    public void Import_Is_Atomic_When_One_Row_Fails()
    {
        if (!TestDb.Available) return;

        var svc = MasterDataBundleService.Create();
        var unique = Guid.NewGuid().ToString("N").Substring(0, 8);
        var goodName = "سليم " + unique;
        var accountCode = "Z" + unique;

        try
        {
            // صف صحيح + صف ناقص العمود الإلزامي → لا يُكتب أي شيء
            var bundle = new DataBundle
            {
                BundleId = "parties",
                Tables = new Dictionary<string, List<Dictionary<string, string?>>>
                {
                    ["accounts"] = new List<Dictionary<string, string?>>
                    {
                        new() { ["code"] = accountCode, ["name"] = "حساب " + unique, ["type"] = "ASSET" }
                    },
                    ["parties"] = new List<Dictionary<string, string?>>
                    {
                        new() { ["name"] = goodName, ["type"] = "customer", ["account_code"] = accountCode },
                        new() { ["name"] = "ناقص" }   // بلا العمود الإلزامي "type" → يفشل الصف
                    }
                }
            };

            var res = svc.Import(bundle, new BundleImportOptions { ConflictMode = BundleConflictMode.Skip });

            Assert.False(res.Success);
            // الكل تراجع — حتى الصف السليم
            Assert.Equal(0, CountRows("accounts", "code = '" + accountCode + "'"));
            Assert.Equal(0, CountRows("parties", "name = '" + goodName + "'"));
        }
        finally { Cleanup(unique); }
    }

    [Fact]
    public void Import_Items_Creates_Kit_With_Components()
    {
        if (!TestDb.Available) return;

        var svc = MasterDataBundleService.Create();
        var unique = Guid.NewGuid().ToString("N").Substring(0, 8);
        var kitBarcode = "K" + unique;
        var compBarcode = "C" + unique;

        try
        {
            // كيت + مكوّن + سطر الكيت — مجمّع «مركّب»
            var bundle = new DataBundle
            {
                BundleId = "items",
                Tables = new Dictionary<string, List<Dictionary<string, string?>>>
                {
                    ["accounts"] = new List<Dictionary<string, string?>>(),
                    ["categories"] = new List<Dictionary<string, string?>>(),
                    ["warehouses"] = new List<Dictionary<string, string?>>(),
                    ["items"] = new List<Dictionary<string, string?>>
                    {
                        new() { ["name"] = "كيت " + unique, ["barcode"] = kitBarcode, ["sell_price"] = "100", ["cost_price"] = "60", ["is_kit"] = "true" },
                        new() { ["name"] = "مكوّن " + unique, ["barcode"] = compBarcode, ["sell_price"] = "0", ["cost_price"] = "20" }
                    },
                    ["kit_components"] = new List<Dictionary<string, string?>>
                    {
                        new() { ["kit_barcode"] = kitBarcode, ["component_barcode"] = compBarcode, ["quantity"] = "3" }
                    },
                    ["stock_levels"] = new List<Dictionary<string, string?>>()
                }
            };

            var res = svc.Import(bundle, new BundleImportOptions { ConflictMode = BundleConflictMode.Skip });
            Assert.True(res.Success, res.Message + " | errors: " + string.Join(" ; ", res.Errors));

            // الصنفان موجودان
            Assert.Equal(2, CountRows("items", "barcode IN ('" + kitBarcode + "', '" + compBarcode + "')"));

            // سطر الكيت مرتبط بالصنفين
            var kitId = ScalarInt("SELECT id FROM items WHERE barcode=@b", ("@b", kitBarcode));
            var compId = ScalarInt("SELECT id FROM items WHERE barcode=@b", ("@b", compBarcode));
            Assert.True(kitId > 0 && compId > 0);
            Assert.Equal(1, CountRows("kit_components", "kit_item_id = " + kitId + " AND component_item_id = " + compId));
        }
        finally { Cleanup(unique); }
    }

    [Fact]
    public void Export_Then_Reimport_RoundTrip_Keeps_Data()
    {
        if (!TestDb.Available) return;

        var svc = MasterDataBundleService.Create();
        var def = BundleCatalog.Find("items")!;

        // صدّر الحالة الحالية
        var bundle = svc.Export(def);

        var target = bundle.Tables["items"].FirstOrDefault();
        if (target == null) return; // لا توجد أصناف في قاعدة الاختبار

        var barcode = target.TryGetValue("barcode", out var bc) ? bc : null;
        if (string.IsNullOrEmpty(barcode)) return;

        var itemId = ScalarInt("SELECT id FROM items WHERE barcode=@b", ("@b", barcode));
        var oldName = TestDb.Scalar("SELECT name FROM items WHERE barcode=@b", ("@b", barcode))?.ToString();
        if (itemId <= 0) return;

        try
        {
            // غيّر الاسم ثم استورد (وضع تحديث) ليعود الاسم الأصلي من الحزمة
            TestDb.Execute("UPDATE items SET name=@n WHERE id=@i", ("@n", "اسم مشوَّه"), ("@i", itemId));
            Assert.Equal("اسم مشوَّه", TestDb.Scalar("SELECT name FROM items WHERE id=@i", ("@i", itemId))?.ToString());

            var res = svc.Import(bundle, new BundleImportOptions { ConflictMode = BundleConflictMode.Update });
            Assert.True(res.Success, res.Message);

            Assert.Equal(oldName, TestDb.Scalar("SELECT name FROM items WHERE id=@i", ("@i", itemId))?.ToString());
        }
        finally
        {
            // نُعيد الاسم الأصلي
            TestDb.Execute("UPDATE items SET name=@n WHERE id=@i", ("@n", oldName), ("@i", itemId));
        }
    }

    // ---------- مساعدات ----------

    /// <summary>يحذف كل ما أنشأه اختبار (أطراف/حسابات/أصناف بكود عشوائي).</summary>
    private static void Cleanup(string unique)
    {
        try
        {
            // الأكواد قد تسبقها بادئة (T/S/U/A/Z/K/C + unique)
            TestDb.Execute("DELETE FROM parties WHERE name LIKE @u", ("@u", "%" + unique));
            TestDb.Execute("DELETE FROM kit_components WHERE kit_item_id IN (SELECT id FROM items WHERE barcode LIKE @u) OR component_item_id IN (SELECT id FROM items WHERE barcode LIKE @u)", ("@u", "%" + unique));
            TestDb.Execute("DELETE FROM stock_levels WHERE item_id IN (SELECT id FROM items WHERE barcode LIKE @u)", ("@u", "%" + unique));
            TestDb.Execute("DELETE FROM items WHERE barcode LIKE @u", ("@u", "%" + unique));
            TestDb.Execute("DELETE FROM accounts WHERE code LIKE @u", ("@u", "%" + unique));
        }
        catch { }
    }

    private static int CountRows(string table, string where)
    {
        var v = TestDb.Scalar("SELECT COUNT(*) FROM " + table + " WHERE " + where);
        return v == null ? 0 : Convert.ToInt32(v);
    }

    private static int ScalarInt(string sql, params (string, object)[] args)
    {
        var v = TestDb.Scalar(sql, args);
        return v == null || v == DBNull.Value ? 0 : Convert.ToInt32(v);
    }
}
