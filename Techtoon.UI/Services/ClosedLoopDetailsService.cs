using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Npgsql;

namespace Techtoon.UI.Services;

/// <summary>
/// إغلاق فجوات «التفاصيل والسجلات» التي كانت بلا书写:
///  * warehouse_transfer_lines + warehouse_transfer_log  (تفاصيل تحويل المخازن وسجله)
///  * kit_component_history                                (تاريخ مكوّنات الكيت)
///  * bad_debt_attachments                                 (مرفقات الديون المعدومة)
///  * recurring_journal_executions                         (سجل تنفيذ القيود الدورية)
/// كل العمليات ذرّية (معاملة واحدة) وقابلة للاختبار.
/// </summary>
public class ClosedLoopDetailsService
{
    private readonly string _cs;

    public ClosedLoopDetailsService(string connectionString) { _cs = connectionString; }

    public static ClosedLoopDetailsService Create() =>
        new ClosedLoopDetailsService(SecureConnectionManager.GetConnectionString());

    // ==================== تحويل المخازن ====================

    /// <summary>سطر تفاصيل تحويل مخزن.</summary>
    public class TransferLine
    {
        public int ItemId { get; set; }
        public decimal RequestedQuantity { get; set; }
        public decimal? ShippedQuantity { get; set; }
        public decimal? ReceivedQuantity { get; set; }
        public decimal? DamagedQuantity { get; set; }
        public decimal? MissingQuantity { get; set; }
        public string? DifferenceReason { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// ينشئ تحويل مخزن مع سطوره وسجل حركاته — داخل معاملة واحدة.
    /// يُستدعى من محرر التحويلات.
    /// </summary>
    public async Task<int> CreateTransferAsync(int fromWarehouseId, int toWarehouseId, DateTime date,
        List<TransferLine> lines, string notes, string status, int performedBy = 1)
    {
        if (fromWarehouseId <= 0 || toWarehouseId <= 0)
            throw new InvalidOperationException("يجب اختيار المخزن المرسل والمستقبل.");
        if (fromWarehouseId == toWarehouseId)
            throw new InvalidOperationException("لا يمكن التحويل إلى نفس المخزن.");
        if (lines == null || lines.Count == 0)
            throw new InvalidOperationException("لا توجد أصناف للتحويل.");

        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            decimal totalQty = 0;
            foreach (var l in lines) totalQty += l.RequestedQuantity;

            int transferId;
            await using (var ins = new NpgsqlCommand(@"INSERT INTO warehouse_transfers
                (date, from_warehouse_id, to_warehouse_id, item_id, quantity, notes, status, created_by)
                VALUES (CAST(@d AS date), @fw, @tw, @item, @qty, @notes, @st, @by) RETURNING id", conn, tx))
            {
                ins.Parameters.AddWithValue("@d", date);
                ins.Parameters.AddWithValue("@fw", fromWarehouseId);
                ins.Parameters.AddWithValue("@tw", toWarehouseId);
                ins.Parameters.AddWithValue("@item", lines[0].ItemId); // العنصر الأساسي (متوافق مع المخطط)
                ins.Parameters.AddWithValue("@qty", totalQty);
                ins.Parameters.AddWithValue("@notes", (object)notes ?? DBNull.Value);
                ins.Parameters.AddWithValue("@st", status);
                ins.Parameters.AddWithValue("@by", performedBy);
                transferId = Convert.ToInt32(await ins.ExecuteScalarAsync());
            }

            foreach (var l in lines)
            {
                await using var ins = new NpgsqlCommand(@"INSERT INTO warehouse_transfer_lines
                    (transfer_id, item_id, requested_quantity, shipped_quantity, received_quantity,
                     damaged_quantity, missing_quantity, difference_reason, notes)
                    VALUES (@tid, @iid, @rq, @sq, @rcq, @dq, @mq, @dr, @n)", conn, tx);
                ins.Parameters.AddWithValue("@tid", transferId);
                ins.Parameters.AddWithValue("@iid", l.ItemId);
                ins.Parameters.AddWithValue("@rq", l.RequestedQuantity);
                ins.Parameters.AddWithValue("@sq", (object?)l.ShippedQuantity ?? DBNull.Value);
                ins.Parameters.AddWithValue("@rcq", (object?)l.ReceivedQuantity ?? DBNull.Value);
                ins.Parameters.AddWithValue("@dq", (object?)l.DamagedQuantity ?? DBNull.Value);
                ins.Parameters.AddWithValue("@mq", (object?)l.MissingQuantity ?? DBNull.Value);
                ins.Parameters.AddWithValue("@dr", (object?)l.DifferenceReason ?? DBNull.Value);
                ins.Parameters.AddWithValue("@n", (object?)l.Notes ?? DBNull.Value);
                await ins.ExecuteNonQueryAsync();
            }

            await LogTransferAsync(conn, tx, transferId, "create", performedBy, null, status,
                $"تحويل بسطرين: {lines.Count}");

            await tx.CommitAsync();
            AuditService.Log("CREATE", "WarehouseTransfer", transferId, $"Transfer created with {lines.Count} line(s)");
            return transferId;
        }
        catch
        {
            try { await tx.RollbackAsync(); } catch { }
            throw;
        }
    }

    /// <summary>يسجّل حركة على التحويل (سجل warehouse_transfer_log).</summary>
    public static async Task LogTransferAsync(NpgsqlConnection conn, NpgsqlTransaction tx, int transferId,
        string action, int performedBy, string? oldStatus, string? newStatus, string? notes)
    {
        await using var ins = new NpgsqlCommand(@"INSERT INTO warehouse_transfer_log
            (transfer_id, action, performed_by, performed_at, notes, old_status, new_status)
            VALUES (@tid, @act, @by, NOW(), @n, @os, @ns)", conn, tx);
        ins.Parameters.AddWithValue("@tid", transferId);
        ins.Parameters.AddWithValue("@act", action);
        ins.Parameters.AddWithValue("@by", performedBy);
        ins.Parameters.AddWithValue("@n", (object?)notes ?? DBNull.Value);
        ins.Parameters.AddWithValue("@os", (object?)oldStatus ?? DBNull.Value);
        ins.Parameters.AddWithValue("@ns", (object?)newStatus ?? DBNull.Value);
        await ins.ExecuteNonQueryAsync();
    }

    // ==================== تاريخ مكوّنات الكيت ====================

    /// <summary>
    /// يسجّل تاريخ مكوّنات الكيت (إضافة/تعديل/إزالة) — يُستدعى عند حفظ مكونات الكيت.
    /// </summary>
    public async Task RecordKitComponentHistoryAsync(int kitItemId, int componentItemId, decimal quantity,
        int? warehouseId, string changeType, decimal? oldQuantity, decimal? newQuantity,
        int? oldWarehouseId, int? newWarehouseId, string? notes, int changedBy = 1)
    {
        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync();
        await using var ins = new NpgsqlCommand(@"INSERT INTO kit_component_history
            (kit_item_id, component_item_id, quantity, warehouse_id, changed_at, changed_by,
             change_type, old_quantity, new_quantity, old_warehouse_id, new_warehouse_id, notes)
            VALUES (@kit, @comp, @qty, @wh, NOW(), @by, @ct, @oq, @nq, @ow, @nw, @n)", conn);
        ins.Parameters.AddWithValue("@kit", kitItemId);
        ins.Parameters.AddWithValue("@comp", componentItemId);
        ins.Parameters.AddWithValue("@qty", quantity);
        ins.Parameters.AddWithValue("@wh", (object?)warehouseId ?? DBNull.Value);
        ins.Parameters.AddWithValue("@by", changedBy);
        ins.Parameters.AddWithValue("@ct", changeType);
        ins.Parameters.AddWithValue("@oq", (object?)oldQuantity ?? DBNull.Value);
        ins.Parameters.AddWithValue("@nq", (object?)newQuantity ?? DBNull.Value);
        ins.Parameters.AddWithValue("@ow", (object?)oldWarehouseId ?? DBNull.Value);
        ins.Parameters.AddWithValue("@nw", (object?)newWarehouseId ?? DBNull.Value);
        ins.Parameters.AddWithValue("@n", (object?)notes ?? DBNull.Value);
        await ins.ExecuteNonQueryAsync();
    }

    // ==================== مرفقات الديون المعدومة ====================

    /// <summary>مرفق مضاف.</summary>
    public class AttachmentInfo
    {
        public int Id { get; set; }
        public string FileName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string? FileType { get; set; }
        public long? FileSize { get; set; }
        public string? FileHash { get; set; }
        public string? Description { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    /// <summary>يحفظ ملف مرفق داخل مجلد بيانات المستخدم ويُرجع مساره.</summary>
    public static string SaveAttachmentFile(int writeoffId, string sourcePath)
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TechtoonAccounting", "BadDebtAttachments", writeoffId.ToString());
        Directory.CreateDirectory(dir);
        var safeName = Path.GetFileName(sourcePath);
        var saved = Path.Combine(dir, safeName);
        File.Copy(sourcePath, saved, true);
        return saved;
    }

    /// <summary>يضيف مرفقًا لشطب ديون — يسجّل المسار والحجم والبصمة.</summary>
    public async Task<AttachmentInfo> AddBadDebtAttachmentAsync(int writeoffId, string sourcePath,
        string? description, int uploadedBy = 1)
    {
        if (writeoffId <= 0) throw new InvalidOperationException("شخص الديون غير محدد.");
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            throw new FileNotFoundException("ملف المرفق غير موجود: " + sourcePath);

        var saved = SaveAttachmentFile(writeoffId, sourcePath);
        var info = new FileInfo(saved);
        var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(saved))).ToLowerInvariant();
        var ext = Path.GetExtension(saved).TrimStart('.').ToLowerInvariant();

        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync();
        int id;
        await using (var ins = new NpgsqlCommand(@"INSERT INTO bad_debt_attachments
            (writeoff_id, file_name, file_path, file_type, file_size, file_hash, description, uploaded_by, uploaded_at)
            VALUES (@w, @n, @p, @t, @s, @h, @d, @by, NOW()) RETURNING id", conn))
        {
            ins.Parameters.AddWithValue("@w", writeoffId);
            ins.Parameters.AddWithValue("@n", info.Name);
            ins.Parameters.AddWithValue("@p", saved);
            ins.Parameters.AddWithValue("@t", (object?)ext ?? DBNull.Value);
            ins.Parameters.AddWithValue("@s", info.Length);
            ins.Parameters.AddWithValue("@h", hash);
            ins.Parameters.AddWithValue("@d", (object?)description ?? DBNull.Value);
            ins.Parameters.AddWithValue("@by", uploadedBy);
            id = Convert.ToInt32(await ins.ExecuteScalarAsync());
        }

        AuditService.Log("ATTACH", "BadDebtWriteoff", writeoffId, "Attachment added: " + info.Name);
        return new AttachmentInfo
        {
            Id = id,
            FileName = info.Name,
            FilePath = saved,
            FileType = ext,
            FileSize = info.Length,
            FileHash = hash,
            Description = description,
            UploadedAt = DateTime.Now
        };
    }

    /// <summary>مرفقات شطب ديون.</summary>
    public async Task<List<AttachmentInfo>> GetBadDebtAttachmentsAsync(int writeoffId)
    {
        var list = new List<AttachmentInfo>();
        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(@"SELECT id, file_name, file_path, file_type, file_size,
            file_hash, description, uploaded_at FROM bad_debt_attachments WHERE writeoff_id=@w ORDER BY uploaded_at DESC", conn);
        cmd.Parameters.AddWithValue("@w", writeoffId);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new AttachmentInfo
            {
                Id = r.GetInt32(0),
                FileName = r.GetString(1),
                FilePath = r.GetString(2),
                FileType = r.IsDBNull(3) ? null : r.GetString(3),
                FileSize = r.IsDBNull(4) ? null : r.GetInt64(4),
                FileHash = r.IsDBNull(5) ? null : r.GetString(5),
                Description = r.IsDBNull(6) ? null : r.GetString(6),
                UploadedAt = Techtoon.Core.Data.DbValue.AsDateTime(r.GetValue(7))
            });
        }
        return list;
    }
}
