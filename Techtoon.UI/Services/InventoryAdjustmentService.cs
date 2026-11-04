using System;
using System.Collections.Generic;
using Npgsql;

namespace Techtoon.UI.Services
{
    public class StockAdjustment
    {
        public int Id { get; set; }
        public string DocumentNumber { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public int? WarehouseId { get; set; }
        public string WarehouseName { get; set; }
        public DateTime AdjustmentDate { get; set; }
        public double OldQuantity { get; set; }
        public double NewQuantity { get; set; }
        public double Difference { get; set; }
        public string Reason { get; set; }
        public string ReasonName { get; set; }
        public string Notes { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } // draft, pending, approved, rejected, executed
        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? ExecutedAt { get; set; }
        public int? MovementId { get; set; }
        public decimal UnitCost { get; set; }
        public decimal ValueBefore { get; set; }
        public decimal ValueAfter { get; set; }
        public string ReferenceDocumentType { get; set; }

        public string StatusDisplay { get { return Status switch { "draft" => "مسودة", "pending" => "بانتظار الاعتماد", "approved" => "معتمد", "rejected" => "مرفوض", "executed" => "منفذ", _ => Status }; } }
        public string DifferenceDisplay { get { return Difference.ToString("0.00"); } }
        public string OldQuantityDisplay { get { return OldQuantity.ToString("0.00"); } }
        public string NewQuantityDisplay { get { return NewQuantity.ToString("0.00"); } }
        public string ValueBeforeDisplay { get { return ValueBefore.ToString("N2"); } }
        public string ValueAfterDisplay { get { return ValueAfter.ToString("N2"); } }
    }

