using System;
using System.Collections.Generic;
using Npgsql;

namespace Techtoon.UI.Services
{
    public class WriteoffRequest
    {
        public int Id { get; set; }
        public string DocumentNumber { get; set; }
        public DateTime Date { get; set; }
        public int? DamagedItemId { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; }
        public double Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalValue { get; set; }
        public int? WriteoffReasonId { get; set; }
        public string WriteoffReasonName { get; set; }
        public string ProposedAction { get; set; } // writeoff, sell_as_is, return_supplier
        public string Notes { get; set; }
        public string Status { get; set; } // draft, pending_approval, approved, rejected, executed
        public int? RequestedBy { get; set; }
        public DateTime RequestedAt { get; set; }
        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public int? RejectedBy { get; set; }
        public DateTime? RejectedAt { get; set; }
        public string RejectionReason { get; set; }
        public int? ExecutedBy { get; set; }
        public DateTime? ExecutedAt { get; set; }
        public int? MovementId { get; set; }

        public string StatusDisplay { get { return Status switch { "draft" => "مسودة", "pending_approval" => "بانتظار الاعتماد", "approved" => "معتمد", "rejected" => "مرفوض", "executed" => "منفذ", _ => Status }; } }
        public string QuantityDisplay { get { return Quantity.ToString("0.00"); } }
        public string TotalValueDisplay { get { return TotalValue.ToString("N2"); } }
    }

