using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;
using Techtoon.Core.Permissions;

namespace Techtoon.UI.Services
{
    /// <summary>سجل دور كما هو مخزَّن في قاعدة البيانات.</summary>
    public class RoleRecord
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Description { get; set; } = "";
        public bool IsSystemRole { get; set; }
        public bool IsActive { get; set; } = true;
        public int PermissionCount { get; set; }
        public int UserCount { get; set; }
    }

    /// <summary>
    /// خدمة الأدوار والصلاحيات.
    ///
    /// <para><b>نموذج الصلاحيات:</b></para>
    /// <list type="number">
    ///   <item><description>الدور (<c>roles</c>) يملك مجموعة صلاحيات (<c>role_permissions</c>).</description></item>
    ///   <item><description>المستخدم يملك دورًا واحدًا (<c>users.role_id</c>).</description></item>
    ///   <item><description>والمستخدم قد يملك استثناءات خاصة (<c>user_permissions</c>) تسبق الدور:
    ///   منح = صلاحية إضافية، منع = سحب صلاحية من الدور.</description></item>
    /// </list>
    ///
    /// <para>ترتيب الحل: استثناء المستخدم ← صلاحية الدور ← منع (افتراضي).</para>
    /// </summary>
    public static class RoleService
    {
        /// <summary>يضمن وجود جدول <c>role_permissions</c> والأدوار النظامية.</summary>
        public static void EnsureSchema()
        {
            try
            {
                using var conn = new NpgsqlConnection(AuthService.GetCS());
                conn.Open();
                Execute(conn, null, @"
                    CREATE TABLE IF NOT EXISTS role_permissions (
                        id              SERIAL PRIMARY KEY,
                        role_id         INTEGER NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
                        permission_key  VARCHAR(100) NOT NULL,
                        is_granted      BOOLEAN NOT NULL DEFAULT TRUE,
                        created_at      TIMESTAMP NOT NULL DEFAULT NOW(),
                        CONSTRAINT role_permissions_unique UNIQUE (role_id, permission_key)
                    )");
                Execute(conn, null, "CREATE INDEX IF NOT EXISTS idx_role_permissions_role ON role_permissions(role_id)");
                Execute(conn, null, "CREATE INDEX IF NOT EXISTS idx_user_permissions_user ON user_permissions(user_id)");
                // مفتاح فريد للاستثناءات (مستخدم + صلاحية) — يلزم لعملية ON CONFLICT
                Execute(conn, null,
                    "CREATE UNIQUE INDEX IF NOT EXISTS idx_user_permissions_unique ON user_permissions(user_id, permission_key)");
                Execute(conn, null, "CREATE UNIQUE INDEX IF NOT EXISTS idx_roles_name ON roles(name)");
                SeedSystemRoles(conn);
            }
            catch (Exception ex)
            {
                App.Log("RoleService.EnsureSchema failed: " + ex.Message);
            }
        }

        /// <summary>
        /// يزرع الأدوار النظامية وصلاحياتها الافتراضية من <see cref="RoleCatalog"/>.
        /// آمن للتكرار: الأدوار تُحدَّث، وصلاحيات الدور تُستبدل بقائمة الكتالوج.
        /// الأدوار المخصصة (غير النظامية) لا تُمس.
        /// </summary>
        public static void SeedSystemRoles()
        {
            using var conn = new NpgsqlConnection(AuthService.GetCS());
            conn.Open();
            SeedSystemRoles(conn);
        }

        private static void SeedSystemRoles(NpgsqlConnection conn)
        {
            foreach (var def in RoleCatalog.All())
            {
                int roleId = UpsertRole(conn, def);
                // 'ADMIN' يُعامَل особيًا في HasPermission (صلاحية كاملة) — نخزّن صفوفًا له أيضًا
                // حتى تظهر مصفوفة الأدوار متسقة في شاشة الصلاحيات.
                SetRolePermissions(conn, roleId, def.Permissions);
            }
        }

        private static int UpsertRole(NpgsqlConnection conn, RoleDefinition def)
        {
            using (var cmd = new NpgsqlCommand(
                "UPDATE roles SET display_name=@dn, description=@ds, is_system_role=TRUE, is_active=TRUE WHERE name=@n", conn))
            {
                cmd.Parameters.AddWithValue("dn", def.DisplayName);
                cmd.Parameters.AddWithValue("ds", (object)def.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("n", def.Name);
                if (cmd.ExecuteNonQuery() > 0)
                {
                    using var sel = new NpgsqlCommand("SELECT id FROM roles WHERE name=@n", conn);
                    sel.Parameters.AddWithValue("n", def.Name);
                    return Convert.ToInt32(sel.ExecuteScalar());
                }
            }
            using (var ins = new NpgsqlCommand(
                "INSERT INTO roles (name, display_name, description, is_system_role, is_active, created_at) " +
                "VALUES (@n, @dn, @ds, TRUE, TRUE, NOW()) RETURNING id", conn))
            {
                ins.Parameters.AddWithValue("n", def.Name);
                ins.Parameters.AddWithValue("dn", def.DisplayName);
                ins.Parameters.AddWithValue("ds", (object)def.Description ?? DBNull.Value);
                return Convert.ToInt32(ins.ExecuteScalar());
            }
        }

        /// <summary>كل الأدوار.</summary>
        public static List<RoleRecord> GetRoles()
        {
            var list = new List<RoleRecord>();
            using var conn = new NpgsqlConnection(AuthService.GetCS());
            conn.Open();
            using var cmd = new NpgsqlCommand(
                "SELECT r.id, r.name, r.display_name, COALESCE(r.description,''), " +
                "       COALESCE(r.is_system_role, FALSE), COALESCE(r.is_active, TRUE), " +
                "       (SELECT COUNT(*) FROM role_permissions rp WHERE rp.role_id=r.id AND rp.is_granted), " +
                "       (SELECT COUNT(*) FROM users u WHERE u.role_id=r.id) " +
                "FROM roles r ORDER BY r.is_system_role DESC, r.name", conn);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new RoleRecord
                {
                    Id = r.GetInt32(0),
                    Name = r.GetString(1),
                    DisplayName = r.GetString(2),
                    Description = r.GetString(3),
                    IsSystemRole = r.GetBoolean(4),
                    IsActive = r.GetBoolean(5),
                    PermissionCount = Convert.ToInt32(r.GetValue(6)),
                    UserCount = Convert.ToInt32(r.GetValue(7))
                });
            }
            return list;
        }

        /// <summary>دور بالاسم (غير حساس لحالة الأحرف).</summary>
        public static RoleRecord? FindByName(string name)
        {
            using var conn = new NpgsqlConnection(AuthService.GetCS());
            conn.Open();
            using var cmd = new NpgsqlCommand(
                "SELECT id, name, display_name, COALESCE(description,''), COALESCE(is_system_role,FALSE), COALESCE(is_active,TRUE) " +
                "FROM roles WHERE UPPER(name) = UPPER(@n)", conn);
            cmd.Parameters.AddWithValue("n", name ?? "");
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;
            return new RoleRecord
            {
                Id = r.GetInt32(0),
                Name = r.GetString(1),
                DisplayName = r.GetString(2),
                Description = r.GetString(3),
                IsSystemRole = r.GetBoolean(4),
                IsActive = r.GetBoolean(5)
            };
        }

        /// <summary>إنشاء دور مخصص.</summary>
        public static int CreateRole(string name, string displayName, string description)
        {
            name = (name ?? "").Trim().ToUpperInvariant();
            if (name.Length == 0) throw new InvalidOperationException("اسم الدور مطلوب");
            if (FindByName(name) != null) throw new InvalidOperationException("يوجد دور بنفس الاسم");

            using var conn = new NpgsqlConnection(AuthService.GetCS());
            conn.Open();
            using var cmd = new NpgsqlCommand(
                "INSERT INTO roles (name, display_name, description, is_system_role, is_active, created_at) " +
                "VALUES (@n, @dn, @ds, FALSE, TRUE, NOW()) RETURNING id", conn);
            cmd.Parameters.AddWithValue("n", name);
            cmd.Parameters.AddWithValue("dn", string.IsNullOrWhiteSpace(displayName) ? name : displayName.Trim());
            cmd.Parameters.AddWithValue("ds", (object)description ?? DBNull.Value);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        /// <summary>تعديل دور (الاسم الداخلي للأدوار النظامية لا يُغيَّر).</summary>
        public static void UpdateRole(int roleId, string displayName, string description, bool isActive)
        {
            using var conn = new NpgsqlConnection(AuthService.GetCS());
            conn.Open();
            using var cmd = new NpgsqlCommand(
                "UPDATE roles SET display_name=@dn, description=@ds, is_active=@act WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("dn", displayName ?? "");
            cmd.Parameters.AddWithValue("ds", (object)description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("act", isActive);
            cmd.Parameters.AddWithValue("id", roleId);
            cmd.ExecuteNonQuery();
        }

        /// <summary>حذف دور مخصص (الأدوار النظامية لا تُحذف، والمستخدمون يُنقلون إلى VIEWER).</summary>
        public static void DeleteRole(int roleId)
        {
            using var conn = new NpgsqlConnection(AuthService.GetCS());
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                string name = "";
                using (var cmd = new NpgsqlCommand("SELECT name, COALESCE(is_system_role,FALSE) FROM roles WHERE id=@id FOR UPDATE", conn, tx))
                {
                    cmd.Parameters.AddWithValue("id", roleId);
                    using var r = cmd.ExecuteReader();
                    if (!r.Read()) throw new InvalidOperationException("الدور غير موجود");
                    name = r.GetString(0);
                    if (r.GetBoolean(1)) throw new InvalidOperationException("لا يمكن حذف دور نظامي");
                }

                int viewerId = -1;
                using (var cmd = new NpgsqlCommand("SELECT id FROM roles WHERE name='VIEWER'", conn, tx))
                {
                    var v = cmd.ExecuteScalar();
                    if (v != null) viewerId = Convert.ToInt32(v);
                }

                using (var cmd = new NpgsqlCommand(
                    "UPDATE users SET role_id=@v, role=@vn WHERE role_id=@id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("v", viewerId >= 0 ? viewerId : (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("vn", viewerId >= 0 ? "VIEWER" : (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("id", roleId);
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new NpgsqlCommand("DELETE FROM roles WHERE id=@id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("id", roleId);
                    cmd.ExecuteNonQuery();
                }
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        /// <summary>صلاحيات دور.</summary>
        public static HashSet<string> GetRolePermissions(int roleId)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using var conn = new NpgsqlConnection(AuthService.GetCS());
            conn.Open();
            using var cmd = new NpgsqlCommand(
                "SELECT permission_key FROM role_permissions WHERE role_id=@id AND is_granted=TRUE", conn);
            cmd.Parameters.AddWithValue("id", roleId);
            using var r = cmd.ExecuteReader();
            while (r.Read()) set.Add(r.GetString(0));
            return set;
        }

        /// <summary>استبدال صلاحيات دور (معاملة واحدة).</summary>
        public static void SetRolePermissions(int roleId, IEnumerable<string> keys)
        {
            var list = (keys ?? Enumerable.Empty<string>())
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Select(k => k.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            using var conn = new NpgsqlConnection(AuthService.GetCS());
            conn.Open();
            SetRolePermissions(conn, roleId, list);
        }

        private static void SetRolePermissions(NpgsqlConnection conn, int roleId, List<string> keys)
        {
            // إن كانت الصلاحيات مطابقة تمامًا فلا نكتب شيئًا (يوفّر عملًا كبيرًا عند كل بدء تشغيل).
            var current = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var sel = new NpgsqlCommand(
                "SELECT permission_key FROM role_permissions WHERE role_id=@id AND is_granted=TRUE", conn))
            {
                sel.Parameters.AddWithValue("id", roleId);
                using var r = sel.ExecuteReader();
                while (r.Read()) current.Add(r.GetString(0));
            }
            if (current.SetEquals(keys)) return;

            using var tx = conn.BeginTransaction();
            try
            {
                using (var cmd = new NpgsqlCommand("DELETE FROM role_permissions WHERE role_id=@id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("id", roleId);
                    cmd.ExecuteNonQuery();
                }
                foreach (var key in keys)
                {
                    using var cmd = new NpgsqlCommand(
                        "INSERT INTO role_permissions (role_id, permission_key, is_granted, created_at) " +
                        "VALUES (@id, @k, TRUE, NOW()) ON CONFLICT (role_id, permission_key) DO UPDATE SET is_granted=TRUE", conn, tx);
                    cmd.Parameters.AddWithValue("id", roleId);
                    cmd.Parameters.AddWithValue("k", key);
                    cmd.ExecuteNonQuery();
                }
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        /// <summary>استثناءات المستخدم (منح/منع فوق الدور).</summary>
        public static Dictionary<string, bool> GetUserOverrides(int userId)
        {
            var map = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            using var conn = new NpgsqlConnection(AuthService.GetCS());
            conn.Open();
            using var cmd = new NpgsqlCommand(
                "SELECT permission_key, COALESCE(is_granted, TRUE) FROM user_permissions WHERE user_id=@uid", conn);
            cmd.Parameters.AddWithValue("uid", userId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                map[r.GetString(0)] = r.IsDBNull(1) || r.GetBoolean(1);
            return map;
        }

        /// <summary>استبدال استثناءات المستخدم (معاملة واحدة).</summary>
        public static void SetUserOverrides(int userId, Dictionary<string, bool> overrides)
        {
            using var conn = new NpgsqlConnection(AuthService.GetCS());
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                using (var cmd = new NpgsqlCommand("DELETE FROM user_permissions WHERE user_id=@uid", conn, tx))
                {
                    cmd.Parameters.AddWithValue("uid", userId);
                    cmd.ExecuteNonQuery();
                }
                if (overrides != null)
                {
                    foreach (var kv in overrides)
                    {
                        if (string.IsNullOrWhiteSpace(kv.Key)) continue;
                        using var cmd = new NpgsqlCommand(
                            "INSERT INTO user_permissions (user_id, permission_key, is_granted, created_at) " +
                            "VALUES (@uid, @k, @g, NOW())", conn, tx);
                        cmd.Parameters.AddWithValue("uid", userId);
                        cmd.Parameters.AddWithValue("k", kv.Key.Trim());
                        cmd.Parameters.AddWithValue("g", kv.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        /// <summary>
        /// الصلاحيات الفعلية لمستخدم:
        /// استثناء المستخدم (منح/منع) ← صلاحيات الدور ← منع.
        /// دور ADMIN يعني كل الصلاحيات.
        /// </summary>
        public static HashSet<string> ResolveEffectivePermissions(int userId, int? roleId, string roleName)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            bool isAdmin = string.Equals(roleName ?? "", "ADMIN", StringComparison.OrdinalIgnoreCase);
            if (isAdmin)
            {
                foreach (var k in PermissionCatalog.AllKeys) result.Add(k);
                return result;
            }

            // 1) صلاحيات الدور
            if (roleId.HasValue && roleId.Value > 0)
            {
                foreach (var k in GetRolePermissions(roleId.Value)) result.Add(k);
            }
            else if (!string.IsNullOrWhiteSpace(roleName))
            {
                var role = FindByName(roleName);
                if (role != null)
                    foreach (var k in GetRolePermissions(role.Id)) result.Add(k);
            }

            // 2) استثناءات المستخدم: المنح يضيف، والمنع يسحب
            foreach (var kv in GetUserOverrides(userId))
            {
                if (kv.Value) result.Add(kv.Key);
                else result.Remove(kv.Key);
            }

            return result;
        }

        /// <summary>تعيين دور لمستخدم (يحدّث role_id و role معًا).</summary>
        public static void SetUserRole(int userId, int roleId)
        {
            using var conn = new NpgsqlConnection(AuthService.GetCS());
            conn.Open();
            using var cmd = new NpgsqlCommand(
                "UPDATE users SET role_id=@rid, role=(SELECT name FROM roles WHERE id=@rid) WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("rid", roleId);
            cmd.Parameters.AddWithValue("id", userId);
            cmd.ExecuteNonQuery();
        }

        private static void Execute(NpgsqlConnection conn, NpgsqlTransaction? tx, string sql)
        {
            using var cmd = new NpgsqlCommand(sql, conn, tx);
            cmd.ExecuteNonQuery();
        }
    }
}
