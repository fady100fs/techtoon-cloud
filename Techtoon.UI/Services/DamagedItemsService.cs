using System;
using System.Collections.Generic;
using Npgsql;

namespace Techtoon.UI.Services
{
    public class DamagedItem
    {
        public int Id { get; set; }
        public string DocumentNumber { get; set; }
        public DateTime Date { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; }
        public double Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalValue { get; set; }
        public int? DamageReasonId { get; set; }
        public string DamageReasonName { get; set; }
        public string Notes { get; set; }
        public string InspectionStatus { get; set; } // pending, under_inspection, inspected
        public string InspectionResult { get; set; } // repair, return_supplier, back_to_stock, sell_as_is, writeoff
        public string InspectionNotes { get; set; }
        public int? InspectedBy { get; set; }
        public DateTime? InspectedAt { get; set; }
        public string Status { get; set; } // draft, under_inspection, inspected, writeoff_requested, written_off, returned, repaired
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        public string StatusDisplay { get { return Status switch { "draft" => "مسودة", "under_inspection" => "تحت الفحص", "inspected" => "تم الفحص", "writeoff_requested" => "طلب شطب", "written_off" => "مشطوب", "returned" => "مُرجع", "repaired" => "مُصلح", _ => Status }; } }
        public string QuantityDisplay { get { return Quantity.ToString("0.00"); } }
        public string TotalValueDisplay { get { return TotalValue.ToString("N2"); } }
    }

