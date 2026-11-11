using System.Globalization;
using System.Text;
using Techtoon.Core.Licensing;

// أداة إصدار تراخيص Techtoon — نفس صيغة license_manager.py (متوافقة معها)
try { Console.OutputEncoding = Encoding.UTF8; } catch { }

if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
{
    Console.WriteLine(@"Techtoon Licensing Tool

Usage:
  machine-id                        Prints this machine identifier (diagnostics only)
  status                           Shows the license status of this installation
  generate --type <type> [--days N] [--customer ""name""] [--machine <ID>] [--out <file>]
  verify <file>                    Verifies a license file (signature, type, expiry)

License types:
  lifetime   Lifetime license
  yearly     One year (or --days 365)
  monthly    One month (or --days 30)
  trial      Trial period (--days N, default 10)

Notes:
  * Licenses are NOT bound to a machine by default (no --machine) and run on any PC.
  * --machine binds the license to one computer (optional).

Examples:
  Techtoon.LicenseTool generate --type yearly --customer ""Al Amal Co"" --out C:\license.key
  Techtoon.LicenseTool generate --type trial --days 10 --customer ""Trial""");
    return 0;
}

var cmd = args[0].ToLowerInvariant();

if (cmd == "machine-id")
{
    Console.WriteLine(LicenseService.GetMachineId());
    Console.WriteLine("--- All acceptable identifiers for this machine (diagnostics only) ---");
    foreach (var id in LicenseService.GetAcceptableMachineIds()) Console.WriteLine(id);
    Console.WriteLine();
    Console.WriteLine("Note: licenses are not machine-bound by default; this ID is for diagnostics only.");
    return 0;
}

if (cmd == "status")
{
    var info = LicenseService.GetStatus();
    Console.WriteLine("State      : " + info.StateDisplay + " (" + info.State + ")");
    Console.WriteLine("Type       : " + LicenseTypes.Arabic(info.LicenseType));
    Console.WriteLine("Issued to  : " + (string.IsNullOrEmpty(info.IssuedTo) ? "(none)" : info.IssuedTo));
    Console.WriteLine("Expires at : " + (info.ExpiresAt?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "(never)"));
    Console.WriteLine("Days left  : " + (info.DaysLeft?.ToString() ?? "-"));
    Console.WriteLine("Bound      : " + (info.IsMachineBound ? "yes (" + info.MachineId + ")" : "no (runs on any PC)"));
    Console.WriteLine("Message    : " + info.Message);
    Console.WriteLine("License path: " + LicenseService.GetLicensePath());
    return info.IsAllowed ? 0 : 1;
}

if (cmd == "verify")
{
    if (args.Length < 2) { Console.Error.WriteLine("File path is required."); return 2; }
    var path = args[1];
    if (!File.Exists(path)) { Console.Error.WriteLine("File not found: " + path); return 2; }

    LicenseInfo info;
    try
    {
        info = LicenseService.ParseLicenseFile(File.ReadAllText(path, Encoding.UTF8));
    }
    catch (Exception ex) { Console.Error.WriteLine("Invalid file: " + ex.Message); return 2; }

    Console.WriteLine("File        : " + Path.GetFullPath(path));
    Console.WriteLine("State       : " + info.StateDisplay + " (" + info.State + ")");
    Console.WriteLine("Type        : " + LicenseTypes.Arabic(info.LicenseType));
    Console.WriteLine("Issued to   : " + (string.IsNullOrEmpty(info.IssuedTo) ? "(none)" : info.IssuedTo));
    Console.WriteLine("Expires at  : " + (info.ExpiresAt?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "(never)"));
    Console.WriteLine("Days left   : " + (info.DaysLeft?.ToString() ?? "-"));
    Console.WriteLine("Machine bind: " + (info.IsMachineBound ? "yes" : "no (runs on any PC)"));
    Console.WriteLine("Message     : " + info.Message);
    return info.IsAllowed ? 0 : 1;
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

    if (!LicenseTypes.IsKnown(type))
    {
        Console.Error.WriteLine("Unknown license type: " + type);
        Console.Error.WriteLine("Valid types: " + string.Join(", ", LicenseTypes.All));
        return 2;
    }

    if (type == "trial" && days <= 0) days = LicenseService.TrialDays;
    if (type == "monthly" && days <= 0) days = 30;
    if (type == "yearly" && days <= 0) days = 365;
    if (type == "lifetime") days = 0;

    var expires = LicenseService.ComputeExpiry(type, days);
    var content = LicenseService.CreateLicenseFile(type, customer, days, machine, expires);

    var full = Path.GetFullPath(outPath);
    try
    {
        var dir = Path.GetDirectoryName(full);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(full, content, new UTF8Encoding(false));
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("Failed to write license: " + ex.Message);
        return 1;
    }

    // التحقق الذاتي: الملف المُنشأ يجب أن يمرّ بالتحقق قبل التسليم
    var check = LicenseService.ParseLicenseFile(content);
    if (!check.IsAllowed)
    {
        Console.Error.WriteLine("Generated license failed self-verification: " + check.Message);
        return 1;
    }

    Console.WriteLine("License created: " + full);
    Console.WriteLine("  Type       : " + LicenseTypes.Arabic(type));
    Console.WriteLine("  Customer   : " + (string.IsNullOrEmpty(customer) ? "(none)" : customer));
    Console.WriteLine("  Expires    : " + expires.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    Console.WriteLine("  Machine    : " + (string.IsNullOrEmpty(machine) ? "(not bound - runs on any PC)" : machine));
    Console.WriteLine("  Verified   : OK (" + check.StateDisplay + ")");
    return 0;
}

Console.Error.WriteLine("Unknown command: " + cmd);
return 2;