    public static class InventoryAdjustmentService
    {
        public static string GenerateDocumentNumber()
        {
            return "ADJ-" + DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        public static int CreateAdjustment(int itemId, int warehouseId, double newQuantity, string reasonCode, string notes, int userId)
        {
            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                // جلب الرصيد الحالي + التكلفة
                double oldQty; decimal unitCost;
                using (var cmd = new NpgsqlCommand(
                    "SELECT COALESCE(i.opening_quantity,0) + COALESCE((SELECT SUM(CASE WHEN type IN ('in','opening') THEN quantity WHEN type='out' THEN -quantity WHEN type='adjustment' THEN quantity ELSE 0 END) FROM inventory_movements WHERE item_id=i.id AND (warehouse_id=@wh OR warehouse_id IS NULL)),0), " +
                    "COALESCE(i.avg_cost_price, i.cost_price, 0) FROM items i WHERE i.id=@item", conn, tx))
                {
                    cmd.Parameters.AddWithValue("wh", warehouseId);
                    cmd.Parameters.AddWithValue("item", itemId);
                    using var r = cmd.ExecuteReader();
                    if (!r.Read()) throw new InvalidOperationException("Item not found");
                    oldQty = r.GetDouble(0);
                    unitCost = (decimal)r.GetDouble(1);
                }

                double difference = newQuantity - oldQty;
                decimal valueBefore = (decimal)oldQty * unitCost;
                decimal valueAfter = (decimal)newQuantity * unitCost;

                int adjId;
                using (var cmd = new NpgsqlCommand(
                    "INSERT INTO stock_adjustments (document_number, item_id, warehouse_id, adjustment_date, old_quantity, new_quantity, difference, reason, notes, created_by, created_at, status, unit_cost, value_before, value_after, reference_document_type) " +
                    "VALUES (@num, @item, @wh, NOW(), @old, @new, @diff, @reason, @n, @u, NOW(), 'draft', @cost, @vb, @va, 'manual') RETURNING id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("num", GenerateDocumentNumber());
                    cmd.Parameters.AddWithValue("item", itemId);
                    cmd.Parameters.AddWithValue("wh", warehouseId);
                    cmd.Parameters.AddWithValue("old", oldQty);
                    cmd.Parameters.AddWithValue("new", newQuantity);
                    cmd.Parameters.AddWithValue("diff", difference);
                    cmd.Parameters.AddWithValue("reason", reasonCode);
                    cmd.Parameters.AddWithValue("n", string.IsNullOrEmpty(notes) ? (object)DBNull.Value : notes);
                    cmd.Parameters.AddWithValue("u", userId);
                    cmd.Parameters.AddWithValue("cost", unitCost);
                    cmd.Parameters.AddWithValue("vb", valueBefore);
                    cmd.Parameters.AddWithValue("va", valueAfter);
                    adjId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                tx.Commit();
                return adjId;
            }
            catch { tx.Rollback(); throw; }
        }

        public static void SubmitForApproval(int adjId)
        {
            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            conn.Open();
            using var cmd = new NpgsqlCommand(
                "UPDATE stock_adjustments SET status='pending' WHERE id=@id AND status='draft'", conn);
            cmd.Parameters.AddWithValue("id", adjId);
            var affected = cmd.ExecuteNonQuery();
            if (affected == 0) throw new InvalidOperationException("Cannot submit (not draft)");
        }

        public static void Approve(int adjId, int userId)
        {
            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            conn.Open();
            using var cmd = new NpgsqlCommand(
                "UPDATE stock_adjustments SET status='approved', approved_by=@u, approved_at=NOW() WHERE id=@id AND status='pending'", conn);
            cmd.Parameters.AddWithValue("id", adjId);
            cmd.Parameters.AddWithValue("u", userId);
            var affected = cmd.ExecuteNonQuery();
            if (affected == 0) throw new InvalidOperationException("Cannot approve (not pending)");
        }

        public static void Reject(int adjId, int userId, string reason)
        {
            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            conn.Open();
            using var cmd = new NpgsqlCommand(
                "UPDATE stock_adjustments SET status='rejected', approved_by=@u, approved_at=NOW(), notes=COALESCE(notes,'') || '\nRejected: ' || @r WHERE id=@id AND status='pending'", conn);
            cmd.Parameters.AddWithValue("id", adjId);
            cmd.Parameters.AddWithValue("u", userId);
            cmd.Parameters.AddWithValue("r", reason);
            cmd.ExecuteNonQuery();
        }

        public static void Execute(int adjId, int userId)
        {
            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                // جلب بيانات التسوية
                int itemId, warehouseId; double oldQty, newQty, difference; decimal unitCost; string reason, notes;
                using (var cmd = new NpgsqlCommand(
                    "SELECT item_id, warehouse_id, old_quantity, new_quantity, difference, unit_cost, reason, notes FROM stock_adjustments WHERE id=@id AND status='approved'", conn, tx))
                {
                    cmd.Parameters.AddWithValue("id", adjId);
                    using var r = cmd.ExecuteReader();
                    if (!r.Read()) throw new InvalidOperationException("Adjustment not found or not approved");
                    itemId = r.GetInt32(0);
                    warehouseId = r.IsDBNull(1) ? 0 : r.GetInt32(1);
                    oldQty = r.GetDouble(2);
                    newQty = r.GetDouble(3);
                    difference = r.GetDouble(4);
                    unitCost = (decimal)r.GetDouble(5);
                    reason = r.GetString(6);
                    notes = r.IsDBNull(7) ? "" : r.GetString(7);
                }

                // إنشاء حركة مخزون
                int movementId;
                using (var cmd = new NpgsqlCommand(
                    "INSERT INTO inventory_movements (item_id, date, type, quantity, reference_id, warehouse_id, reference_type, created_at, before_quantity, after_quantity, unit_cost, total_value, created_by, approved_by, reason, status, actual_date) " +
                    "VALUES (@item, NOW(), 'adjustment', @qty, @adjId, @wh, 'adjustment', NOW(), @before, @after, @cost, @val, @u, @u, @reason, 'approved', NOW()) RETURNING id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("item", itemId);
                    cmd.Parameters.AddWithValue("qty", difference);
                    cmd.Parameters.AddWithValue("adjId", adjId);
                    cmd.Parameters.AddWithValue("wh", warehouseId == 0 ? (object)DBNull.Value : warehouseId);
                    cmd.Parameters.AddWithValue("before", oldQty);
                    cmd.Parameters.AddWithValue("after", newQty);
                    cmd.Parameters.AddWithValue("cost", unitCost);
                    cmd.Parameters.AddWithValue("val", difference * (double)unitCost);
                    cmd.Parameters.AddWithValue("u", userId);
                    cmd.Parameters.AddWithValue("reason", reason);
                    movementId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // تحديث stock_levels
                if (warehouseId > 0)
                {
                    using var cmd2 = new NpgsqlCommand(
                        "UPDATE stock_levels SET quantity=@qty, last_updated=NOW() WHERE warehouse_id=@wh AND item_id=@item", conn, tx);
                    cmd2.Parameters.AddWithValue("qty", newQty);
                    cmd2.Parameters.AddWithValue("wh", warehouseId);
                    cmd2.Parameters.AddWithValue("item", itemId);
                    var rows = cmd2.ExecuteNonQuery();
                    if (rows == 0)
                    {
                        using var cmd3 = new NpgsqlCommand(
                            "INSERT INTO stock_levels (warehouse_id, item_id, quantity, last_updated) VALUES (@wh, @item, @qty, NOW())", conn, tx);
                        cmd3.Parameters.AddWithValue("wh", warehouseId);
                        cmd3.Parameters.AddWithValue("item", itemId);
                        cmd3.Parameters.AddWithValue("qty", newQty);
                        cmd3.ExecuteNonQuery();
                    }
                }

                // تحديث التسوية
                using var cmd4 = new NpgsqlCommand(
                    "UPDATE stock_adjustments SET status='executed', executed_at=NOW(), movement_id=@mid WHERE id=@id", conn, tx);
                cmd4.Parameters.AddWithValue("mid", movementId);
                cmd4.Parameters.AddWithValue("id", adjId);
                cmd4.ExecuteNonQuery();

                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        public static List<StockAdjustment> GetAdjustments(string statusFilter = null)
        {
            var list = new List<StockAdjustment>();
            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            conn.Open();
            var sql = "SELECT a.id, a.document_number, a.item_id, i.name, a.warehouse_id, w.name, a.adjustment_date, " +
                      "a.old_quantity, a.new_quantity, a.difference, a.reason, sr.name, a.notes, a.created_by, a.created_at, " +
                      "a.status, a.approved_by, a.approved_at, a.executed_at, a.movement_id, a.unit_cost, a.value_before, a.value_after, a.reference_document_type " +
                      "FROM stock_adjustments a " +
                      "LEFT JOIN items i ON a.item_id=i.id " +
                      "LEFT JOIN warehouses w ON a.warehouse_id=w.id " +
                      "LEFT JOIN stock_reasons sr ON a.reason=sr.code ";
            if (!string.IsNullOrEmpty(statusFilter)) sql += "WHERE a.status=@s";
            sql += " ORDER BY a.id DESC";
            using var cmd = new NpgsqlCommand(sql, conn);
            if (!string.IsNullOrEmpty(statusFilter)) cmd.Parameters.AddWithValue("s", statusFilter);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new StockAdjustment
                {
                    Id = r.GetInt32(0),
                    DocumentNumber = r.GetString(1),
                    ItemId = r.GetInt32(2),
                    ItemName = r.IsDBNull(3) ? "" : r.GetString(3),
                    WarehouseId = r.IsDBNull(4) ? (int?)null : r.GetInt32(4),
                    WarehouseName = r.IsDBNull(5) ? "" : r.GetString(5),
                    AdjustmentDate = r.GetDateTime(6),
                    OldQuantity = r.GetDouble(7),
                    NewQuantity = r.GetDouble(8),
                    Difference = r.GetDouble(9),
                    Reason = r.IsDBNull(10) ? "" : r.GetString(10),
                    ReasonName = r.IsDBNull(11) ? "" : r.GetString(11),
                    Notes = r.IsDBNull(12) ? "" : r.GetString(12),
                    CreatedBy = r.IsDBNull(13) ? (int?)null : r.GetInt32(13),
                    CreatedAt = r.GetDateTime(14),
                    Status = r.GetString(15),
                    ApprovedBy = r.IsDBNull(16) ? (int?)null : r.GetInt32(16),
                    ApprovedAt = r.IsDBNull(17) ? (DateTime?)null : r.GetDateTime(17),
                    ExecutedAt = r.IsDBNull(18) ? (DateTime?)null : r.GetDateTime(18),
                    MovementId = r.IsDBNull(19) ? (int?)null : r.GetInt32(19),
                    UnitCost = (decimal)r.GetDouble(20),
                    ValueBefore = (decimal)r.GetDouble(21),
                    ValueAfter = (decimal)r.GetDouble(22),
                    ReferenceDocumentType = r.IsDBNull(23) ? "" : r.GetString(23)
                });
            }
            return list;
        }

        public static StockAdjustment GetById(int adjId)
        {
            var all = GetAdjustments();
            return all.Find(a => a.Id == adjId);
        }
        public static void DeleteAdjustment(int adjId)
        {
            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            conn.Open();
            using var cmd = new NpgsqlCommand("DELETE FROM stock_adjustments WHERE id=@id AND status IN ('draft', 'rejected')", conn);
            cmd.Parameters.AddWithValue("id", adjId);
            var affected = cmd.ExecuteNonQuery();
            if (affected == 0) throw new InvalidOperationException("Cannot delete (only draft/rejected can be deleted)");
        }
    }
}