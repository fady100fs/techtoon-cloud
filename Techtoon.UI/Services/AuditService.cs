using System;
using Npgsql;

namespace Techtoon.UI.Services;

public static class AuditService
{
    public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();

    public static void Log(string action, string entityType, int? entityId, string details = "")
    {
        try
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            
            var checkCmd = new NpgsqlCommand(@"
                CREATE TABLE IF NOT EXISTS app_audit_logs (
                    id SERIAL PRIMARY KEY,
                    action VARCHAR(50) NOT NULL,
                    entity_type VARCHAR(100) NOT NULL,
                    entity_id INTEGER,
                    details TEXT,
                    user_name VARCHAR(100),
                    created_at TIMESTAMP NOT NULL DEFAULT NOW()
                )", conn);
            checkCmd.ExecuteNonQuery();

            using var cmd = new NpgsqlCommand(@"
                INSERT INTO app_audit_logs (action, entity_type, entity_id, details, user_name, created_at) 
                VALUES (@action, @entity, @id, @details, @user, NOW())", conn);
            
            cmd.Parameters.AddWithValue("action", action);
            cmd.Parameters.AddWithValue("entity", entityType);
            cmd.Parameters.AddWithValue("id", (object)entityId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("details", details ?? "");
            cmd.Parameters.AddWithValue("user", AuthService.CurrentUser?.Username ?? "System");
            cmd.ExecuteNonQuery();
        }
        catch { /* تجاهل أخطاء السجل حتى لا توقف العمل الأساسي */ }
    }
}