    public static class DamagedItemsService
    {
        public static string GenerateDocumentNumber()
        {
            return "DMG-" + DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        public static int Create(int itemId, int warehouseId, double quantity, int? reasonId, string notes, int userId)
        {
            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                // جلب unit_cost
                decimal unitCost;
                using (var cmd = new NpgsqlCommand("SELECT COALESCE(avg_cost_price, cost_price, 0) FROM items WHERE id=@item", conn, tx))
                {
                    cmd.Parameters.AddWithValue("item", itemId);
                    unitCost = Convert.ToDecimal(cmd.ExecuteScalar() ?? 0);
                }

                int id;
                using (var cmd = new NpgsqlCommand(
                    "INSERT INTO damaged_items (document_number, date, item_id, warehouse_id, quantity, unit_cost, total_value, damage_reason_id, notes, inspection_status, status, created_by, created_at) " +
                    "VALUES (@num, NOW(), @item, @wh, @qty, @cost, @val, @r, @n, 'pending', 'draft', @u, NOW()) RETURNING id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("num", GenerateDocumentNumber());
                    cmd.Parameters.AddWithValue("item", itemId);
                    cmd.Parameters.AddWithValue("wh", warehouseId);
                    cmd.Parameters.AddWithValue("qty", quantity);
                    cmd.Parameters.AddWithValue("cost", unitCost);
                    cmd.Parameters.AddWithValue("val", (decimal)quantity * unitCost);
                    cmd.Parameters.AddWithValue("r", reasonId.HasValue ? (object)reasonId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("n", string.IsNullOrEmpty(notes) ? (object)DBNull.Value : notes);
                    cmd.Parameters.AddWithValue("u", userId);
                    id = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // سجل الفحص
                using (var cmd = new NpgsqlCommand(
                    "INSERT INTO damaged_item_inspection_log (damaged_item_id, action, notes, performed_by, performed_at) VALUES (@id, 'created', 'تم إنشاء سجل التالف', @u, NOW())", conn, tx))
                {
                    cmd.Parameters.AddWithValue("id", id);
                    cmd.Parameters.AddWithValue("u", userId);
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
                return id;
            }
            catch { tx.Rollback(); throw; }
        }

        public static void StartInspection(int id, int userId)
        {
            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                using (var cmd = new NpgsqlCommand(
                    "UPDATE damaged_items SET inspection_status='under_inspection', status='under_inspection' WHERE id=@id AND status='draft'", conn, tx))
                {
                    cmd.Parameters.AddWithValue("id", id);
                    var affected = cmd.ExecuteNonQuery();
                    if (affected == 0) throw new InvalidOperationException("Cannot start inspection (not draft)");
                }
                using (var cmd = new NpgsqlCommand(
                    "INSERT INTO damaged_item_inspection_log (damaged_item_id, action, notes, performed_by, performed_at) VALUES (@id, 'inspection_started', NULL, @u, NOW())", conn, tx))
                {
                    cmd.Parameters.AddWithValue("id", id);
                    cmd.Parameters.AddWithValue("u", userId);
                    cmd.ExecuteNonQuery();
                }
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        public static void CompleteInspection(int id, string result, string notes, int userId)
        {
            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                string newStatus = result switch
                {
                    "writeoff" => "writeoff_requested",
                    "repair" => "repaired",
                    "return_supplier" => "returned",
                    "back_to_stock" => "inspected",
                    "sell_as_is" => "inspected",
                    _ => "inspected"
                };

                using (var cmd = new NpgsqlCommand(
                    "UPDATE damaged_items SET inspection_status='inspected', inspection_result=@r, inspection_notes=@n, inspected_by=@u, inspected_at=NOW(), status=@s WHERE id=@id AND status='under_inspection'", conn, tx))
                {
                    cmd.Parameters.AddWithValue("id", id);
                    cmd.Parameters.AddWithValue("r", result);
                    cmd.Parameters.AddWithValue("n", string.IsNullOrEmpty(notes) ? (object)DBNull.Value : notes);
                    cmd.Parameters.AddWithValue("u", userId);
                    cmd.Parameters.AddWithValue("s", newStatus);
                    var affected = cmd.ExecuteNonQuery();
                    if (affected == 0) throw new InvalidOperationException("Cannot complete inspection (not under inspection)");
                }

                using (var cmd = new NpgsqlCommand(
                    "INSERT INTO damaged_item_inspection_log (damaged_item_id, action, notes, performed_by, performed_at) VALUES (@id, 'inspection_completed', @n, @u, NOW())", conn, tx))
                {
                    cmd.Parameters.AddWithValue("id", id);
                    cmd.Parameters.AddWithValue("n", "Result: " + result + " - " + notes);
                    cmd.Parameters.AddWithValue("u", userId);
                    cmd.ExecuteNonQuery();
                }

                // إذا كان back_to_stock: إنشاء حركة مخزون لإرجاع الصنف
                if (result == "back_to_stock")
                {
                    int itemId, warehouseId; double qty; decimal unitCost;
                    using (var cmd = new NpgsqlCommand("SELECT item_id, warehouse_id, quantity, unit_cost FROM damaged_items WHERE id=@id", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("id", id);
                        using var r = cmd.ExecuteReader();
                        if (r.Read())
                        {
                            itemId = r.GetInt32(0); warehouseId = r.GetInt32(1); qty = r.GetDouble(2); unitCost = (decimal)r.GetDouble(3);
                        }
                        else throw new InvalidOperationException("Not found");
                    }

                    using (var cmd = new NpgsqlCommand(
                        "INSERT INTO inventory_movements (item_id, date, type, quantity, reference_id, warehouse_id, reference_type, created_at, before_quantity, after_quantity, unit_cost, total_value, created_by, approved_by, reason, status, actual_date) " +
                        "VALUES (@item, NOW(), 'adjustment', @qty, @dmgId, @wh, 'damaged_return', NOW(), 0, @qty, @cost, @val, @u, @u, 'Damaged item returned to stock', 'approved', NOW())", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("item", itemId);
                        cmd.Parameters.AddWithValue("qty", qty);
                        cmd.Parameters.AddWithValue("dmgId", id);
                        cmd.Parameters.AddWithValue("wh", warehouseId);
                        cmd.Parameters.AddWithValue("cost", unitCost);
                        cmd.Parameters.AddWithValue("val", (decimal)qty * unitCost);
                        cmd.Parameters.AddWithValue("u", userId);
                        cmd.ExecuteNonQuery();
                    }

                    using (var cmd = new NpgsqlCommand("UPDATE damaged_items SET status='returned' WHERE id=@id", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("id", id);
                        cmd.ExecuteNonQuery();
                    }
                }

                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        public static void Delete(int id)
        {
            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            conn.Open();
            using var cmd = new NpgsqlCommand("DELETE FROM damaged_items WHERE id=@id AND status='draft'", conn);
            cmd.Parameters.AddWithValue("id", id);
            var affected = cmd.ExecuteNonQuery();
            if (affected == 0) throw new InvalidOperationException("Cannot delete (only draft can be deleted)");
        }

        public static List<DamagedItem> GetAll(string statusFilter = null)
        {
            var list = new List<DamagedItem>();
            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            conn.Open();
            var sql = "SELECT d.id, d.document_number, d.date, d.item_id, i.name, d.warehouse_id, w.name, d.quantity, d.unit_cost, d.total_value, " +
                      "d.damage_reason_id, sr.name, d.notes, d.inspection_status, d.inspection_result, d.inspection_notes, d.inspected_by, d.inspected_at, " +
                      "d.status, d.created_by, d.created_at " +
                      "FROM damaged_items d " +
                      "LEFT JOIN items i ON d.item_id=i.id " +
                      "LEFT JOIN warehouses w ON d.warehouse_id=w.id " +
                      "LEFT JOIN stock_reasons sr ON d.damage_reason_id=sr.id ";
            if (!string.IsNullOrEmpty(statusFilter)) sql += "WHERE d.status=@s";
            sql += " ORDER BY d.id DESC";
            using var cmd = new NpgsqlCommand(sql, conn);
            if (!string.IsNullOrEmpty(statusFilter)) cmd.Parameters.AddWithValue("s", statusFilter);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new DamagedItem
                {
                    Id = r.GetInt32(0),
                    DocumentNumber = r.GetString(1),
                    Date = r.GetDateTime(2),
                    ItemId = r.GetInt32(3),
                    ItemName = r.IsDBNull(4) ? "" : r.GetString(4),
                    WarehouseId = r.GetInt32(5),
                    WarehouseName = r.IsDBNull(6) ? "" : r.GetString(6),
                    Quantity = r.GetDouble(7),
                    UnitCost = (decimal)r.GetDouble(8),
                    TotalValue = (decimal)r.GetDouble(9),
                    DamageReasonId = r.IsDBNull(10) ? (int?)null : r.GetInt32(10),
                    DamageReasonName = r.IsDBNull(11) ? "" : r.GetString(11),
                    Notes = r.IsDBNull(12) ? "" : r.GetString(12),
                    InspectionStatus = r.IsDBNull(13) ? "" : r.GetString(13),
                    InspectionResult = r.IsDBNull(14) ? "" : r.GetString(14),
                    InspectionNotes = r.IsDBNull(15) ? "" : r.GetString(15),
                    InspectedBy = r.IsDBNull(16) ? (int?)null : r.GetInt32(16),
                    InspectedAt = r.IsDBNull(17) ? (DateTime?)null : r.GetDateTime(17),
                    Status = r.GetString(18),
                    CreatedBy = r.IsDBNull(19) ? (int?)null : r.GetInt32(19),
                    CreatedAt = r.GetDateTime(20)
                });
            }
            return list;
        }

        public static DamagedItem GetById(int id)
        {
            var all = GetAll();
            return all.Find(d => d.Id == id);
        }
    }
}