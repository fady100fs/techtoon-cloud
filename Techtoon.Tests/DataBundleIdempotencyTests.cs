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
/// B-Data: Import must be idempotent even when the natural key contains NULL/empty values.
///
/// <para><b>Why:</b> <c>NULL = NULL</c> is never true in SQL, so a row with an empty
/// barcode would never be matched and would be INSERTed again on every import —
/// silently duplicating data. The bundles therefore use a key expression that
/// falls back to another column.</para>
/// </summary>
[Collection("DatabaseIntegration")]
public class DataBundleIdempotencyTests : IDisposable
{
    private readonly string _sandbox;

    public DataBundleIdempotencyTests()
    {
        _sandbox = Path.Combine(Path.GetTempPath(), "techtoon_idem_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_sandbox);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_sandbox)) Directory.Delete(_sandbox, true); } catch { }
    }

    [Fact]
    public void Reimporting_Items_With_Empty_Barcode_Does_Not_Duplicate()
    {
        if (!TestDb.Available) return;

        var svc = MasterDataBundleService.Create();
        var def = BundleCatalog.Find("items")!;
        var unique = Guid.NewGuid().ToString("N").Substring(0, 8);

        // صنفان: واحد بباركود وواحد بدون باركود (المفتاح الطبيعي NULL)
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
                    new() { ["name"] = "Barcode Item " + unique, ["barcode"] = "B" + unique, ["sell_price"] = "10" },
                    new() { ["name"] = "No Barcode Item " + unique, ["barcode"] = "", ["sell_price"] = "20" }
                },
                ["kit_components"] = new List<Dictionary<string, string?>>(),
                ["stock_levels"] = new List<Dictionary<string, string?>>()
            }
        };

        // 1) الاستيراد الأول
        var r1 = svc.Import(bundle, new BundleImportOptions { ConflictMode = BundleConflictMode.Update });
        Assert.True(r1.Success, r1.Message);
        Assert.Equal(2, r1.Inserted);

        var countAfterFirst = CountItems(unique);
        Assert.Equal(2, countAfterFirst);

        // 2) استيراد نفس الحزمة مرة أخرى — يجب ألا يضيف شيئًا
        var r2 = svc.Import(bundle, new BundleImportOptions { ConflictMode = BundleConflictMode.Update });
        Assert.True(r2.Success, r2.Message);
        var names = new List<string>();
        using (var c = TestDb.Open())
        using (var cmd = new Npgsql.NpgsqlCommand(
            "SELECT name || '|' || COALESCE(barcode,'<NULL>') FROM items WHERE name LIKE @u ORDER BY id", c))
        {
            cmd.Parameters.AddWithValue("@u", "%" + unique);
            using var r = cmd.ExecuteReader();
            while (r.Read()) names.Add(r.GetString(0));
        }
        Assert.True(r2.Inserted == 0,
            "Expected 0 inserted on re-import but got " + r2.Inserted + " | rows: " + string.Join(", ", names));
        Assert.Equal(2, r2.Updated);

        var countAfterSecond = CountItems(unique);
        Assert.Equal(2, countAfterSecond);   // لا تكرار

        // 3) ولا في وضع "تخطّي"
        var r3 = svc.Import(bundle, new BundleImportOptions { ConflictMode = BundleConflictMode.Skip });
        Assert.True(r3.Success, r3.Message);
        Assert.Equal(2, r3.Skipped);
        Assert.Equal(2, CountItems(unique));

        // ---- تنظيف ----
        TestDb.Execute("DELETE FROM items WHERE name LIKE @u", ("@u", "%" + unique));
    }

    [Fact]
    public void Items_Bundle_Has_A_Null_Safe_Key_Fallback()
    {
        var items = BundleCatalog.Find("items")!;
        var spec = items.Tables.First(t => t.Table == "items");
        Assert.NotEmpty(spec.KeyFallbacks);
        Assert.Contains("name", spec.KeyFallbacks);
    }

    [Fact]
    public void Every_Bundle_Fallback_References_A_Real_Column()
    {
        if (!TestDb.Available) return;

        var schema = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        using (var conn = TestDb.Open())
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
                if (!schema.TryGetValue(spec.Table, out var cols)) continue;
                foreach (var fb in spec.KeyFallbacks)
                {
                    if (string.IsNullOrEmpty(fb)) continue;
                    Assert.True(cols.Contains(fb!),
                        "عمود بديل غير موجود في " + spec.Table + ": " + fb);
                }
            }
    }

    private static int CountItems(string unique)
    {
        var v = TestDb.Scalar("SELECT COUNT(*) FROM items WHERE name LIKE @u", ("@u", "%" + unique));
        return v == null ? 0 : Convert.ToInt32(v);
    }
}
