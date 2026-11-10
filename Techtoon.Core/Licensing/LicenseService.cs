using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Techtoon.Core.Licensing;

/// <summary>حالة الترخيص كما يعرضها البرنامج.</summary>
public enum LicenseState
{
    Valid,          // رخصة سارية
    Trial,          // يعمل في الفترة التجريبية
    Expired,        // انتهت المدة
    Invalid,        // ملف تالف أو توقيع غير مطابق
    MachineMismatch // الترخيص لجهاز آخر
}

/// <summary>أنواع الرخص المدعومة.</summary>
public static class LicenseTypes
{
    public const string Lifetime = "lifetime";
    public const string Yearly = "yearly";
    public const string Monthly = "monthly";
    public const string Trial = "trial";

    public static string Arabic(string type) => type switch
    {
        Lifetime => "رخصة مدى الحياة",
        Yearly => "رخصة سنوية",
        Monthly => "رخصة شهرية",
        Trial => "فترة تجريبية",
        _ => "غير معروف"
    };
}

/// <summary>معلومات الترخيص كما يقرؤها التطبيق.</summary>
public class LicenseInfo
{
    public LicenseState State { get; set; } = LicenseState.Invalid;
    public string LicenseType { get; set; } = "";
    public string Message { get; set; } = "";
    public int? DaysLeft { get; set; }
    public string MachineId { get; set; } = "";
    public string IssuedTo { get; set; } = "";
    public DateTime? ExpiresAt { get; set; }
    public bool IsAllowed => State == LicenseState.Valid || State == LicenseState.Trial;

    public string StateDisplay => State switch
    {
        LicenseState.Valid => "مرخّص",
        LicenseState.Trial => "تجريبي",
        LicenseState.Expired => "منتهي",
        LicenseState.Invalid => "غير صالح",
        LicenseState.MachineMismatch => "جهاز آخر",
        _ => ""
    };
}

/// <summary>
/// P5: نظام التراخيص — متوافق تمامًا مع أداة Python القائمة (license_manager.py):
/// الملف license.key بصيغة JSON، والتوقيع HMAC-SHA256 بنفس المفتاح.
/// يدعم: مدى الحياة / سنوي / شهري / تجريبي (10 أيام افتراضيًا).
/// </summary>
public static class LicenseService
{
    /// <summary>المفتاح السري للتوقيع — نفس قيمة license_manager.py (b"TECHTOON_LICENSE_2026_V1").</summary>
    public const string Secret = "TECHTOON_LICENSE_2026_V1";

    /// <summary>مدة الفترة التجريبية الافتراضية بالأيام (بدل 30 في أداة Python).</summary>
    public const int TrialDays = 10;

    public const string LicenseFileName = "license.key";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    // ==================== هوية الجهاز ====================

    /// <summary>معرّف الجهاز الأساسي (WMI) — نفس ترتيب أداة Python.</summary>
    public static string GetMachineId()
    {
        var raw = BuildWmiRaw();
        if (raw != null) return HashRaw(raw);
        return HashRaw(BuildFallbackRaw());
    }

    /// <summary>
    /// P5: معرّفات مقبولة للتحقق — يسمح بترخيص صادر من أداة Python على جهاز لا يتوفر فيه WMI
    /// (الذي يستخدم عندها احتياطي NODE|MAC) أو العكس. نضيف كل الصيغ (hex/عشرية/حالة الاسم).
    /// </summary>
    public static List<string> GetAcceptableMachineIds()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var wmi = BuildWmiRaw();
        if (wmi != null) set.Add(HashRaw(wmi));

