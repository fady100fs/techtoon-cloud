using System.Globalization;
using Techtoon.Core.Licensing;

// أداة إصدار تراخيص Techtoon — نفس صيغة license_manager.py (متوافقة معها)
if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
{
    Console.WriteLine(@"أداة إصدار تراخيص Techtoon Accounting

الاستخدام:
  machine-id                        يعرض معرّف هذا الجهاز (أرسله للمالك لإصدار رخصة مرتبطة)
  generate --type <type> [--days N] [--customer ""name""] [--machine <ID>] [--out <file>]
  verify <file>                     يتحقق من صلاحية ملف ترخيص

أنواع الرخص:
  lifetime   رخصة مدى الحياة
  yearly     رخصة سنة (أو --days 365)
  monthly    رخصة شهر (أو --days 30)
  trial      فترة تجريبية (--days N، الافتراضي 10)

أمثلة:
  Techtoon.LicenseTool generate --type yearly --customer ""شركة الأمل"" --out C:\license.key
  Techtoon.LicenseTool generate --type trial --days 10 --customer ""تجريبي""");
    return 0;
}

var cmd = args[0].ToLowerInvariant();

if (cmd == "machine-id")
{
    Console.WriteLine(LicenseService.GetMachineId());
    Console.WriteLine("--- كل المعرّفات المقبولة لهذا الجهاز (استخدم أيًا منها في الترخيص) ---");
    foreach (var id in LicenseService.GetAcceptableMachineIds()) Console.WriteLine(id);
    return 0;
}

if (cmd == "verify")
{
    if (args.Length < 2) { Console.Error.WriteLine("المسار مطلوب"); return 2; }
    var content = File.ReadAllText(args[1]);
    var info = LicenseService.GetStatus();
    try
    {
        var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(content);
        var dict = data.ToDictionary(k => k.Key, v => (object?)(v.Value.ValueKind == System.Text.Json.JsonValueKind.String ? v.Value.GetString() : v.Value.ToString()));
        var sig = data.TryGetValue("signature", out var s) ? s.GetString() ?? "" : "";
        bool ok = LicenseService.VerifySignature(dict, sig);
        Console.WriteLine("التوقيع: " + (ok ? "صالح" : "غير صالح"));
        Console.WriteLine("معرّف الجهاز في الملف: " + (dict.TryGetValue("machine_id", out var m) ? m?.ToString() : ""));
        Console.WriteLine("معرّف هذا الجهاز     : " + info.MachineId);
        return ok ? 0 : 1;
    }
    catch (Exception ex) { Console.Error.WriteLine("ملف غير صالح: " + ex.Message); return 2; }
}

if (cmd == "generate")
{
    string type = "yearly";
    int days = 0;
    string customer = "";
    string machine = "";
    string outPath = "license.key";

    for (int i = 1; i < args.Length; i++)
    {
        switch (args[i].ToLowerInvariant())
        {
            case "--type": type = args[++i].ToLowerInvariant(); break;
            case "--days": int.TryParse(args[++i], out days); break;
            case "--customer": customer = args[++i]; break;
            case "--machine": machine = args[++i]; break;
            case "--out": outPath = args[++i]; break;
        }
    }

    if (type == "trial" && days <= 0) days = LicenseService.TrialDays;
    if (type == "monthly" && days <= 0) days = 30;
    if (type == "yearly" && days <= 0) days = 365;
    if (type == "lifetime") days = 0;

    var expires = LicenseService.ComputeExpiry(type, days);
    var content = LicenseService.CreateLicenseFile(type, customer, days, machine, expires);

    var full = Path.GetFullPath(outPath);
    File.WriteAllText(full, content, new System.Text.UTF8Encoding(false));
    Console.WriteLine("تم إنشاء الترخيص: " + full);
    Console.WriteLine("  النوع     : " + LicenseTypes.Arabic(type));
    Console.WriteLine("  للعميل    : " + customer);
    Console.WriteLine("  ينتهي في : " + expires.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    Console.WriteLine("  الجهاز    : " + (string.IsNullOrEmpty(machine) ? "(أي جهاز)" : machine));
    return 0;
}

Console.Error.WriteLine("أمر غير معروف: " + cmd);
return 2;
