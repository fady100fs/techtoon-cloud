using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Npgsql;
using Techtoon.Core.Bundles;

namespace Techtoon.UI.Services;

/// <summary>كيف نتعامل مع الصف الموجود مسبقًا.</summary>
public enum BundleConflictMode
{
    /// <summary>تخطّي الصف إن وُجد (آمن — لا يغيّر بيانات قائمة).</summary>
    Skip,
    /// <summary>تحديث الصف الموجود بقيم الملف.</summary>
    Update,
    /// <summary>إدراج دائمًا (قد ينتج تكرار).</summary>
    InsertAlways
}

/// <summary>خيارات الاستيراد.</summary>
public class BundleImportOptions
{
    public BundleConflictMode ConflictMode { get; set; } = BundleConflictMode.Skip;
    /// <summary>إنشاء السجلات المرجعية المفقودة تلقائيًا (عميل/صنف/حساب غير موجود).</summary>
    public bool CreateMissingReferences { get; set; } = true;
    /// <summary>تجربة بدون كتابة (معاينة).</summary>
    public bool DryRun { get; set; }
}

/// <summary>تقرير جدول داخل خطة الاستيراد.</summary>
public class BundleTablePlan
{
    public string Table { get; set; } = "";
    public int ToInsert { get; set; }
    public int ToUpdate { get; set; }
    public int ToSkip { get; set; }
    public List<string> Errors { get; } = new();
}

/// <summary>خطة الاستيراد (نتيجة المعاينة).</summary>
public class BundleImportPlan
{
    public string BundleId { get; set; } = "";
    public string Title { get; set; } = "";
    public int TotalRows { get; set; }
    public List<BundleTablePlan> Tables { get; } = new();
    public List<string> GlobalErrors { get; } = new();
    public List<string> Warnings { get; } = new();

    public int TotalInsert => Tables.Sum(t => t.ToInsert);
    public int TotalUpdate => Tables.Sum(t => t.ToUpdate);
    public int TotalSkip => Tables.Sum(t => t.ToSkip);
    public int TotalErrors => Tables.Sum(t => t.Errors.Count) + GlobalErrors.Count;
    public bool CanImport => TotalErrors == 0 && TotalRows > 0;
}

/// <summary>نتيجة الاستيراد الفعلية.</summary>
public class BundleImportResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public int Inserted { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public List<string> CreatedReferences { get; } = new();
    public List<string> Errors { get; } = new();
}

/// <summary>
/// B7-Data: خدمات استيراد/تصدير المجمّعات (Bundles).
///
/// <para><b>الفكرة:</b> بدل استيراد جدول واحد، نصدّر/نستورد <b>مجمّعًا</b> من الجداول
/// المترابطة (عملاء + حساباتهم، أصناف + كيتاتها، اشتراكات + خطوطها) في ملف واحد،
/// بمفاتيح طبيعية لا بأرقام — فينقل الملف بين أي قاعدتين.</para>
///
/// <para><b>السلامة:</b> الاستيراد ذرّي (معاملة واحدة — كله أو لا شيء)،
/// ومسبوق بمعاينة (تحليل) لا تكتب شيئًا، والمراجع المفقودة يمكن إنشاؤها تلقائيًا.</para>
/// </summary>
public class MasterDataBundleService
{
    private readonly string _cs;

    public MasterDataBundleService(string connectionString) { _cs = connectionString; }

    public static MasterDataBundleService Create() =>
        new MasterDataBundleService(SecureConnectionManager.GetConnectionString());

    /// <summary>
    /// أعمدة كل جدول كما هي في المخطط الفعلي — تُستعلم مرة واحدة.
    /// تُستخدم لتجاهل الأعمدة غير الموجودة (انحراف المخطط بين الإصدارات) بدل الفشل.
    /// </summary>
    private Dictionary<string, HashSet<string>>? _schemaColumns;