        var node = Environment.MachineName ?? "";
        foreach (var mac in GetAllMacHex())
        {
            foreach (var n in CaseVariants(node))
            {
                foreach (var variant in MacVariants(mac))
                {
                    set.Add(HashRaw($"NODE:{n}|MAC:{variant}"));
                    set.Add(HashRaw($"NODE:{n}|MAC:{variant}".ToUpperInvariant()));
                }
            }
        }
        set.Add(HashRaw(BuildFallbackRaw()));
        return set.ToList();
    }

    /// <summary>صيغ اسم الجهاز (كما يرد من WMI/Registry وقد يختلف في الحالة عن Environment.MachineName).</summary>
    private static IEnumerable<string> CaseVariants(string name)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        if (!string.IsNullOrEmpty(name)) set.Add(name);
        set.Add(name.ToUpperInvariant());
        set.Add(name.ToLowerInvariant());
        if (!string.IsNullOrEmpty(name))
            set.Add(char.ToUpperInvariant(name[0]) + name.Substring(1).ToLowerInvariant());
        return set;
    }

    /// <summary>صيغ MAC الممكنة (hex، hex معكوس، عشري، عشري معكوس) — لتغطية اختلاف ترتيب الأدوات.</summary>
    private static List<string> MacVariants(string hex)
    {
        var list = new List<string> { hex };
        var bytes = ParseHex(hex);
        if (bytes.Length > 0)
        {
            var reversed = string.Join("", bytes.Reverse().Select(b => b.ToString("X2")));
            list.Add(reversed);
            list.Add(ToDecimal(bytes));
            list.Add(ToDecimal(bytes.Reverse().ToArray()));
        }
        return list;
    }

    private static string ToDecimal(byte[] bytes)
    {
        try
        {
            ulong v = 0;
            foreach (var b in bytes) v = (v << 8) | b;
            return v.ToString();
        }
        catch { return "0"; }
    }

    private static byte[] ParseHex(string hex)
    {
        try
        {
            if (hex.Length % 2 != 0) return Array.Empty<byte>();
            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            return bytes;
        }
        catch { return Array.Empty<byte>(); }
    }

    private static string? BuildWmiRaw()
    {
        var parts = new List<string>();
        var mb = GetWmi("Win32_BaseBoard", "SerialNumber");
        if (!string.IsNullOrWhiteSpace(mb)) parts.Add("MB:" + mb);
        var uuid = GetWmi("Win32_ComputerSystemProduct", "UUID");
        if (!string.IsNullOrWhiteSpace(uuid)) parts.Add("UUID:" + uuid);
        var bios = GetWmi("Win32_BIOS", "SerialNumber");
        if (!string.IsNullOrWhiteSpace(bios) && bios != mb && bios != uuid) parts.Add("BIOS:" + bios);
        if (parts.Count < 2) return null;
        return string.Join("|", parts).ToUpperInvariant();
    }

    private static string BuildFallbackRaw()
    {
        var parts = new List<string>
        {
            "NODE:" + (Environment.MachineName ?? ""),
            "MAC:" + GetAllMacHex().FirstOrDefault()
        };
        return string.Join("|", parts).ToUpperInvariant();
    }

    private static string HashRaw(string raw)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash)[..16].ToUpperInvariant();
    }

    private static List<string> GetAllMacHex()
    {
        var list = new List<string>();
        try
        {
            foreach (var nic in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                var bytes = nic.GetPhysicalAddress().GetAddressBytes();
                if (bytes.Length == 0) continue;
                var hex = string.Join("", bytes.Select(b => b.ToString("X2")));
                if (hex.Trim('0').Length == 0) continue;
                if (!list.Contains(hex)) list.Add(hex);
            }
        }
        catch { }
        if (list.Count == 0) list.Add("000000000000");
        return list;
    }

    private static string GetWmi(string wmiClass, string field)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher($"SELECT {field} FROM {wmiClass}");
            foreach (ManagementObject obj in searcher.Get())
            {
                var v = obj[field]?.ToString()?.Trim() ?? "";
                if (string.IsNullOrEmpty(v)) continue;
                if (v.Equals("To Be Filled By O.E.M.", StringComparison.OrdinalIgnoreCase)) continue;
                if (v.Equals("Default string", StringComparison.OrdinalIgnoreCase)) continue;
                if (v.Equals("None", StringComparison.OrdinalIgnoreCase)) continue;
                if (v.All(c => c == '0')) continue;
                return v.ToUpperInvariant();
            }
        }
        catch { }
        return "";
    }

    private static string GetMacHex()
    {
        try
        {
            var nics = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
            foreach (var nic in nics)
            {
                var mac = nic.GetPhysicalAddress();
                var bytes = mac.GetAddressBytes();
                if (bytes.Length == 0) continue;
                var hex = string.Join("", bytes.Select(b => b.ToString("X2")));
                if (hex.Trim('0').Length > 0) return hex;
            }
        }
        catch { }
        return "000000000000";
    }

    // ==================== التوقيع ====================

    /// <summary>يحسب توقيع HMAC-SHA256 للبيانات (بنفس ترتيب المفاتيح المستخدم في Python).</summary>
    public static string Sign(Dictionary<string, object?> data)
    {
        var payload = JsonSerializer.Serialize(OrderedDictionary(data), JsonOpts);
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }

    private static SortedDictionary<string, object?> OrderedDictionary(Dictionary<string, object?> data)
    {
        var ordered = new SortedDictionary<string, object?>(StringComparer.Ordinal);
        foreach (var kv in data) ordered[kv.Key] = kv.Value;
        return ordered;
    }

    public static bool VerifySignature(Dictionary<string, object?> data, string signature)
    {
        try
        {
            var payload = new Dictionary<string, object?>(data);
            payload.Remove("signature");
            var expected = Sign(payload);
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes((signature ?? "").ToLowerInvariant()));
        }
        catch { return false; }
    }

    // ==================== إصدار التراخيص ====================

    /// <summary>ينشئ ملف ترخيص موقّع (يستعمله المالك لإصدار الرخص).</summary>
    public static string CreateLicenseFile(string licenseType, string customer, int days = 0, string machineId = "", DateTime? expiresAt = null)
    {
        var data = new Dictionary<string, object?>
        {
            ["license_type"] = licenseType,
            ["issued_to"] = customer ?? "",
            ["machine_id"] = machineId ?? "",
            ["issued_at"] = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
            ["expires_at"] = (expiresAt ?? ComputeExpiry(licenseType, days)).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)
        };
        data["signature"] = Sign(data);
        return JsonSerializer.Serialize(data, JsonOpts);
    }

    public static DateTime ComputeExpiry(string licenseType, int days = 0)
    {
        var now = DateTime.Now;
        return licenseType switch
        {
            LicenseTypes.Lifetime => now.AddYears(50),
            LicenseTypes.Yearly => now.AddYears(1),
            LicenseTypes.Monthly => now.AddMonths(1),
            LicenseTypes.Trial => now.AddDays(days > 0 ? days : TrialDays),
            _ => now.AddDays(days > 0 ? days : TrialDays)
        };
    }

    // ==================== قراءة الحالة ====================

    /// <summary>مسار ملف الترخيص المتوقع: بجانب الـ EXE أو في مجلد بيانات المستخدم.</summary>
    public static string GetLicensePath()
    {
        var beside = Path.Combine(AppContext.BaseDirectory, LicenseFileName);
        if (File.Exists(beside)) return beside;
        var userDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TechtoonAccounting");
        var inUser = Path.Combine(userDir, LicenseFileName);
        if (File.Exists(inUser)) return inUser;
        return beside;
    }

    private static string GetTrialFilePath()
    {
        // P5: عداد التجربة يتبع النسخة الجديدة فقط (سياسة 10 أيام تبدأ من أول تشغيل لها).
        // ملف النسخة القديمة (Python) لا يؤثر — كان بسياسة 30 يومًا.
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TechtoonAccounting");
        return Path.Combine(dir, ".trial_start");
    }

    private static DateTime GetTrialStart()
    {
        var file = GetTrialFilePath();
        try
        {
            if (File.Exists(file))
            {
                var txt = File.ReadAllText(file).Trim();
                if (DateTime.TryParse(txt, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                    return parsed;
            }
        }
        catch { }
        var now = DateTime.Now;
        try
        {
            var dir = Path.GetDirectoryName(file);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(file, now.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture));
        }
        catch { }
        return now;
    }

    /// <summary>يقرأ حالة الترخيص الفعلية من القرص (مع احترام الفترة التجريبية).</summary>
    public static LicenseInfo GetStatus()
    {
        var info = new LicenseInfo { MachineId = GetMachineId() };
        var path = GetLicensePath();

        if (!File.Exists(path))
        {
            var start = GetTrialStart();
            var used = (DateTime.Now - start).Days;
            var left = TrialDays - used;
            info.MachineId = GetMachineId();
            if (left <= 0)
            {
                info.State = LicenseState.Expired;
                info.LicenseType = LicenseTypes.Trial;
                info.Message = $"انتهت الفترة التجريبية ({TrialDays} يوم). ضع ملف الترخيص license.key بجانب البرنامج.";
            }
            else
            {
                info.State = LicenseState.Trial;
                info.LicenseType = LicenseTypes.Trial;
                info.DaysLeft = left;
                info.ExpiresAt = start.AddDays(TrialDays);
                info.Message = $"نسخة تجريبية — متبقي {left} يوم من أصل {TrialDays}.";
            }
            return info;
        }

        try
        {
            var content = File.ReadAllText(path, Encoding.UTF8).Trim();
            var raw = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content);
            if (raw == null) throw new Exception("محتوى غير صالح");

            var data = raw.ToDictionary(k => k.Key, v => (object?)ConvertElement(v.Value));
            var sig = raw.TryGetValue("signature", out var s) ? s.GetString() ?? "" : "";
            if (!VerifySignature(data, sig))
            {
                info.State = LicenseState.Invalid;
                info.Message = "التوقيع الرقمي للترخيص غير صالح.";
                return info;
            }

            info.LicenseType = raw.TryGetValue("license_type", out var lt) ? lt.GetString() ?? "" : "";
            info.IssuedTo = raw.TryGetValue("issued_to", out var it) ? it.GetString() ?? "" : "";
            var midInFile = raw.TryGetValue("machine_id", out var mid) ? (mid.GetString() ?? "").Trim() : "";
            if (!string.IsNullOrEmpty(midInFile) && !GetAcceptableMachineIds().Contains(midInFile, StringComparer.OrdinalIgnoreCase))
            {
                info.State = LicenseState.MachineMismatch;
                info.Message = "هذا الترخيص مخصص لجهاز آخر. معرّف هذا الجهاز: " + info.MachineId;
                return info;
            }

            if (info.LicenseType == LicenseTypes.Lifetime)
            {
                info.State = LicenseState.Valid;
                info.Message = "رخصة مدى الحياة — " + (string.IsNullOrEmpty(info.IssuedTo) ? "" : info.IssuedTo);
                return info;
            }

            if (raw.TryGetValue("expires_at", out var exp) && exp.ValueKind == JsonValueKind.String)
            {
                var expText = exp.GetString();
                if (DateTime.TryParse(expText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var expDt))
                {
                    info.ExpiresAt = expDt;
                    if (DateTime.Now > expDt)
                    {
                        info.State = LicenseState.Expired;
                        info.Message = $"انتهت الرخصة بتاريخ {expDt:yyyy-MM-dd}.";
                        return info;
                    }
                    info.State = LicenseState.Valid;
                    info.DaysLeft = (int)Math.Ceiling((expDt - DateTime.Now).TotalDays);
                    info.Message = $"{LicenseTypes.Arabic(info.LicenseType)} — متبقي {info.DaysLeft} يوم.";
                    return info;
                }
            }

            info.State = LicenseState.Invalid;
            info.Message = "بيانات الرخصة ناقصة (لا يوجد تاريخ انتهاء).";
            return info;
        }
        catch (Exception ex)
        {
            info.State = LicenseState.Invalid;
            info.Message = "ملف الترخيص تالف: " + ex.Message;
            return info;
        }
    }

    private static object? ConvertElement(JsonElement el)
    {
        return el.ValueKind switch
        {
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Number => el.TryGetInt64(out var l) ? l : el.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => el.ToString()
        };
    }
}
