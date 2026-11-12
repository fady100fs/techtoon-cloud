using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

namespace Techtoon.Core.Bundles;

/// <summary>
/// تحويل الحزمة (Bundle) إلى ملف Excel (.xlsx) ومنه.
///
/// <para><b>تنسيق الملف:</b> ورقة لكل جدول — الصف الأول رؤوس الأعمدة (بأسماء أعمدة الملف)،
/// والصفوف التالية البيانات. تُضاف ورقة <c>_bundle</c> تحمل المعرّف والعنوان.</para>
///
/// <para><b>قابلية النقل:</b> المفاتيح تُكتب كنصوص (كود الحساب، باركود الصنف، اسم الطرف)،
/// فلا يعتمد الملف على أرقام قاعدة البيانات.</para>
/// </summary>
public static class DataBundleExcel
{
    /// <summary>اسم ورقة البيانات الوصفية.</summary>
    public const string MetaSheetName = "_bundle";

    /// <summary>يكتب الحزمة في ملف Excel.</summary>
    public static void Write(DataBundle bundle, string path)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        using var wb = new XLWorkbook();
        WriteTo(bundle, wb);
        wb.SaveAs(path);
    }

    /// <summary>يكتب الحزمة في أي تدفّق.</summary>
    public static void WriteTo(DataBundle bundle, Stream stream)
    {
        using var wb = new XLWorkbook();
        WriteTo(bundle, wb);
        wb.SaveAs(stream);
    }

    private static void WriteTo(DataBundle bundle, XLWorkbook wb)
    {
        foreach (var (table, rows) in bundle.Tables)
        {
            // اسم الورقة: اسم الجدول (يُقصّ لحد 31 محرفًا كما يطلب Excel)
            var sheetName = table.Length <= 31 ? table : table.Substring(0, 31);
            var ws = wb.AddWorksheet(sheetName);

            // الصف الأول: الرؤوس
            string[] headers = rows.Count > 0
                ? rows[0].Keys.ToArray()
                : new[] { "value" };
            for (int c = 0; c < headers.Length; c++)
                ws.Cell(1, c + 1).Value = headers[c];

            for (int r = 0; r < rows.Count; r++)
            {
                var row = rows[r];
                for (int c = 0; c < headers.Length; c++)
                {
                    var header = headers[c];
                    row.TryGetValue(header, out var value);
                    if (value == null) continue;
                    WriteCell(ws.Cell(r + 2, c + 1), value);
                }
            }

            ws.Row(1).Style.Font.Bold = true;
            ws.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F5E9");
            ws.SheetView.FreezeRows(1);
            ws.Columns().AdjustToContents(1, 40);
        }

        // ورقة البيانات الوصفية
        var meta = wb.AddWorksheet(MetaSheetName);
        meta.Cell(1, 1).Value = "bundle_id";
        meta.Cell(1, 2).Value = bundle.BundleId;
        meta.Cell(2, 1).Value = "title";
        meta.Cell(2, 2).Value = bundle.Title;
        meta.Cell(3, 1).Value = "schema_version";
        meta.Cell(3, 2).Value = bundle.SchemaVersion;
        meta.Cell(4, 1).Value = "exported_at";
        meta.Cell(4, 2).Value = bundle.ExportedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        meta.Cell(5, 1).Value = "rows";
        meta.Cell(5, 2).Value = bundle.TotalRows;
        meta.Columns().AdjustToContents();
    }

    /// <summary>يكتب القيمة في الخلية مع الحفاظ على نصها كما هو (دقة كاملة + استقلال عن اللغة).</summary>
    private static void WriteCell(IXLCell cell, string value)
    {
        // نترك كل القيم كنصوص (صيغة الخلية "@"): الأرقام والتواريخ تُقرأ لاحقًا حسب نوع العمود،
        // فلا تفقد الدقة ولا تتأثر بإعدادات اللغة/المنطقة في Excel.
        cell.Value = value;
        cell.Style.NumberFormat.Format = "@";
    }

    /// <summary>يقرأ الحزمة من ملف Excel.</summary>
    public static DataBundle Read(string path)
    {
        using var wb = new XLWorkbook(path);
        return ReadFrom(wb);
    }

    /// <summary>يقرأ الحزمة من أي تدفّق.</summary>
    public static DataBundle ReadFrom(Stream stream)
    {
        using var wb = new XLWorkbook(stream);
        return ReadFrom(wb);
    }

    /// <summary>يقرأ الحزمة من مصنّف Excel مفتوح.</summary>
    public static DataBundle ReadFrom(XLWorkbook wb)
    {
        var bundle = new DataBundle();

        // البيانات الوصفية (إن وُجدت)
        if (wb.Worksheets.TryGetWorksheet(MetaSheetName, out var meta))
        {
            string? Get(int row) => meta.Cell(row, 1).GetString();
            string? Val(int row) => meta.Cell(row, 2).GetString();
            var id = Val(1); if (!string.IsNullOrWhiteSpace(id)) bundle.BundleId = id!.Trim();
            var title = Val(2); if (!string.IsNullOrWhiteSpace(title)) bundle.Title = title!.Trim();
            var ver = Val(3); if (!string.IsNullOrWhiteSpace(ver)) bundle.SchemaVersion = ver!.Trim();
            var at = Val(4);
            if (DateTime.TryParse(at, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                bundle.ExportedAt = parsed;
        }

        foreach (var ws in wb.Worksheets)
        {
            if (ws.Name == MetaSheetName) continue;
            if (ws.Name.StartsWith("_", StringComparison.Ordinal)) continue;   // أوراق مساعدة

            var used = ws.RangeUsed();
            if (used == null) continue;

            int firstCol = used.FirstColumn().ColumnNumber();
            int lastCol = used.LastColumn().ColumnNumber();
            int firstRow = used.FirstRow().RowNumber();
            int lastRow = used.LastRow().RowNumber();

            // الرؤوس
            var headers = new List<string>();
            for (int c = firstCol; c <= lastCol; c++)
                headers.Add(ws.Cell(firstRow, c).GetString().Trim());

            var rows = new List<Dictionary<string, string?>>();
            for (int r = firstRow + 1; r <= lastRow; r++)
            {
                bool any = false;
                var row = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                for (int c = firstCol; c <= lastCol; c++)
                {
                    var cell = ws.Cell(r, c);
                    string? v = null;
                    if (!cell.IsEmpty())
                    {
                        v = cell.GetFormattedString();
                        if (string.IsNullOrWhiteSpace(v)) v = null;
                        else any = true;
                    }
                    row[headers[c - firstCol]] = v;
                }
                if (!any) continue;   // نتخطّى الصفوف الفارغة
                rows.Add(row);
            }

            bundle.Tables[ws.Name] = rows;
            bundle.RowCounts[ws.Name] = rows.Count;
        }

        return bundle;
    }

    /// <summary>يتحقق أن الملف قابل للقراءة كمجمّع معروف.</summary>
    public static bool LooksLikeBundle(string path)
    {
        try
        {
            using var wb = new XLWorkbook(path);
            var b = ReadFrom(wb);
            return !string.IsNullOrWhiteSpace(b.BundleId) && b.Tables.Count > 0;
        }
        catch { return false; }
    }
}