    private Dictionary<string, HashSet<string>> GetSchemaColumns(NpgsqlConnection conn)
    {
        if (_schemaColumns != null) return _schemaColumns;
        var map = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        using var cmd = new NpgsqlCommand(
            "SELECT table_name, column_name FROM information_schema.columns WHERE table_schema='public'", conn);
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            var t = r.GetString(0);
            var c = r.GetString(1);
            if (!map.TryGetValue(t, out var set))
            {
                set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                map[t] = set;
            }
            set.Add(c);
        }
        _schemaColumns = map;
        return map;
    }

    /// <summary>يعيد الأعمدة الموجودة فعلًا في المخطط (يتجاهل غير الموجود).</summary>
    private static List<BundleColumn> ExistingColumns(BundleTableSpec spec, Dictionary<string, HashSet<string>> schema)
    {
        if (!schema.TryGetValue(spec.Table, out var cols)) return new List<BundleColumn>();
        return spec.Columns.Where(c => cols.Contains(c.Db)).ToList();
    }

    // ==================================================================
    //  التصدير
    // ==================================================================

    /// <summary>يصدّر مجمّعًا كاملًا إلى كائن حزمة (بدون كتابة ملف).</summary>
    public DataBundle Export(BundleDefinition def)
    {
        var bundle = new DataBundle
        {
            BundleId = def.Id,
            Title = def.Title,
            ExportedAt = DateTime.Now,
            SourceDatabase = "techtoon"
        };

        using var conn = new NpgsqlConnection(_cs);
        conn.Open();

        foreach (var spec in def.Tables)
        {
            var rows = ExportTable(conn, def, spec);
            bundle.Tables[spec.Table] = rows;
            bundle.RowCounts[spec.Table] = rows.Count;
        }
        return bundle;
    }

    /// <summary>يصدّر مجمّعًا إلى ملف JSON.</summary>
    public string ExportToFile(BundleDefinition def, string path)
    {
        var bundle = Export(def);
        var dir = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir)) System.IO.Directory.CreateDirectory(dir);
        System.IO.File.WriteAllText(path, bundle.ToJson(), new UTF8Encoding(false));
        return System.IO.Path.GetFullPath(path);
    }

    private List<Dictionary<string, string?>> ExportTable(NpgsqlConnection conn, BundleDefinition def, BundleTableSpec spec)
    {
        var result = new List<Dictionary<string, string?>>();
        var schema = GetSchemaColumns(conn);
        var columns = ExistingColumns(spec, schema);
        if (columns.Count == 0) return result;

        var targetSpecs = def.Tables.ToDictionary(t => t.Table, t => t);

        // الأعمدة المصدرية
        var selectParts = new List<string>();
        foreach (var c in columns)
            selectParts.Add($"s.\"{c.Db}\" AS \"{c.Db}\"");

        // الوصلات لتحويل المفاتيح الخارجية إلى مفاتيح طبيعية
        var joins = new List<string>();
        for (int i = 0; i < spec.References.Count; i++)
        {
            var r = spec.References[i];
            if (!targetSpecs.ContainsKey(r.TargetTable)) continue;
            var alias = "ref" + i;
            joins.Add($"LEFT JOIN \"{r.TargetTable}\" {alias} ON {alias}.id = s.\"{r.ForeignKey}\"");
            for (int k = 0; k < r.TargetKeys.Count && k < r.ExportKeys.Count; k++)
            {
                // نتحقق أن عمود المفتاح موجود فعلًا في الجدول الهدف
                if (!schema.TryGetValue(r.TargetTable, out var tcols) || !tcols.Contains(r.TargetKeys[k])) continue;
                selectParts.Add($"{alias}.\"{r.TargetKeys[k]}\" AS \"{r.ExportKeys[k]}\"");
            }
        }

        var sql = $"SELECT {string.Join(", ", selectParts)} FROM \"{spec.Table}\" s";
        if (joins.Count > 0) sql += " " + string.Join(" ", joins);
        if (!string.IsNullOrWhiteSpace(spec.Where)) sql += " WHERE " + spec.Where;
        if (!string.IsNullOrWhiteSpace(spec.OrderBy)) sql += $" ORDER BY s.\"{spec.OrderBy}\"";

        using var cmd = new NpgsqlCommand(sql, conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var row = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var v = reader.GetValue(i);
                row[reader.GetName(i)] = v == DBNull.Value ? null : FormatValue(v);
            }
            result.Add(row);
        }
        return result;
    }

    private static string FormatValue(object v) => v switch
    {
        DateTime dt => dt.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
        DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        decimal m => m.ToString(CultureInfo.InvariantCulture),
        double d => d.ToString(CultureInfo.InvariantCulture),
        float f => f.ToString(CultureInfo.InvariantCulture),
        bool b => b ? "true" : "false",
        _ => Convert.ToString(v, CultureInfo.InvariantCulture) ?? ""
    };

    // ==================================================================
    //  المعاينة (بدون كتابة)
    // ==================================================================

    /// <summary>يحلّل الحزمة ويُعيد خطة الاستيراد — لا يكتب شيئًا.</summary>
    public BundleImportPlan Analyze(DataBundle bundle)
    {
        var plan = new BundleImportPlan { BundleId = bundle.BundleId, Title = bundle.Title };
        var def = BundleCatalog.Find(bundle.BundleId);
        if (def == null)
        {
            plan.GlobalErrors.Add("مجمّع غير معروف: '" + bundle.BundleId + "' — لن يتم الاستيراد.");
            return plan;
        }

        using var conn = new NpgsqlConnection(_cs);
        conn.Open();

        var ctx = new ImportContext(conn, def, bundle, plan);
        foreach (var spec in def.Tables)
        {
            if (!bundle.Tables.TryGetValue(spec.Table, out var rows)) continue;
            var tablePlan = new BundleTablePlan { Table = spec.Table };
            plan.Tables.Add(tablePlan);

            foreach (var row in rows)
            {
                plan.TotalRows++;
                try
                {
                    var action = ctx.ResolveRow(spec, row, tablePlan, dryRun: true);
                    switch (action)
                    {
                        case RowAction.Insert: tablePlan.ToInsert++; break;
                        case RowAction.Update: tablePlan.ToUpdate++; break;
                        case RowAction.Skip: tablePlan.ToSkip++; break;
                    }
                }
                catch (Exception ex)
                {
                    tablePlan.Errors.Add(ex.Message);
                }
            }
        }
        return plan;
    }

    // ==================================================================
    //  الاستيراد
    // ==================================================================

    /// <summary>يستورد الحزمة — ذرّي (معاملة واحدة).</summary>
    public BundleImportResult Import(DataBundle bundle, BundleImportOptions options)
    {
        var result = new BundleImportResult();
        var def = BundleCatalog.Find(bundle.BundleId);
        if (def == null)
        {
            result.Success = false;
            result.Message = "مجمّع غير معروف: '" + bundle.BundleId + "'.";
            return result;
        }

        using var conn = new NpgsqlConnection(_cs);
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            var plan = new BundleImportPlan { BundleId = bundle.BundleId, Title = bundle.Title };
            var ctx = new ImportContext(conn, def, bundle, plan, tx, options, result);

            foreach (var spec in def.Tables)
            {
                if (!bundle.Tables.TryGetValue(spec.Table, out var rows)) continue;
                var tablePlan = new BundleTablePlan { Table = spec.Table };
                plan.Tables.Add(tablePlan);

                foreach (var row in rows)
                {
                    try
                    {
                        var action = ctx.ResolveRow(spec, row, tablePlan, dryRun: false);
                        switch (action)
                        {
                            case RowAction.Insert: result.Inserted++; break;
                            case RowAction.Update: result.Updated++; break;
                            case RowAction.Skip: result.Skipped++; break;
                        }
                    }
                    catch (Exception ex)
                    {
                        tablePlan.Errors.Add(ex.Message);
                        result.Errors.Add(spec.Table + ": " + ex.Message);
                    }
                }
            }

            // أي خطأ → تراجع كامل (لا استيراد جزئي)
            if (result.Errors.Count > 0)
            {
                tx.Rollback();
                result.Success = false;
                result.Message = "فشل الاستيراد — تم التراجع عن كل التغييرات. الأخطاء: " + result.Errors.Count + ".";
                return result;
            }

            tx.Commit();
            result.Success = true;
            result.Message = $"تم الاستيراد: {result.Inserted} جديد، {result.Updated} تحديث، {result.Skipped} متخطّى"
                + (result.CreatedReferences.Count > 0 ? $". سجلات مرجعية أُنشئت: {result.CreatedReferences.Count}." : ".");
            AuditService.Log("IMPORT", "Bundle", 0, result.Message);
        }
        catch (Exception ex)
        {
            try { tx.Rollback(); } catch { }
            result.Success = false;
            result.Message = "فشل الاستيراد: " + ex.Message;
        }
        return result;
    }

    // ==================================================================
    //  سياق الاستيراد الداخلي
    // ==================================================================

    private enum RowAction { Insert, Update, Skip }

    /// <summary>يحتفظ بخرائط المفاتيح الطبيعية ← أرقام قاعدة البيانات أثناء الاستيراد.</summary>
    private sealed class ImportContext
    {
        private readonly NpgsqlConnection _conn;
        private readonly NpgsqlTransaction? _tx;
        private readonly BundleDefinition _def;
        private readonly DataBundle _bundle;
        private readonly BundleImportPlan _plan;
        private readonly BundleImportOptions _options;
        private readonly BundleImportResult _result;
        private readonly Dictionary<string, HashSet<string>> _schema;
        // key: "table|keyvalue" -> id
        private readonly Dictionary<string, int> _idCache = new(StringComparer.OrdinalIgnoreCase);

        public ImportContext(NpgsqlConnection conn, BundleDefinition def, DataBundle bundle, BundleImportPlan plan,
            NpgsqlTransaction? tx = null, BundleImportOptions? options = null, BundleImportResult? result = null)
        {
            _conn = conn;
            _tx = tx;
            _def = def;
            _bundle = bundle;
            _plan = plan;
            _options = options ?? new BundleImportOptions();
            _result = result ?? new BundleImportResult();

            _schema = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            using var cmd = new NpgsqlCommand(
                "SELECT table_name, column_name FROM information_schema.columns WHERE table_schema='public'", conn);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var t = r.GetString(0);
                var c = r.GetString(1);
                if (!_schema.TryGetValue(t, out var set))
                {
                    set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    _schema[t] = set;
                }
                set.Add(c);
            }
        }

        /// <summary>الأعمدة الموجودة فعلًا في المخطط (تُتجاهل غير الموجود).</summary>
        private List<BundleColumn> ColumnsOf(BundleTableSpec spec)
        {
            if (!_schema.TryGetValue(spec.Table, out var cols))
                return new List<BundleColumn>();
            return spec.Columns.Where(c => cols.Contains(c.Db)).ToList();
        }

        /// <summary>نوع العمود كما هو معرَّف في الكتالوج (افتراضي: نص).</summary>
        private string ColumnType(string table, string column)
        {
            var spec = _def.Tables.FirstOrDefault(t => t.Table == table);
            var col = spec?.Columns.FirstOrDefault(c => c.Db == column);
            return col?.Type ?? "text";
        }

        /// <summary>يحوّل قيمة المفتاح إلى نوع العمود الصحيح (int / decimal / date / text).</summary>
        private object KeyValue(string table, string column, string raw)
        {
            if (string.IsNullOrEmpty(raw)) return DBNull.Value;
            var type = ColumnType(table, column);
            if (type == "text") return raw;
            return ConvertValue(raw, type);
        }

        private NpgsqlCommand Cmd(string sql)
        {
            var cmd = new NpgsqlCommand(sql, _conn, _tx);
            return cmd;
        }

        /// <summary>يحلّل صفًا: يتحقق، ويحل المراجع، ويحدّد الإجراء وينفّذه.</summary>
        public RowAction ResolveRow(BundleTableSpec spec, Dictionary<string, string?> row,
            BundleTablePlan tablePlan, bool dryRun)
        {
            // 1) التحقق من الأعمدة الإلزامية
            var specColumns = ColumnsOf(spec);
            foreach (var c in specColumns)
            {
                if (!c.Required) continue;
                var v = GetValue(row, c);
                if (string.IsNullOrWhiteSpace(v))
                    throw new InvalidOperationException(
                        $"عمود إلزامي مفقود: '{c.Export}'" + (c.DefaultValue == null ? "" : " (لا يوجد قيمة افتراضية)"));
            }

            // 2) حل المراجع (FK)
            var resolved = new Dictionary<string, int?>();
            foreach (var r in spec.References)
            {
                var keyValues = new List<string>();
                foreach (var ek in r.ExportKeys)
                    keyValues.Add(GetValue(row, ek) ?? "");

                bool anyValue = keyValues.Any(v => !string.IsNullOrWhiteSpace(v));
                if (!anyValue)
                {
                    if (r.Required)
                        throw new InvalidOperationException("مرجع إلزامي مفقود: " + string.Join(" + ", r.ExportKeys));
                    resolved[r.ForeignKey] = null;
                    continue;
                }

                var id = ResolveReference(r, keyValues);
                if (!id.HasValue)
                {
                    if (!_options.CreateMissingReferences)
                        throw new InvalidOperationException(
                            "لم يُعثر على المرجع '" + string.Join(" + ", r.ExportKeys) + "' في " + r.TargetTable);
                    id = CreateMissingReference(r, keyValues, tablePlan, dryRun);
                    if (!id.HasValue)
                        throw new InvalidOperationException(
                            "تعذّر إنشاء المرجع '" + string.Join(" + ", r.ExportKeys) + "' في " + r.TargetTable);
                }
                resolved[r.ForeignKey] = id;
            }

            // 3) تحديد الإجراء عبر المفتاح الطبيعي
            var keyParts = new List<string>();
            foreach (var kc in spec.KeyColumns)
            {
                string v;
                if (resolved.TryGetValue(kc, out var rid))
                    v = rid.HasValue ? rid.Value.ToString(CultureInfo.InvariantCulture) : "";
                else
                {
                    var col = specColumns.FirstOrDefault(c => c.Db == kc);
                    v = col == null ? "" : (GetValue(row, col) ?? "");
                }
                keyParts.Add(kc + "=" + v);
            }
            var keyString = string.Join("|", keyParts);
            var existingId = FindByNaturalKey(spec, keyString, row, resolved);

            RowAction action;
            if (existingId.HasValue && _options.ConflictMode == BundleConflictMode.Skip)
                action = RowAction.Skip;
            else if (existingId.HasValue && _options.ConflictMode == BundleConflictMode.Update)
                action = RowAction.Update;
            else
                action = RowAction.Insert;

            if (dryRun) return action;

            // 4) التنفيذ
            if (action == RowAction.Update)
            {
                UpdateRow(spec, row, resolved, existingId!.Value);
            }
            else if (action == RowAction.Insert)
            {
                InsertRow(spec, row, resolved);
            }
            return action;
        }

        private static string? GetValue(Dictionary<string, string?> row, BundleColumn c)
        {
            if (row.TryGetValue(c.ExportName, out var v)) return v;
            if (row.TryGetValue(c.Db, out var v2)) return v2;
            return null;
        }

        private static string? GetValue(Dictionary<string, string?> row, string exportName)
        {
            if (row.TryGetValue(exportName, out var v)) return v;
            return null;
        }

        // ---------- البحث بالمفتاح الطبيعي ----------

        private int? FindByNaturalKey(BundleTableSpec spec, string keyString,
            Dictionary<string, string?> row, Dictionary<string, int?> resolved)
        {
            // نبني شرط WHERE من أعمدة المفتاح
            var where = new List<string>();
            var args = new List<object>();
            for (int i = 0; i < spec.KeyColumns.Count; i++)
            {
                var kc = spec.KeyColumns[i];
                string v;
                if (resolved.TryGetValue(kc, out var rid))
                    v = rid.HasValue ? rid.Value.ToString(CultureInfo.InvariantCulture) : "";
                else
                {
                    var col = ColumnsOf(spec).FirstOrDefault(c => c.Db == kc);
                    v = col == null ? "" : (GetValue(row, col) ?? "");
                }
                if (string.IsNullOrEmpty(v)) return null; // لا يمكن المطابقة — نعتبره جديدًا
                where.Add($"\"{kc}\" = @k{i}");
                args.Add(KeyValue(spec.Table, kc, v));
            }
            if (where.Count == 0) return null;

            var sql = $"SELECT id FROM \"{spec.Table}\" WHERE {string.Join(" AND ", where)} LIMIT 1";
            using var cmd = Cmd(sql);
            for (int i = 0; i < args.Count; i++) cmd.Parameters.AddWithValue("k" + i, args[i]);
            var v2 = cmd.ExecuteScalar();
            if (v2 == null || v2 == DBNull.Value) return null;
            return Convert.ToInt32(v2);
        }

        // ---------- حل المراجع ----------

        private int? ResolveReference(BundleRef r, List<string> keyValues)
        {
            var cacheKey = r.TargetTable + "|" + string.Join("|", keyValues);
            if (_idCache.TryGetValue(cacheKey, out var cached)) return cached;

            var targetSpec = _def.Tables.FirstOrDefault(t => t.Table == r.TargetTable);
            if (targetSpec == null) return null;

            var where = new List<string>();
            var args = new List<object>();
            for (int i = 0; i < r.TargetKeys.Count && i < keyValues.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(keyValues[i])) return null;
                where.Add($"\"{r.TargetKeys[i]}\" = @k{i}");
                args.Add(KeyValue(r.TargetTable, r.TargetKeys[i], keyValues[i]));
            }
            if (where.Count == 0) return null;

            using var cmd = Cmd($"SELECT id FROM \"{r.TargetTable}\" WHERE {string.Join(" AND ", where)} LIMIT 1");
            for (int i = 0; i < args.Count; i++) cmd.Parameters.AddWithValue("k" + i, args[i]);
            var v = cmd.ExecuteScalar();
            if (v == null || v == DBNull.Value) return null;

            var id = Convert.ToInt32(v);
            _idCache[cacheKey] = id;
            return id;
        }

        /// <summary>
        /// ينشئ سجلًا مرجعيًا مفقودًا (عميل/صنف/حساب...).
        /// في وضع المعاينة (dryRun) **لا يُكتب شيئًا** — يُسجَّل فقط ما سيُنشأ.
        /// </summary>
        private int? CreateMissingReference(BundleRef r, List<string> keyValues, BundleTablePlan tablePlan, bool dryRun)
        {
            var targetSpec = _def.Tables.FirstOrDefault(t => t.Table == r.TargetTable);
            if (targetSpec == null) return null;

            // القيم: المفتاح الطبيعي من الملفات + القيم الافتراضية
            var values = new Dictionary<string, string?>();
            for (int i = 0; i < r.TargetKeys.Count && i < keyValues.Count; i++)
                values[r.TargetKeys[i]] = keyValues[i];

            // الاسم: أول عمود "name" أو أول قيمة مفتاح
            var nameCol = targetSpec.Columns.FirstOrDefault(c => c.Db == "name");
            if (nameCol != null && !values.ContainsKey("name"))
                values["name"] = keyValues.FirstOrDefault() ?? "";

            // القيم الافتراضية (مع دعم الرمز $key لنسخ قيمة المفتاح)
            foreach (var c in ColumnsOf(targetSpec))
            {
                if (values.ContainsKey(c.Db)) continue;
                if (c.DefaultValue == null) continue;
                var dv = c.DefaultValue;
                for (int i = 0; i < keyValues.Count; i++)
                    dv = dv.Replace("$key" + i, keyValues[i]);
                dv = dv.Replace("$key", keyValues.FirstOrDefault() ?? "");
                values[c.Db] = dv;
            }

            // نحتاج كل الأعمدة الإلزامية
            foreach (var c in ColumnsOf(targetSpec))
            {
                if (!c.Required) continue;
                if (!values.ContainsKey(c.Db) || string.IsNullOrWhiteSpace(values[c.Db]))
                    return null; // لا يمكن الإنشاء تلقائيًا — يُبلَّغ عنه
            }

            var cacheKey = r.TargetTable + "|" + string.Join("|", keyValues);

            // معاينة: نُبلّغ فقط — بلا كتابة
            if (dryRun)
            {
                _result.CreatedReferences.Add(r.TargetTable + ": " + string.Join(" / ", keyValues));
                return -1; // مُعرّف مؤقت (لا يُستخدم في وضع المعاينة)
            }

            var cols = new List<string>();
            var names = new List<string>();
            var args = new List<object>();
            int idx = 0;
            foreach (var kv in values)
            {
                cols.Add($"\"{kv.Key}\"");
                names.Add("@p" + idx);
                args.Add(ConvertValue(kv.Value ?? "", "text"));
                idx++;
            }

            using var cmd = Cmd($"INSERT INTO \"{r.TargetTable}\" ({string.Join(", ", cols)}) VALUES ({string.Join(", ", names)}) RETURNING id");
            for (int i = 0; i < args.Count; i++) cmd.Parameters.AddWithValue("p" + i, args[i]);
            var id = Convert.ToInt32(cmd.ExecuteScalar());

            _idCache[cacheKey] = id;
            _result.CreatedReferences.Add(r.TargetTable + ": " + string.Join(" / ", keyValues));
            return id;
        }

        // ---------- الكتابة ----------

        private void InsertRow(BundleTableSpec spec, Dictionary<string, string?> row, Dictionary<string, int?> resolved)
        {
            var cols = new List<string>();
            var names = new List<string>();
            var args = new List<object>();
            int idx = 0;

            foreach (var c in ColumnsOf(spec))
            {
                if (resolved.TryGetValue(c.Db, out var rid))
                {
                    cols.Add($"\"{c.Db}\"");
                    names.Add("@p" + idx);
                    args.Add(rid.HasValue ? (object)rid.Value : DBNull.Value);
                    idx++;
                    continue;
                }
                var raw = GetValue(row, c);
                if (raw == null)
                {
                    if (c.DefaultValue == null) continue;
                    raw = c.DefaultValue;
                }
                cols.Add($"\"{c.Db}\"");
                names.Add("@p" + idx);
                args.Add(ConvertValue(raw, c.Type));
                idx++;
            }

            using var cmd = Cmd($"INSERT INTO \"{spec.Table}\" ({string.Join(", ", cols)}) VALUES ({string.Join(", ", names)})");
            for (int i = 0; i < args.Count; i++) cmd.Parameters.AddWithValue("p" + i, args[i]);
            cmd.ExecuteNonQuery();
        }

        private void UpdateRow(BundleTableSpec spec, Dictionary<string, string?> row,
            Dictionary<string, int?> resolved, int id)
        {
            var sets = new List<string>();
            var args = new List<object>();
            int idx = 0;

            foreach (var c in ColumnsOf(spec))
            {
                // لا نحدّث أعمدة المفتاح الطبيعي
                if (spec.KeyColumns.Contains(c.Db)) continue;

                if (resolved.TryGetValue(c.Db, out var rid))
                {
                    sets.Add($"\"{c.Db}\" = @p{idx}");
                    args.Add(rid.HasValue ? (object)rid.Value : DBNull.Value);
                    idx++;
                    continue;
                }
                var raw = GetValue(row, c);
                if (raw == null) continue; // لا نمسح القيم غير الموجودة في الملف
                sets.Add($"\"{c.Db}\" = @p{idx}");
                args.Add(ConvertValue(raw, c.Type));
                idx++;
            }
            if (sets.Count == 0) return;

            using var cmd = Cmd($"UPDATE \"{spec.Table}\" SET {string.Join(", ", sets)} WHERE id = @pid");
            for (int i = 0; i < args.Count; i++) cmd.Parameters.AddWithValue("p" + i, args[i]);
            cmd.Parameters.AddWithValue("pid", id);
            cmd.ExecuteNonQuery();
        }

        /// <summary>يحوّل نصًا إلى نوع العمود.</summary>
        private static object ConvertValue(string raw, string type)
        {
            if (raw == null) return DBNull.Value;
            var t = (type ?? "text").ToLowerInvariant();
            try
            {
                switch (t)
                {
                    case "int":
                        if (int.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var i)) return i;
                        if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var di)) return (int)di;
                        return DBNull.Value;
                    case "decimal":
                        if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)) return d;
                        return DBNull.Value;
                    case "bool":
                        if (bool.TryParse(raw, out var b)) return b;
                        if (raw == "1") return true;
                        if (raw == "0") return false;
                        return DBNull.Value;
                    case "date":
                        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture,
                                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt))
                            return dt.Date;
                        return DBNull.Value;
                    case "datetime":
                        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture,
                                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt2))
                            return DateTime.SpecifyKind(dt2, DateTimeKind.Unspecified);
                        return DBNull.Value;
                    default:
                        return raw;
                }
            }
            catch { return DBNull.Value; }
        }
    }
}
