using System;
using System.Collections.Generic;
using Npgsql;
using Techtoon.Core.Data;

namespace Techtoon.UI.Services
{
    /// <summary>
    /// حركات مراكز التكلفة (<c>cost_center_transactions</c>) — خدمة مستقلة قابلة للاختبار.
    /// تستخدمها صفحة توزيع التكاليف وتُغطّي الفجوة «حركات مراكز التكلفة — لم تُختبر الإضافة».
    /// </summary>
    public class CostCenterService
    {
        private readonly string _cs;

        public CostCenterService(string connectionString) { _cs = connectionString; }

        public static CostCenterService Create() =>
            new CostCenterService(SecureConnectionManager.GetConnectionString());

        /// <summary>حركة مركز تكلفة.</summary>
        public class Transaction
        {
            public int Id { get; set; }
            public int CostCenterId { get; set; }
            public DateTime Date { get; set; }
            public string Description { get; set; } = "";
            public decimal Amount { get; set; }
            public string TransactionType { get; set; } = "debit";
            public string ReferenceType { get; set; } = "";
            public int? ReferenceId { get; set; }
        }

        /// <summary>حركات مركز تكلفة معيّنة.</summary>
        public List<Transaction> GetTransactions(int costCenterId)
        {
            var list = new List<Transaction>();
            using var conn = new NpgsqlConnection(_cs);
            conn.Open();
            using var cmd = new NpgsqlCommand(
                @"SELECT id, cost_center_id, date, description, amount, transaction_type,
                         COALESCE(reference_type,''), reference_id
                  FROM cost_center_transactions
                  WHERE cost_center_id = @cc
                  ORDER BY date DESC, id DESC LIMIT 500", conn);
            cmd.Parameters.AddWithValue("@cc", costCenterId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new Transaction
                {
                    Id = r.GetInt32(0),
                    CostCenterId = r.GetInt32(1),
                    Date = DbValue.AsDateTime(r.GetValue(2)),
                    Description = r.IsDBNull(3) ? "" : r.GetString(3),
                    Amount = Convert.ToDecimal(r.GetValue(4)),
                    TransactionType = r.IsDBNull(5) ? "debit" : r.GetString(5),
                    ReferenceType = r.IsDBNull(6) ? "" : r.GetString(6),
                    ReferenceId = r.IsDBNull(7) ? (int?)null : r.GetInt32(7)
                });
            }
            return list;
        }

        /// <summary>صافي رصيد مركز التكلفة (مدين − دائن).</summary>
        public decimal GetBalance(int costCenterId)
        {
            using var conn = new NpgsqlConnection(_cs);
            conn.Open();
            using var cmd = new NpgsqlCommand(
                @"SELECT COALESCE(SUM(CASE WHEN LOWER(transaction_type) IN ('debit','expense','transfer_out') THEN amount
                                            WHEN LOWER(transaction_type) IN ('credit','revenue','transfer_in') THEN -amount
                                            ELSE 0 END),0)
                  FROM cost_center_transactions WHERE cost_center_id = @cc", conn);
            cmd.Parameters.AddWithValue("@cc", costCenterId);
            return Convert.ToDecimal(cmd.ExecuteScalar());
        }

        /// <summary>إضافة حركة لمركز تكلفة.</summary>
        public int AddTransaction(int costCenterId, DateTime date, string description, decimal amount,
            string transactionType, string referenceType, int? referenceId, int createdBy = 1)
        {
            if (costCenterId <= 0) throw new InvalidOperationException("مركز التكلفة غير محدد.");
            if (amount <= 0) throw new InvalidOperationException("المبلغ يجب أن يكون أكبر من صفر.");

            using var conn = new NpgsqlConnection(_cs);
            conn.Open();
            using var cmd = new NpgsqlCommand(
                @"INSERT INTO cost_center_transactions
                    (cost_center_id, date, description, amount, transaction_type, reference_type, reference_id, created_by, created_at)
                  VALUES (@cc, @d, @desc, @amt, @type, @rt, @rid, @cb, NOW()) RETURNING id", conn);
            cmd.Parameters.AddWithValue("@cc", costCenterId);
            cmd.Parameters.AddWithValue("@d", DbValue.AsDateTime(date));
            cmd.Parameters.AddWithValue("@desc", description ?? "");
            cmd.Parameters.AddWithValue("@amt", amount);
            cmd.Parameters.AddWithValue("@type", string.IsNullOrWhiteSpace(transactionType) ? "debit" : transactionType);
            cmd.Parameters.AddWithValue("@rt", string.IsNullOrWhiteSpace(referenceType) ? (object)DBNull.Value : referenceType);
            cmd.Parameters.AddWithValue("@rid", (object?)referenceId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cb", createdBy);
            var id = Convert.ToInt32(cmd.ExecuteScalar());
            AuditService.Log("CREATE", "CostCenterTransaction", id,
                "Cost center " + costCenterId + ": " + transactionType + " " + amount.ToString("N2") + " - " + description);
            return id;
        }

        /// <summary>تعديل حركة.</summary>
        public void UpdateTransaction(int id, DateTime date, string description, decimal amount,
            string transactionType, string referenceType, int? referenceId)
        {
            using var conn = new NpgsqlConnection(_cs);
            conn.Open();
            using var cmd = new NpgsqlCommand(
                @"UPDATE cost_center_transactions
                  SET date=@d, description=@desc, amount=@amt, transaction_type=@type,
                      reference_type=@rt, reference_id=@rid
                  WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@d", DbValue.AsDateTime(date));
            cmd.Parameters.AddWithValue("@desc", description ?? "");
            cmd.Parameters.AddWithValue("@amt", amount);
            cmd.Parameters.AddWithValue("@type", transactionType);
            cmd.Parameters.AddWithValue("@rt", string.IsNullOrWhiteSpace(referenceType) ? (object)DBNull.Value : referenceType);
            cmd.Parameters.AddWithValue("@rid", (object?)referenceId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
            AuditService.Log("UPDATE", "CostCenterTransaction", id, "Cost center transaction updated");
        }

        /// <summary>حذف حركة.</summary>
        public bool DeleteTransaction(int id)
        {
            using var conn = new NpgsqlConnection(_cs);
            conn.Open();
            using var cmd = new NpgsqlCommand("DELETE FROM cost_center_transactions WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            var n = cmd.ExecuteNonQuery();
            if (n > 0) AuditService.Log("DELETE", "CostCenterTransaction", id, "Cost center transaction deleted");
            return n > 0;
        }
    }
}
