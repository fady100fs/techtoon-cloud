using System;
using System.Collections.Generic;
using Npgsql;

namespace Techtoon.UI.Services
{
    public class DataPreviewItem
    {
        public string TableName { get; set; }
        public long RowCount { get; set; }
    }

    public static class DataManagementService
    {
        private static string GetCS() => SecureConnectionManager.GetConnectionString();

        // Helper: Execute scalar
        private static long GetCount(NpgsqlConnection conn, string table, string where = "")
        {
            try
            {
                using var cmd = new NpgsqlCommand($"SELECT COUNT(*) FROM {table} {where}", conn);
                return Convert.ToInt64(cmd.ExecuteScalar());
            }
            catch { return 0; }
        }

        // 1) PREVIEW METHODS
        public static List<DataPreviewItem> PreviewDeleteInvoices()
        {
            var list = new List<DataPreviewItem>();
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            list.Add(new DataPreviewItem { TableName = "invoices", RowCount = GetCount(conn, "invoices") });
            list.Add(new DataPreviewItem { TableName = "invoice_lines", RowCount = GetCount(conn, "invoice_lines") });
            list.Add(new DataPreviewItem { TableName = "invoice_payments", RowCount = GetCount(conn, "invoice_payments") });
            list.Add(new DataPreviewItem { TableName = "payments (related)", RowCount = GetCount(conn, "payments", "WHERE invoice_id IS NOT NULL") });
            list.Add(new DataPreviewItem { TableName = "inventory_movements (invoice)", RowCount = GetCount(conn, "inventory_movements", "WHERE reference_type='invoice'") });
            return list;
        }

        public static List<DataPreviewItem> PreviewClearInventory()
        {
            var list = new List<DataPreviewItem>();
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            list.Add(new DataPreviewItem { TableName = "inventory_movements", RowCount = GetCount(conn, "inventory_movements") });
            list.Add(new DataPreviewItem { TableName = "stock_levels", RowCount = GetCount(conn, "stock_levels") });
            list.Add(new DataPreviewItem { TableName = "items.opening_quantity", RowCount = GetCount(conn, "items", "WHERE opening_quantity > 0") });
            return list;
        }

        public static List<DataPreviewItem> PreviewDeleteStockOps()
        {
            var list = new List<DataPreviewItem>();
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            list.Add(new DataPreviewItem { TableName = "stock_count_sessions", RowCount = GetCount(conn, "stock_count_sessions") });
            list.Add(new DataPreviewItem { TableName = "stock_counts", RowCount = GetCount(conn, "stock_counts") });
            list.Add(new DataPreviewItem { TableName = "stock_adjustments", RowCount = GetCount(conn, "stock_adjustments") });
            list.Add(new DataPreviewItem { TableName = "damaged_items", RowCount = GetCount(conn, "damaged_items") });
            list.Add(new DataPreviewItem { TableName = "writeoff_requests", RowCount = GetCount(conn, "writeoff_requests") });
            list.Add(new DataPreviewItem { TableName = "damaged_item_inspection_log", RowCount = GetCount(conn, "damaged_item_inspection_log") });
            return list;
        }

        public static List<DataPreviewItem> PreviewDeleteFinance()
        {
            var list = new List<DataPreviewItem>();
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            list.Add(new DataPreviewItem { TableName = "payments", RowCount = GetCount(conn, "payments") });
            list.Add(new DataPreviewItem { TableName = "expenses", RowCount = GetCount(conn, "expenses") });
            list.Add(new DataPreviewItem { TableName = "cash_transfers", RowCount = GetCount(conn, "cash_transfers") });
            list.Add(new DataPreviewItem { TableName = "journal_entries", RowCount = GetCount(conn, "journal_entries") });
            list.Add(new DataPreviewItem { TableName = "journal_lines", RowCount = GetCount(conn, "journal_lines") });
            return list;
        }

