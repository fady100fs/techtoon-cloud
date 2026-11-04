using System;
using System.Collections.Generic;
using Npgsql;

namespace Techtoon.UI.Services
{
    public class StockReason
    {
        public int Id { get; set; }
        public string ReasonType { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public bool AffectsFinancial { get; set; }
    }

    public static class StockReasonsService
    {
        public static List<StockReason> GetReasons(string type = null)
        {
            var list = new List<StockReason>();
            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            conn.Open();
            var sql = "SELECT id, reason_type, code, name, is_active, affects_financial FROM stock_reasons WHERE is_active = TRUE";
            if (!string.IsNullOrEmpty(type)) sql += " AND reason_type = @t";
            sql += " ORDER BY name";
            using var cmd = new NpgsqlCommand(sql, conn);
            if (!string.IsNullOrEmpty(type)) cmd.Parameters.AddWithValue("t", type);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new StockReason
                {
                    Id = r.GetInt32(0),
                    ReasonType = r.GetString(1),
                    Code = r.GetString(2),
                    Name = r.GetString(3),
                    IsActive = r.GetBoolean(4),
                    AffectsFinancial = r.GetBoolean(5)
                });
            }
            return list;
        }

        public static int SaveReason(StockReason reason)
        {
            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            conn.Open();
            if (reason.Id == 0)
            {
                using var cmd = new NpgsqlCommand(
                    "INSERT INTO stock_reasons (reason_type, code, name, is_active, affects_financial) " +
                    "VALUES (@t, @c, @n, @a, @f) RETURNING id", conn);
                cmd.Parameters.AddWithValue("t", reason.ReasonType);
                cmd.Parameters.AddWithValue("c", reason.Code);
                cmd.Parameters.AddWithValue("n", reason.Name);
                cmd.Parameters.AddWithValue("a", reason.IsActive);
                cmd.Parameters.AddWithValue("f", reason.AffectsFinancial);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
            else
            {
                using var cmd = new NpgsqlCommand(
                    "UPDATE stock_reasons SET reason_type=@t, code=@c, name=@n, is_active=@a, affects_financial=@f WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("t", reason.ReasonType);
                cmd.Parameters.AddWithValue("c", reason.Code);
                cmd.Parameters.AddWithValue("n", reason.Name);
                cmd.Parameters.AddWithValue("a", reason.IsActive);
                cmd.Parameters.AddWithValue("f", reason.AffectsFinancial);
                cmd.Parameters.AddWithValue("id", reason.Id);
                cmd.ExecuteNonQuery();
                return reason.Id;
            }
        }

        public static void DeleteReason(int id)
        {
            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            conn.Open();
            using var cmd = new NpgsqlCommand("DELETE FROM stock_reasons WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", id);
            cmd.ExecuteNonQuery();
        }
    }
}