using System;
using System.Collections.Generic;
using Npgsql;

namespace Techtoon.UI.Services
{
    public class StockCountSession
    {
        public int Id { get; set; }
        public string DocumentNumber { get; set; }
        public DateTime SessionDate { get; set; }
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; }
        public string CountType { get; set; } // full, partial, surprise
        public string CountTypeDisplay { get; set; }
        public string Status { get; set; } // draft, in_progress, under_review, approved, rejected, executed
        public string StatusDisplay { get; set; }
        public bool BookBalanceLocked { get; set; }
        public string Notes { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public int? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? ExecutedAt { get; set; }
        public int ItemsCount { get; set; }
        public int CountedItems { get; set; }
        public decimal TotalDifferenceValue { get; set; }
    }

    public class StockCountItem
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public string Barcode { get; set; }
        public double BookQuantity { get; set; }
        public double? CountedQuantity { get; set; }
        public double? RecountQuantity { get; set; }
        public double Difference { get; set; }
        public decimal DifferenceValue { get; set; }
        public decimal UnitCost { get; set; }
        public string Status { get; set; } // pending, counted, reviewed, approved
        public DateTime? CountedAt { get; set; }
        public string Notes { get; set; }

        public string BookQuantityDisplay { get { return BookQuantity.ToString("0.00"); } }
        public string CountedQuantityDisplay { get { return CountedQuantity.HasValue ? CountedQuantity.Value.ToString("0.00") : ""; } }
        public string DifferenceDisplay { get { return Difference.ToString("0.00"); } }
        public string DifferenceValueDisplay { get { return DifferenceValue.ToString("N2"); } }
    }

    public static class InventoryCountService
    {
        public static string GenerateDocumentNumber()
        {
            return "CNT-" + DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        public static int CreateSession(int warehouseId, string countType, bool bookBalanceLocked, string notes, int userId)
        {
            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                // 1) إنشاء الجلسة
                int sessionId;
                using (var cmd = new NpgsqlCommand(
                    "INSERT INTO stock_count_sessions (document_number, session_date, warehouse_id, count_type, status, book_balance_locked, notes, created_by, created_at) " +
                    "VALUES (@num, @date, @wh, @ct, 'draft', @bl, @n, @u, NOW()) RETURNING id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("num", GenerateDocumentNumber());
                    cmd.Parameters.AddWithValue("date", DateTime.Now);
                    cmd.Parameters.AddWithValue("wh", warehouseId);
                    cmd.Parameters.AddWithValue("ct", countType);
                    cmd.Parameters.AddWithValue("bl", bookBalanceLocked);
                    cmd.Parameters.AddWithValue("n", string.IsNullOrEmpty(notes) ? (object)DBNull.Value : notes);
                    cmd.Parameters.AddWithValue("u", userId);
                    sessionId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // 2) جلب الأصناف وأرصدتها الدفترية
                var items = new List<(int itemId, string name, string barcode, double bookQty, decimal unitCost)>();
                using (var cmd = new NpgsqlCommand(
                    "SELECT i.id, i.name, i.barcode, " +
                    "COALESCE(i.opening_quantity,0) + COALESCE((SELECT SUM(CASE WHEN type IN ('in','opening') THEN quantity WHEN type='out' THEN -quantity WHEN type='adjustment' THEN quantity ELSE 0 END) FROM inventory_movements WHERE item_id=i.id AND (warehouse_id=@wh OR warehouse_id IS NULL)),0), " +
                    "COALESCE(i.avg_cost_price, i.cost_price, 0) " +
                    "FROM items i WHERE i.is_active = TRUE AND i.is_kit IS NOT TRUE ORDER BY i.name", conn, tx))
                {
                    cmd.Parameters.AddWithValue("wh", warehouseId);
                    using var r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        items.Add((r.GetInt32(0), r.GetString(1), r.IsDBNull(2) ? "" : r.GetString(2), r.GetDouble(3), (decimal)r.GetDouble(4)));
                    }
                }

                // 3) إدراج عناصر الجرد
                foreach (var item in items)
                {
                    using var cmd = new NpgsqlCommand(
                        "INSERT INTO stock_counts (session_id, date, warehouse_id, item_id, system_quantity, book_quantity, counted_quantity, difference, difference_value, status, counted_by) " +
                        "VALUES (@sid, @date, @wh, @item, @sys, @book, NULL, 0, 0, 'pending', NULL)", conn, tx);
                    cmd.Parameters.AddWithValue("sid", sessionId);
                    cmd.Parameters.AddWithValue("date", DateTime.Now);
                    cmd.Parameters.AddWithValue("wh", warehouseId);
                    cmd.Parameters.AddWithValue("item", item.itemId);
                    cmd.Parameters.AddWithValue("sys", item.bookQty);
                    cmd.Parameters.AddWithValue("book", item.bookQty);
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
                return sessionId;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static void StartSession(int sessionId, int userId)
        {
            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            conn.Open();
            using var cmd = new NpgsqlCommand(
                "UPDATE stock_count_sessions SET status='in_progress', started_at=NOW() " +
                "WHERE id=@id AND status='draft'", conn);
            cmd.Parameters.AddWithValue("id", sessionId);
            var affected = cmd.ExecuteNonQuery();
            if (affected == 0) throw new InvalidOperationException("Cannot start session (not in draft status or already started)");
        }

        public static void SubmitForReview(int sessionId)
        {
            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            conn.Open();
            using var cmd = new NpgsqlCommand(
                "UPDATE stock_count_sessions SET status='under_review', submitted_at=NOW() " +
                "WHERE id=@id AND status='in_progress'", conn);
            cmd.Parameters.AddWithValue("id", sessionId);
            var affected = cmd.ExecuteNonQuery();
            if (affected == 0) throw new InvalidOperationException("Cannot submit (not in progress or already submitted)");
        }

        public static void ApproveSession(int sessionId, int userId)
        {
            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                // تحديث حالة الجلسة
                using (var cmd = new NpgsqlCommand(
                    "UPDATE stock_count_sessions SET status='approved', approved_by=@u, approved_at=NOW() " +
                    "WHERE id=@id AND status='under_review'", conn, tx))
                {
                    cmd.Parameters.AddWithValue("id", sessionId);
                    cmd.Parameters.AddWithValue("u", userId);
                    var affected = cmd.ExecuteNonQuery();
                    if (affected == 0) throw new InvalidOperationException("Cannot approve (not under review)");
                }

                // تحديث عناصر الجرد للحالة approved
                using var cmd2 = new NpgsqlCommand(
                    "UPDATE stock_counts SET status='approved' WHERE session_id=@sid", conn, tx);
                cmd2.Parameters.AddWithValue("sid", sessionId);
                cmd2.ExecuteNonQuery();

                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        public static void RejectSession(int sessionId, string reason)
        {
            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            conn.Open();
            using var cmd = new NpgsqlCommand(
                "UPDATE stock_count_sessions SET status='in_progress', notes=COALESCE(notes,'') || '\nRejected: ' || @r " +
                "WHERE id=@id AND status='under_review'", conn);
            cmd.Parameters.AddWithValue("id", sessionId);
            cmd.Parameters.AddWithValue("r", reason);
            var affected = cmd.ExecuteNonQuery();
            if (affected == 0) throw new InvalidOperationException("Cannot reject (not under review)");
        }

        public static void ExecuteSession(int sessionId, int userId)
        {
            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                // تحقق من الحالة
                string status;
                int warehouseId;
                using (var cmd = new NpgsqlCommand("SELECT status, warehouse_id FROM stock_count_sessions WHERE id=@id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("id", sessionId);
                    using var r = cmd.ExecuteReader();
                    if (!r.Read()) throw new InvalidOperationException("Session not found");
                    status = r.GetString(0);
                    warehouseId = r.GetInt32(1);
                }
                if (status != "approved") throw new InvalidOperationException("Session must be approved first");

                // جلب عناصر الجرد ذات الفروق
                var items = new List<(int itemId, double bookQty, double countedQty, double difference, decimal unitCost)>();
                using (var cmd = new NpgsqlCommand(
                    "SELECT sc.item_id, sc.book_quantity, sc.counted_quantity, " +
                    "COALESCE(sc.counted_quantity,0) - sc.book_quantity, " +
                    "COALESCE(i.avg_cost_price, i.cost_price, 0) " +
                    "FROM stock_counts sc JOIN items i ON sc.item_id=i.id " +
                    "WHERE sc.session_id=@sid AND sc.counted_quantity IS NOT NULL " +
                    "AND ABS(COALESCE(sc.counted_quantity,0) - sc.book_quantity) > 0.001", conn, tx))
                {
                    cmd.Parameters.AddWithValue("sid", sessionId);
                    using var r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        items.Add((r.GetInt32(0), r.GetDouble(1), r.GetDouble(2), r.GetDouble(3), (decimal)r.GetDouble(4)));
                    }
                }

                // إنشاء تسوية لكل فرق + حركة مخزون
                foreach (var item in items)
                {
                    // التسوية
                    int adjId;
                    using (var cmd = new NpgsqlCommand(
                        "INSERT INTO stock_adjustments (document_number, item_id, warehouse_id, adjustment_date, old_quantity, new_quantity, difference, reason, notes, created_by, created_at, status, approved_by, approved_at, executed_at, unit_cost, value_before, value_after, reference_document_type) " +
                        "VALUES (@num, @item, @wh, NOW(), @old, @new, @diff, @reason, @notes, @u, NOW(), 'executed', @u, NOW(), NOW(), @cost, @vb, @va, 'count') RETURNING id", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("num", "ADJ-" + DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + item.itemId);
                        cmd.Parameters.AddWithValue("item", item.itemId);
                        cmd.Parameters.AddWithValue("wh", warehouseId);
                        cmd.Parameters.AddWithValue("old", item.bookQty);
                        cmd.Parameters.AddWithValue("new", item.countedQty);
                        cmd.Parameters.AddWithValue("diff", item.difference);
                        cmd.Parameters.AddWithValue("reason", "ADJ_COUNT");
                        cmd.Parameters.AddWithValue("notes", "From stock count session #" + sessionId);
                        cmd.Parameters.AddWithValue("u", userId);
                        cmd.Parameters.AddWithValue("cost", item.unitCost);
                        cmd.Parameters.AddWithValue("vb", (decimal)item.bookQty * item.unitCost);
                        cmd.Parameters.AddWithValue("va", (decimal)item.countedQty * item.unitCost);
                        adjId = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    // حركة المخزون
                    using var cmd2 = new NpgsqlCommand(
                        "INSERT INTO inventory_movements (item_id, date, type, quantity, reference_id, warehouse_id, reference_type, created_at, before_quantity, after_quantity, unit_cost, total_value, created_by, approved_by, reason, status, actual_date) " +
                        "VALUES (@item, NOW(), 'adjustment', @qty, @adjId, @wh, 'adjustment', NOW(), @before, @after, @cost, @val, @u, @u, 'Stock count adjustment', 'approved', NOW())", conn, tx);
                    cmd2.Parameters.AddWithValue("item", item.itemId);
                    cmd2.Parameters.AddWithValue("qty", item.difference);
                    cmd2.Parameters.AddWithValue("adjId", adjId);
                    cmd2.Parameters.AddWithValue("wh", warehouseId);
                    cmd2.Parameters.AddWithValue("before", item.bookQty);
                    cmd2.Parameters.AddWithValue("after", item.countedQty);
                    cmd2.Parameters.AddWithValue("cost", item.unitCost);
                    cmd2.Parameters.AddWithValue("val", item.difference * (double)item.unitCost);
                    cmd2.Parameters.AddWithValue("u", userId);
                    cmd2.ExecuteNonQuery();

                    // تحديث stock_levels
                    using var cmd3 = new NpgsqlCommand(
                        "UPDATE stock_levels SET quantity=@qty, last_updated=NOW() WHERE warehouse_id=@wh AND item_id=@item", conn, tx);
                    cmd3.Parameters.AddWithValue("qty", item.countedQty);
                    cmd3.Parameters.AddWithValue("wh", warehouseId);
                    cmd3.Parameters.AddWithValue("item", item.itemId);
                    var rowsAffected = cmd3.ExecuteNonQuery();
                    if (rowsAffected == 0)
                    {
                        // إنشاء stock_level إن لم يكن موجودًا
                        using var cmd4 = new NpgsqlCommand(
                            "INSERT INTO stock_levels (warehouse_id, item_id, quantity, last_updated) VALUES (@wh, @item, @qty, NOW())", conn, tx);
                        cmd4.Parameters.AddWithValue("wh", warehouseId);
                        cmd4.Parameters.AddWithValue("item", item.itemId);
                        cmd4.Parameters.AddWithValue("qty", item.countedQty);
                        cmd4.ExecuteNonQuery();
                    }

                    // ربط التسوية بالحركة
                    using var cmd5 = new NpgsqlCommand(
                        "UPDATE stock_adjustments SET movement_id=(SELECT id FROM inventory_movements WHERE reference_id=@adjId AND reference_type='adjustment' ORDER BY id DESC LIMIT 1) WHERE id=@adjId", conn, tx);
                    cmd5.Parameters.AddWithValue("adjId", adjId);
                    cmd5.ExecuteNonQuery();
                }

                // تحديث حالة الجلسة
                using var cmd6 = new NpgsqlCommand(
                    "UPDATE stock_count_sessions SET status='executed', executed_at=NOW() WHERE id=@id", conn, tx);
                cmd6.Parameters.AddWithValue("id", sessionId);
                cmd6.ExecuteNonQuery();

                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        public static void UpdateCountedQuantity(int countItemId, double countedQty, int userId)
        {
            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                // جلب book_quantity و unit_cost
                double bookQty; decimal unitCost;
                using (var cmd = new NpgsqlCommand(
                    "SELECT sc.book_quantity, COALESCE(i.avg_cost_price, i.cost_price, 0) " +
                    "FROM stock_counts sc JOIN items i ON sc.item_id=i.id WHERE sc.id=@id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("id", countItemId);
                    using var r = cmd.ExecuteReader();
                    if (!r.Read()) throw new InvalidOperationException("Count item not found");
                    bookQty = r.GetDouble(0);
                    unitCost = (decimal)r.GetDouble(1);
                }

                double difference = countedQty - bookQty;
                decimal diffValue = (decimal)difference * unitCost;

                using var cmd2 = new NpgsqlCommand(
                    "UPDATE stock_counts SET counted_quantity=@qty, difference=@diff, difference_value=@val, counted_at=NOW(), status='counted' " +
                    "WHERE id=@id AND status IN ('pending','counted')", conn, tx);
                cmd2.Parameters.AddWithValue("qty", countedQty);
                cmd2.Parameters.AddWithValue("diff", difference);
                cmd2.Parameters.AddWithValue("val", diffValue);
                cmd2.Parameters.AddWithValue("id", countItemId);
                var affected = cmd2.ExecuteNonQuery();
                if (affected == 0) throw new InvalidOperationException("Cannot update (already reviewed or approved)");

                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        public static void RecountItem(int countItemId, double recountQty)
        {
            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            conn.Open();
            using var cmd = new NpgsqlCommand(
                "UPDATE stock_counts SET recount_quantity=@qty, status='pending' WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("qty", recountQty);
            cmd.Parameters.AddWithValue("id", countItemId);
            cmd.ExecuteNonQuery();
        }

        public static List<StockCountSession> GetSessions(string statusFilter = null)
        {
            var list = new List<StockCountSession>();
            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            conn.Open();
            var sql = "SELECT s.id, s.document_number, s.session_date, s.warehouse_id, w.name, s.count_type, s.status, s.book_balance_locked, " +
                      "s.notes, s.created_by, s.created_at, s.started_at, s.submitted_at, s.reviewed_by, s.reviewed_at, s.approved_by, s.approved_at, s.executed_at, " +
                      "COALESCE((SELECT COUNT(*) FROM stock_counts WHERE session_id=s.id),0), " +
                      "COALESCE((SELECT COUNT(*) FROM stock_counts WHERE session_id=s.id AND counted_quantity IS NOT NULL),0), " +
                      "COALESCE((SELECT SUM(difference_value) FROM stock_counts WHERE session_id=s.id),0) " +
                      "FROM stock_count_sessions s LEFT JOIN warehouses w ON s.warehouse_id=w.id";
            if (!string.IsNullOrEmpty(statusFilter)) sql += " WHERE s.status=@s";
            sql += " ORDER BY s.id DESC";
            using var cmd = new NpgsqlCommand(sql, conn);
            if (!string.IsNullOrEmpty(statusFilter)) cmd.Parameters.AddWithValue("s", statusFilter);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new StockCountSession
                {
                    Id = r.GetInt32(0),
                    DocumentNumber = r.GetString(1),
                    SessionDate = r.GetDateTime(2),
                    WarehouseId = r.GetInt32(3),
                    WarehouseName = r.IsDBNull(4) ? "" : r.GetString(4),
                    CountType = r.GetString(5),
                    Status = r.GetString(6),
                    BookBalanceLocked = r.GetBoolean(7),
                    Notes = r.IsDBNull(8) ? "" : r.GetString(8),
                    CreatedBy = r.IsDBNull(9) ? (int?)null : r.GetInt32(9),
                    CreatedAt = r.GetDateTime(10),
                    StartedAt = r.IsDBNull(11) ? (DateTime?)null : r.GetDateTime(11),
                    SubmittedAt = r.IsDBNull(12) ? (DateTime?)null : r.GetDateTime(12),
                    ReviewedBy = r.IsDBNull(13) ? (int?)null : r.GetInt32(13),
                    ReviewedAt = r.IsDBNull(14) ? (DateTime?)null : r.GetDateTime(14),
                    ApprovedBy = r.IsDBNull(15) ? (int?)null : r.GetInt32(15),
                    ApprovedAt = r.IsDBNull(16) ? (DateTime?)null : r.GetDateTime(16),
                    ExecutedAt = r.IsDBNull(17) ? (DateTime?)null : r.GetDateTime(17),
                    ItemsCount = r.GetInt32(18),
                    CountedItems = r.GetInt32(19),
                    TotalDifferenceValue = r.IsDBNull(20) ? 0 : (decimal)r.GetDouble(20)
                });
            }
            return list;
        }

        public static List<StockCountItem> GetSessionItems(int sessionId, bool hideBookBalance = false)
        {
            var list = new List<StockCountItem>();
            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            conn.Open();
            using var cmd = new NpgsqlCommand(
                "SELECT sc.id, sc.session_id, sc.item_id, i.name, i.barcode, sc.book_quantity, sc.counted_quantity, sc.recount_quantity, " +
                "sc.difference, sc.difference_value, COALESCE(i.avg_cost_price, i.cost_price, 0), sc.status, sc.counted_at, sc.notes " +
                "FROM stock_counts sc JOIN items i ON sc.item_id=i.id WHERE sc.session_id=@sid ORDER BY i.name", conn);
            cmd.Parameters.AddWithValue("sid", sessionId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new StockCountItem
                {
                    Id = r.GetInt32(0),
                    SessionId = r.GetInt32(1),
                    ItemId = r.GetInt32(2),
                    ItemName = r.GetString(3),
                    Barcode = r.IsDBNull(4) ? "" : r.GetString(4),
                    BookQuantity = hideBookBalance ? 0 : r.GetDouble(5),
                    CountedQuantity = r.IsDBNull(6) ? (double?)null : r.GetDouble(6),
                    RecountQuantity = r.IsDBNull(7) ? (double?)null : r.GetDouble(7),
                    Difference = hideBookBalance ? 0 : r.GetDouble(8),
                    DifferenceValue = hideBookBalance ? 0 : (decimal)r.GetDouble(9),
                    UnitCost = (decimal)r.GetDouble(10),
                    Status = r.GetString(11),
                    CountedAt = r.IsDBNull(12) ? (DateTime?)null : r.GetDateTime(12),
                    Notes = r.IsDBNull(13) ? "" : r.GetString(13)
                });
            }
            return list;
        }
        public static void DeleteSession(int sessionId)
        {
            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                // تحقق من الحالة (فقط draft أو in_progress يمكن حذفها)
                string status;
                using (var cmd = new NpgsqlCommand("SELECT status FROM stock_count_sessions WHERE id=@id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("id", sessionId);
                    var result = cmd.ExecuteScalar();
                    if (result == null) throw new InvalidOperationException("Session not found");
                    status = result.ToString();
                }
                if (status == "approved" || status == "executed")
                    throw new InvalidOperationException("Cannot delete approved/executed session");

                // حذف العناصر
                using (var cmd = new NpgsqlCommand("DELETE FROM stock_counts WHERE session_id=@sid", conn, tx))
                {
                    cmd.Parameters.AddWithValue("sid", sessionId);
                    cmd.ExecuteNonQuery();
                }
                // حذف الجلسة
                using (var cmd = new NpgsqlCommand("DELETE FROM stock_count_sessions WHERE id=@id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("id", sessionId);
                    cmd.ExecuteNonQuery();
                }
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }
    }
}