        // 2) EXECUTE METHODS (Safe Deletion)
        public static void ExecuteDeleteInvoices()
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                // Order matters: details first, then main
                ExecuteNonQuery(tx, "DELETE FROM invoice_lines");
                ExecuteNonQuery(tx, "DELETE FROM invoice_payments");
                ExecuteNonQuery(tx, "DELETE FROM payments WHERE invoice_id IS NOT NULL");
                ExecuteNonQuery(tx, "DELETE FROM inventory_movements WHERE reference_type='invoice'");
                ExecuteNonQuery(tx, "DELETE FROM invoices");
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        public static void ExecuteClearInventory()
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                ExecuteNonQuery(tx, "DELETE FROM inventory_movements");
                ExecuteNonQuery(tx, "UPDATE stock_levels SET quantity=0, reserved_quantity=0, damaged_quantity=0, under_inspection_quantity=0");
                ExecuteNonQuery(tx, "UPDATE items SET opening_quantity=0");
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        public static void ExecuteDeleteStockOps()
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                ExecuteNonQuery(tx, "DELETE FROM damaged_item_inspection_log");
                ExecuteNonQuery(tx, "DELETE FROM writeoff_requests");
                ExecuteNonQuery(tx, "DELETE FROM damaged_items");
                ExecuteNonQuery(tx, "DELETE FROM stock_counts");
                ExecuteNonQuery(tx, "DELETE FROM stock_count_sessions");
                ExecuteNonQuery(tx, "DELETE FROM stock_adjustments");
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        public static void ExecuteDeleteFinance()
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                ExecuteNonQuery(tx, "DELETE FROM journal_lines");
                ExecuteNonQuery(tx, "DELETE FROM journal_entries");
                ExecuteNonQuery(tx, "DELETE FROM cash_transfers");
                ExecuteNonQuery(tx, "DELETE FROM expenses");
                ExecuteNonQuery(tx, "DELETE FROM payments");
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        // 3) FACTORY RESET
        public static void ExecuteFactoryReset(bool keepUsersAndRoles)
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                // Delete all operational data
                ExecuteDeleteStockOpsInternal(tx);
                ExecuteDeleteFinanceInternal(tx);
                ExecuteDeleteInvoicesInternal(tx);
                ExecuteClearInventoryInternal(tx);
                ExecuteNonQuery(tx, "DELETE FROM warehouse_transfers");

                if (!keepUsersAndRoles)
                {
                    // Keep only the main admin
                    ExecuteNonQuery(tx, "DELETE FROM user_permissions WHERE user_id != 1");
                    ExecuteNonQuery(tx, "DELETE FROM users WHERE id != 1");
                }

                // Reset sequences (optional but good for "new start")
                ResetSequence(tx, "invoices_id_seq");
                ResetSequence(tx, "payments_id_seq");
                ResetSequence(tx, "inventory_movements_id_seq");

                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        // Internal helpers for Factory Reset
        private static void ExecuteDeleteStockOpsInternal(NpgsqlTransaction tx)
        {
            ExecuteNonQuery(tx, "DELETE FROM damaged_item_inspection_log");
            ExecuteNonQuery(tx, "DELETE FROM writeoff_requests");
            ExecuteNonQuery(tx, "DELETE FROM damaged_items");
            ExecuteNonQuery(tx, "DELETE FROM stock_counts");
            ExecuteNonQuery(tx, "DELETE FROM stock_count_sessions");
            ExecuteNonQuery(tx, "DELETE FROM stock_adjustments");
        }
        private static void ExecuteDeleteFinanceInternal(NpgsqlTransaction tx)
        {
            ExecuteNonQuery(tx, "DELETE FROM journal_lines");
            ExecuteNonQuery(tx, "DELETE FROM journal_entries");
            ExecuteNonQuery(tx, "DELETE FROM cash_transfers");
            ExecuteNonQuery(tx, "DELETE FROM expenses");
            ExecuteNonQuery(tx, "DELETE FROM payments");
        }
        private static void ExecuteDeleteInvoicesInternal(NpgsqlTransaction tx)
        {
            ExecuteNonQuery(tx, "DELETE FROM invoice_lines");
            ExecuteNonQuery(tx, "DELETE FROM invoice_payments");
            ExecuteNonQuery(tx, "DELETE FROM inventory_movements WHERE reference_type='invoice'");
            ExecuteNonQuery(tx, "DELETE FROM invoices");
        }
        private static void ExecuteClearInventoryInternal(NpgsqlTransaction tx)
        {
            ExecuteNonQuery(tx, "DELETE FROM inventory_movements");
            ExecuteNonQuery(tx, "UPDATE stock_levels SET quantity=0, reserved_quantity=0, damaged_quantity=0, under_inspection_quantity=0");
            ExecuteNonQuery(tx, "UPDATE items SET opening_quantity=0");
        }

        private static void ExecuteNonQuery(NpgsqlTransaction tx, string sql)
        {
            using var cmd = new NpgsqlCommand(sql, tx.Connection, tx);
            cmd.ExecuteNonQuery();
        }

        private static void ResetSequence(NpgsqlTransaction tx, string seqName)
        {
            try { ExecuteNonQuery(tx, $"ALTER SEQUENCE {seqName} RESTART WITH 1"); }
            catch { } // Ignore if sequence doesn't exist
        }
    }
}