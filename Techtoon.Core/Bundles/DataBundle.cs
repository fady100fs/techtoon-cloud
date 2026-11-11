using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Techtoon.Core.Bundles;

/// <summary>
/// حزمة بيانات (Bundle) — صيغة محايدة قابلة للنقل بين قواعد البيانات.
/// كل القيم نصوص (ISO للتواريخ، invariant للأرقام) ليبقى الملف قابلًا للتعديل يدويًا
/// أو الفتح في Excel.
/// </summary>
public class DataBundle
{
    /// <summary>رقم صيغة الحزمة — يُستخدم للتوافق المستقبلي.</summary>
    public string SchemaVersion { get; set; } = "1.0";

    /// <summary>معرّف المجمّع (parties / items / invoices / ...).</summary>
    public string BundleId { get; set; } = "";

    /// <summary>اسم المجمّع كما يظهر للمستخدم.</summary>
    public string Title { get; set; } = "";

    /// <summary>تاريخ التصدير.</summary>
    public DateTime ExportedAt { get; set; } = DateTime.Now;

    /// <summary>اسم قاعدة البيانات المصدر (تعليمي فقط).</summary>
    public string SourceDatabase { get; set; } = "";

    /// <summary>الجداول — كل جدول قائمة صفوف (كل صف قاموس عمود→قيمة نصية).</summary>
    public Dictionary<string, List<Dictionary<string, string?>>> Tables { get; set; } = new();

    /// <summary>عدد الصفوف لكل جدول.</summary>
    public Dictionary<string, int> RowCounts { get; set; } = new();

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string ToJson() => JsonSerializer.Serialize(this, JsonOpts);

    public static DataBundle FromJson(string json)
    {
        var bundle = JsonSerializer.Deserialize<DataBundle>(json, JsonOpts);
        if (bundle == null) throw new InvalidOperationException("ملف الحزمة غير صالح.");
        if (string.IsNullOrWhiteSpace(bundle.BundleId))
            throw new InvalidOperationException("ملف الحزمة لا يحتوي على معرّف مجمّع.");
        return bundle;
    }

    public static DataBundle TryFromJson(string json)
    {
        try { return FromJson(json); }
        catch { return null; }
    }

    /// <summary>إجمالي الصفوف في كل الجداول.</summary>
    public int TotalRows => Tables.Values.Sum(t => t.Count);
}

/// <summary>
/// وصف جدول داخل المجمّع: أعمدة التصدير + المفتاح الطبيعي + المراجع (FK).
/// </summary>
public class BundleTableSpec
{
    /// <summary>اسم الجدول في قاعدة البيانات.</summary>
    public string Table { get; set; } = "";

    /// <summary>الأعمدة المصدَّرة.</summary>
    public List<BundleColumn> Columns { get; set; } = new();

    /// <summary>الأعمدة التي تشكّل المفتاح الطبيعي (يُستخدم للمنعuny/تحديث الصفوف).</summary>
    public List<string> KeyColumns { get; set; } = new();

    /// <summary>المراجع: عمود FK ← الجدول الهدف + عمود المفتاح الطبيعي فيه.</summary>
    public List<BundleRef> References { get; set; } = new();

    /// <summary>عمود الترتيب (اختياري).</summary>
    public string? OrderBy { get; set; }

    /// <summary>شرط تصفية للتصدير (مثال: type = 'sales').</summary>
    public string? Where { get; set; }
}

/// <summary>عمود مُصدَّر.</summary>
public class BundleColumn
{
    /// <summary>اسم العمود في قاعدة البيانات.</summary>
    public string Db { get; set; } = "";

    /// <summary>اسم العمود داخل الملف (يمكن أن يختلف).</summary>
    public string Export { get; set; } = "";

    /// <summary>نوع البيانات: text | int | decimal | date | datetime | bool.</summary>
    public string Type { get; set; } = "text";

    /// <summary>إلزامي؟</summary>
    public bool Required { get; set; }

    /// <summary>قيمة افتراضية عند غياب العمود في الملف.</summary>
    public string? DefaultValue { get; set; }

    public string ExportName => string.IsNullOrEmpty(Export) ? Db : Export;
}

/// <summary>مرجع (علاقة) بين جدولين داخل المجمّع — يدعم المفتاح المركّب.</summary>
public class BundleRef
{
    /// <summary>عمود FK في الجدول المصدر.</summary>
    public string ForeignKey { get; set; } = "";

    /// <summary>الجدول الهدف.</summary>
    public string TargetTable { get; set; } = "";

    /// <summary>أعمدة المفتاح الطبيعي في الجدول الهدف (مطابقة позиقيًا لـ ExportKeys).</summary>
    public List<string> TargetKeys { get; set; } = new();

    /// <summary>أعمدة المفتاح في الملف (مطابقة позиقيًا لـ TargetKeys).</summary>
    public List<string> ExportKeys { get; set; } = new();

    /// <summary>هل المرجع إلزامي؟ (بلا قيمة = لا مرجع).</summary>
    public bool Required { get; set; }
}