    public static class WriteoffService
    {
        public static string GenerateDocumentNumber()
        {
            return "WO-" + DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        public static int Create(int? damagedItemId, int itemId, int warehouseId, double quantity, int? reasonId, string proposedAction, string notes, int userId)
        {
            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                decimal unitCost;
                using (var cmd = new NpgsqlCommand("SELECT COALESCE(avg_cost_price, cost_price, 0) FROM items WHERE id=@item", conn, tx))
                {
                    cmd.Parameters.AddWithValue("item", itemId);
                    unitCost = Convert.ToDecimal(cmd.ExecuteScalar() ?? 0);
                }

                int id;
                using (var cmd = new NpgsqlCommand(
                    "INSERT INTO writeoff_requests (document_number, date, damaged_item_id, item_id, warehouse_id, quantity, unit_cost, total_value, writeoff_reason_id, proposed_action, notes, status, requested_by, requested_at) " +
                    "VALUES (@num, NOW(), @dmg, @item, @wh, @qty, @cost, @val, @r, @action, @n, 'draft', @u, NOW()) RETURNING id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("num", GenerateDocumentNumber());
                    cmd.Parameters.AddWithValue("dmg", damagedItemId.HasValue ? (object)damagedItemId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("item", itemId);
                    cmd.Parameters.AddWithValue("wh", warehouseId);
                    cmd.Parameters.AddWithValue("qty", quantity);
                    cmd.Parameters.AddWithValue("cost", unitCost);
                    cmd.Parameters.AddWithValue("val", (decimal)quantity * unitCost);
                    cmd.Parameters.AddWithValue("r", reasonId.HasValue ? (object)reasonId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("action", proposedAction);
                    cmd.Parameters.AddWithValue("n", string.IsNullOrEmpty(notes) ? (object)DBNull.Value : notes);
                    cmd.Parameters.AddWithValue("u", userId);
                    id = Convert.ToInt32(cmd.ExecuteScalar());
                }

                tx.Commit();
                return id;
            }
            catch { tx.Rollback(); throw; }
        }

        public static void SubmitForApproval(int id)
        {
            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            conn.Open();
            using var cmd = new NpgsqlCommand("UPDATE writeoff_requests SET status='pending_approval' WHERE id=@id AND status='draft'", conn);
            cmd.Parameters.AddWithValue("id", id);
            var affected = cmd.ExecuteNonQuery();
            if (affected == 0) throw new InvalidOperationException("Cannot submit (not draft)");
        }

        public static void Approve(int id, int userId)
        {
            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            conn.Open();
            using var cmd = new NpgsqlCommand("UPDATE writeoff_requests SET status='approved', approved_by=@u, approved_at=NOW() WHERE id=@id AND status='pending_approval'", conn);
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("u", userId);
            var affected = cmd.ExecuteNonQuery();
            if (affected == 0) throw new InvalidOperationException("Cannot approve (not pending)");
        }

        public static void Reject(int id, int userId, string reason)
        {
            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            conn.Open();
            using var cmd = new NpgsqlCommand("UPDATE writeoff_requests SET status='rejected', rejected_by=@u, rejected_at=NOW(), rejection_reason=@r WHERE id=@id AND status='pending_approval'", conn);
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("u", userId);
            cmd.Parameters.AddWithValue("r", reason);
            cmd.ExecuteNonQuery();
        }

        public static void Execute(int id, int userId)
        {
            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                int itemId, warehouseId; double qty; decimal unitCost; string reason;
                using (var cmd = new NpgsqlCommand("SELECT item_id, warehouse_id, quantity, unit_cost, writeoff_reason_id, proposed_action FROM writeoff_requests WHERE id=@id AND status='approved'", conn, tx))
                {
                    cmd.Parameters.AddWithValue("id", id);
                    using var r = cmd.ExecuteReader();
                    if (!r.Read()) throw new InvalidOperationException("Not found or not approved");
                    itemId = r.GetInt32(0); warehouseId = r.GetInt32(1); qty = r.GetDouble(2); unitCost = (decimal)r.GetDouble(3);
                    reason = r.IsDBNull(4) ? "WO_OTHER" : r.GetInt32(4).ToString();
                }

                // إنشاء حركة مخزون (شطب = out)
                int movementId;
                using (var cmd = new NpgsqlCommand(
                    "INSERT INTO inventory_movements (item_id, date, type, quantity, reference_id, warehouse_id, reference_type, created_at, before_quantity, after_quantity, unit_cost, total_value, created_by, approved_by, reason, status, actual_date) " +
                    "VALUES (@item, NOW(), 'out', @qty, @woid, @wh, 'writeoff', NOW(), 0, 0, @cost, @val, @u, @u, 'Writeoff', 'approved', NOW()) RETURNING id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("item", itemId);
                    cmd.Parameters.AddWithValue("qty", qty);
                    cmd.Parameters.AddWithValue("woid", id);
                    cmd.Parameters.AddWithValue("wh", warehouseId);
                    cmd.Parameters.AddWithValue("cost", unitCost);
                    cmd.Parameters.AddWithValue("val", (decimal)qty * unitCost);
                    cmd.Parameters.AddWithValue("u", userId);
                    movementId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // تحديث stock_levels (نقص)
                using (var cmd = new NpgsqlCommand("UPDATE stock_levels SET quantity = GREATEST(0, quantity - @qty), last_updated=NOW() WHERE warehouse_id=@wh AND item_id=@item", conn, tx))
                {
                    cmd.Parameters.AddWithValue("qty", qty);
                    cmd.Parameters.AddWithValue("wh", warehouseId);
                    cmd.Parameters.AddWithValue("item", itemId);
                    cmd.ExecuteNonQuery();
                }

                // تحديث الطلب
                using (var cmd = new NpgsqlCommand("UPDATE writeoff_requests SET status='executed', executed_by=@u, executed_at=NOW(), movement_id=@mid WHERE id=@id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("mid", movementId);
                    cmd.Parameters.AddWithValue("u", userId);
                    cmd.Parameters.AddWithValue("id", id);
                    cmd.ExecuteNonQuery();
                }

                // تحديث حالة العنصر التالف المرتبط (إن وُجد)
                using (var cmd = new NpgsqlCommand("UPDATE damaged_items SET status='written_off' WHERE id=(SELECT damaged_item_id FROM writeoff_requests WHERE id=@id) AND damaged_item_id IS NOT NULL", conn, tx))
                {
                    cmd.Parameters.AddWithValue("id", id);
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        public static void Delete(int id)
        {
            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            conn.Open();
            using var cmd = new NpgsqlCommand("DELETE FROM writeoff_requests WHERE id=@id AND status IN ('draft', 'rejected')", conn);
            cmd.Parameters.AddWithValue("id", id);
            var affected = cmd.ExecuteNonQuery();
            if (affected == 0) throw new InvalidOperationException("Cannot delete (only draft/rejected)");
        }

        public static List<WriteoffRequest> GetAll(string statusFilter = null)
        {
            var list = new List<WriteoffRequest>();
            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            conn.Open();
            var sql = "SELECT w.id, w.document_number, w.date, w.damaged_item_id, w.item_id, i.name, w.warehouse_id, wh.name, " +
                      "w.quantity, w.unit_cost, w.total_value, w.writeoff_reason_id, sr.name, w.proposed_action, w.notes, " +
                      "w.status, w.requested_by, w.requested_at, w.approved_by, w.approved_at, w.rejected_by, w.rejected_at, w.rejection_reason, w.executed_by, w.executed_at, w.movement_id " +
                      "FROM writeoff_requests w " +
                      "LEFT JOIN items i ON w.item_id=i.id " +
                      "LEFT JOIN warehouses wh ON w.warehouse_id=wh.id " +
                      "LEFT JOIN stock_reasons sr ON w.writeoff_reason_id=sr.id ";
            if (!string.IsNullOrEmpty(statusFilter)) sql += "WHERE w.status=@s";
            sql += " ORDER BY w.id DESC";
            using var cmd = new NpgsqlCommand(sql, conn);
            if (!string.IsNullOrEmpty(statusFilter)) cmd.Parameters.AddWithValue("s", statusFilter);
            using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new WriteoffRequest
                    {
                        Id = r.GetInt32(0),
                        DocumentNumber = r.GetString(1),
                        Date = r.GetDateTime(2),
                        DamagedItemId = r.IsDBNull(3) ? (int?)null : r.GetInt32(3),
                        ItemId = r.GetInt32(4),
                        ItemName = r.IsDBNull(5) ? "" : r.GetString(5),
                        WarehouseId = r.GetInt32(6),
                        WarehouseName = r.IsDBNull(7) ? "" : r.GetString(7),
                        Quantity = r.GetDouble(8),
                        UnitCost = (decimal)r.GetDouble(9),
                        TotalValue = (decimal)r.GetDouble(10),
                        WriteoffReasonId = r.IsDBNull(11) ? (int?)null : r.GetInt32(11),
                        WriteoffReasonName = r.IsDBNull(12) ? "" : r.GetString(12),
                        ProposedAction = r.IsDBNull(13) ? "" : r.GetString(13),
                        Notes = r.IsDBNull(14) ? "" : r.GetString(14),
                        Status = r.GetString(15),
                        RequestedBy = r.IsDBNull(16) ? (int?)null : r.GetInt32(16),
                        RequestedAt = r.GetDateTime(17),
                        ApprovedBy = r.IsDBNull(18) ? (int?)null : r.GetInt32(18),
                        ApprovedAt = r.IsDBNull(19) ? (DateTime?)null : r.GetDateTime(19),
                        RejectedBy = r.IsDBNull(20) ? (int?)null : r.GetInt32(20),
                        RejectedAt = r.IsDBNull(21) ? (DateTime?)null : r.GetDateTime(21),
                        RejectionReason = r.IsDBNull(22) ? "" : r.GetString(22),
                        ExecutedBy = r.IsDBNull(23) ? (int?)null : r.GetInt32(23),
                        ExecutedAt = r.IsDBNull(24) ? (DateTime?)null : r.GetDateTime(24),
                        MovementId = r.IsDBNull(25) ? (int?)null : r.GetInt32(25)
                    });
                  }
              return list;
        }

        public static WriteoffRequest GetById(int id)
        {
            var all = GetAll();
            return all.Find(w => w.Id == id);
        }
    }
}