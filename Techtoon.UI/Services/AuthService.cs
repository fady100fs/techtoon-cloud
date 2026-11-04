using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using BCrypt.Net;

namespace Techtoon.UI.Services
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
    }

    public class LoginResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public User User { get; set; }
    }

    public static class AuthService
    {
        public static User CurrentUser { get; private set; }
        public static bool IsLoggedIn => CurrentUser != null;

        private static HashSet<string> _permissions = new HashSet<string>();

        public static string GetCS() { return Techtoon.UI.Services.SecureConnectionManager.GetConnectionString(); }

        public static async Task<LoginResult> LoginAsync(string username, string password)
        {
            return await Task.FromResult(Login(username, password));
        }

        public static LoginResult Login(string username, string password)
        {
            var result = new LoginResult();
            try
            {
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT id, username, COALESCE(password_hash,''), COALESCE(full_name,''), COALESCE(role,'SALESPERSON'), COALESCE(is_active, FALSE) FROM users WHERE username=@u", conn))
                    {
                        cmd.Parameters.AddWithValue("u", username);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (!r.Read()) { result.ErrorMessage = "User not found"; return result; }
                            if (!r.GetBoolean(5)) { result.ErrorMessage = "User inactive"; return result; }

                            string hash = r.GetString(2);
                            if (!string.IsNullOrEmpty(hash) && !BCrypt.Net.BCrypt.Verify(password, hash))
                            {
                                result.ErrorMessage = "Wrong password";
                                return result;
                            }

                            CurrentUser = new User
                            {
                                Id = r.GetInt32(0),
                                Username = r.GetString(1),
                                FullName = r.GetString(3),
                                Role = r.GetString(4),
                                IsActive = true
                            };
                        }
                    }

                    LoadPermissions();

                    using (var cmd = new NpgsqlCommand("UPDATE users SET last_login=NOW() WHERE id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("id", CurrentUser.Id);
                        cmd.ExecuteNonQuery();
                    }

                    LogAudit("login", "user", CurrentUser.Id, "Login success");
                    result.Success = true;
                    result.User = CurrentUser;
                    return result;
                }
            }
            catch (Exception ex) { result.ErrorMessage = ex.Message; return result; }
        }

        public static void Logout()
        {
            if (IsLoggedIn) LogAudit("logout", "user", CurrentUser.Id, "Logout");
            CurrentUser = null;
            _permissions.Clear();
        }

        private static void LoadPermissions()
        {
            _permissions.Clear();
            if (CurrentUser == null) return;
            try
            {
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT permission_key FROM user_permissions WHERE user_id=@id AND is_granted=TRUE", conn))
                    {
                        cmd.Parameters.AddWithValue("id", CurrentUser.Id);
                        using (var r = cmd.ExecuteReader())
                            while (r.Read()) _permissions.Add(r.GetString(0));
                    }
                }
            }
            catch { }
        }

        public static bool HasPermission(string key)
        {
            if (!IsLoggedIn) return false;
            if (CurrentUser.Role == "ADMIN") return true;
            return _permissions.Contains(key);
        }

        public static void LogAudit(string action, string entityType, int entityId, string details)
        {
            try
            {
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(@"INSERT INTO audit_log (user_id, username, action, entity_type, entity_id, details, created_at) VALUES (@uid, @uname, @action, @etype, @eid, @details, NOW())", conn))
                    {
                        cmd.Parameters.AddWithValue("uid", CurrentUser?.Id > 0 ? (object)CurrentUser.Id : DBNull.Value);
                        cmd.Parameters.AddWithValue("uname", (object)CurrentUser?.Username ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("action", action);
                        cmd.Parameters.AddWithValue("etype", (object)entityType ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("eid", entityId > 0 ? (object)entityId : DBNull.Value);
                        cmd.Parameters.AddWithValue("details", (object)details ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }

        public static async Task EnsureDefaultAdminAsync()
        {
            try
            {
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM users WHERE username='admin'", conn))
                    {
                        if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                        {
                            string hash = BCrypt.Net.BCrypt.HashPassword("admin123", workFactor: 12);
                            using (var c2 = new NpgsqlCommand("INSERT INTO users (username, password_hash, full_name, role, is_active, created_at) VALUES ('admin', @h, 'Administrator', 'ADMIN', TRUE, NOW())", conn))
                            {
                                c2.Parameters.AddWithValue("h", hash);
                                c2.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            catch { }
            await Task.CompletedTask;
        }

        public static bool CreateUser(string username, string password, string fullName, string role)
        {
            if (!HasPermission("manage_users")) return false;
            try
            {
                string hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("INSERT INTO users (username, password_hash, full_name, role, is_active, created_at) VALUES (@u, @h, @fn, @r, TRUE, NOW())", conn))
                    {
                        cmd.Parameters.AddWithValue("u", username);
                        cmd.Parameters.AddWithValue("h", hash);
                        cmd.Parameters.AddWithValue("fn", fullName);
                        cmd.Parameters.AddWithValue("r", role);
                        cmd.ExecuteNonQuery();
                    }
                }
                LogAudit("create_user", "user", 0, $"Created user: {username} ({role})");
                return true;
            }
            catch { return false; }
        }

        public static bool ChangePassword(int userId, string newPassword)
        {
            try
            {
                string hash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("UPDATE users SET password_hash=@h WHERE id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("h", hash);
                        cmd.Parameters.AddWithValue("id", userId);
                        cmd.ExecuteNonQuery();
                    }
                }
                LogAudit("change_password", "user", userId, "Password changed");
                return true;
            }
            catch { return false; }
        }

        public static bool SetPermission(int userId, string permissionKey, bool granted)
        {
            if (!HasPermission("manage_users")) return false;
            try
            {
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(@"INSERT INTO user_permissions (user_id, permission_key, is_granted) VALUES (@uid, @key, @granted) ON CONFLICT (user_id, permission_key) DO UPDATE SET is_granted = @granted", conn))
                    {
                        cmd.Parameters.AddWithValue("uid", userId);
                        cmd.Parameters.AddWithValue("key", permissionKey);
                        cmd.Parameters.AddWithValue("granted", granted);
                        cmd.ExecuteNonQuery();
                    }
                }
                LogAudit("set_permission", "user", userId, $"Permission '{permissionKey}' = {granted}");
                return true;
            }
            catch { return false; }
        }
        // ============= USER MANAGEMENT METHODS =============

        public class UserWithRole
        {
            public int Id { get; set; }
            public string Username { get; set; }
            public string FullName { get; set; }
            public string Email { get; set; }
            public string Role { get; set; }
            public int? RoleId { get; set; }
            public string RoleDisplayName { get; set; }
            public bool IsActive { get; set; }
            public bool MustChangePassword { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? LastLogin { get; set; }
        }

        public class RoleOption
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string DisplayName { get; set; }
            public string Description { get; set; }
            public bool IsSystemRole { get; set; }
        }

        public static System.Collections.Generic.List<UserWithRole> GetAllUsers(string search = null)
        {
            var list = new System.Collections.Generic.List<UserWithRole>();
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            var sql = "SELECT u.id, u.username, u.full_name, u.email, u.role, u.role_id, " +
                      "COALESCE(r.display_name, u.role), u.is_active, u.must_change_password, u.created_at, u.last_login " +
                      "FROM users u LEFT JOIN roles r ON u.role_id = r.id";
            if (!string.IsNullOrEmpty(search))
                sql += " WHERE u.username ILIKE @q OR u.full_name ILIKE @q OR u.role ILIKE @q";
            sql += " ORDER BY u.id";
            using var cmd = new NpgsqlCommand(sql, conn);
            if (!string.IsNullOrEmpty(search))
                cmd.Parameters.AddWithValue("q", "%" + search + "%");
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new UserWithRole
                {
                    Id = r.GetInt32(0),
                    Username = r.GetString(1),
                    FullName = r.GetString(2),
                    Email = r.IsDBNull(3) ? "" : r.GetString(3),
                    Role = r.GetString(4),
                    RoleId = r.IsDBNull(5) ? (int?)null : r.GetInt32(5),
                    RoleDisplayName = r.GetString(6),
                    IsActive = r.IsDBNull(7) ? true : r.GetBoolean(7),
                    MustChangePassword = r.IsDBNull(8) ? false : r.GetBoolean(8),
                    CreatedAt = r.IsDBNull(9) ? DateTime.MinValue : r.GetDateTime(9),
                    LastLogin = r.IsDBNull(10) ? (DateTime?)null : r.GetDateTime(10)
                });
            }
            return list;
        }

        public static System.Collections.Generic.List<RoleOption> GetAllRoles()
        {
            var list = new System.Collections.Generic.List<RoleOption>();
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT id, name, display_name, description, is_system_role FROM roles WHERE is_active = TRUE ORDER BY name", conn);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new RoleOption
                {
                    Id = r.GetInt32(0),
                    Name = r.GetString(1),
                    DisplayName = r.GetString(2),
                    Description = r.IsDBNull(3) ? "" : r.GetString(3),
                    IsSystemRole = r.GetBoolean(4)
                });
            }
            return list;
        }

        public static bool IsLastAdmin(int userId)
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            // تحقق هل المستخدم هو آخر ADMIN نشط
            using var cmd = new NpgsqlCommand(
                "SELECT COUNT(*) FROM users WHERE role = 'ADMIN' AND is_active = TRUE AND id != @id", conn);
            cmd.Parameters.AddWithValue("id", userId);
            int count = Convert.ToInt32(cmd.ExecuteScalar());
            return count == 0;
        }

        public static bool IsCurrentUser(int userId)
        {
            return CurrentUser != null && CurrentUser.Id == userId;
        }

        public static void UpdateUser(int userId, string fullName, string email, int roleId)
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                // جلب اسم الـ role من roles table
                string roleName = "";
                using (var cmd = new NpgsqlCommand("SELECT name FROM roles WHERE id = @rid", conn, tx))
                {
                    cmd.Parameters.AddWithValue("rid", roleId);
                    var result = cmd.ExecuteScalar();
                    if (result != null) roleName = result.ToString();
                }

                using var cmd2 = new NpgsqlCommand(
                    "UPDATE users SET full_name = @fn, email = @em, role_id = @rid, role = @r WHERE id = @id", conn, tx);
                cmd2.Parameters.AddWithValue("fn", fullName);
                cmd2.Parameters.AddWithValue("em", string.IsNullOrEmpty(email) ? (object)DBNull.Value : email);
                cmd2.Parameters.AddWithValue("rid", roleId);
                cmd2.Parameters.AddWithValue("r", roleName);
                cmd2.Parameters.AddWithValue("id", userId);
                cmd2.ExecuteNonQuery();

                LogAudit("UPDATE_USER", "user", userId, "Updated user: " + fullName);
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        public static void ToggleUserActive(int userId)
        {
            if (IsCurrentUser(userId))
                throw new InvalidOperationException("لا يمكن تعطيل حسابك الحالي");

            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                // جلب الحالة الحالية + role
                bool currentActive; string role;
                using (var cmd = new NpgsqlCommand("SELECT is_active, role FROM users WHERE id = @id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("id", userId);
                    using var r = cmd.ExecuteReader();
                    if (!r.Read()) throw new InvalidOperationException("المستخدم غير موجود");
                    currentActive = r.IsDBNull(0) ? true : r.GetBoolean(0);
                    role = r.GetString(1);
                }

                // إذا كان سيتم تعطيل مستخدم ADMIN، تحقق من عدم كونه الأخير
                if (currentActive && role == "ADMIN" && IsLastAdmin(userId))
                    throw new InvalidOperationException("لا يمكن تعطيل آخر حساب مدير نشط في النظام");

                bool newActive = !currentActive;
                using var cmd2 = new NpgsqlCommand("UPDATE users SET is_active = @a WHERE id = @id", conn, tx);
                cmd2.Parameters.AddWithValue("a", newActive);
                cmd2.Parameters.AddWithValue("id", userId);
                cmd2.ExecuteNonQuery();

                LogAudit(newActive ? "ACTIVATE_USER" : "DEACTIVATE_USER", "user", userId,
                    newActive ? "User activated" : "User deactivated");
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        public static void ResetPassword(int userId, string newPassword)
        {
            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 4)
                throw new InvalidOperationException("كلمة المرور قصيرة جدًا (4 أحرف على الأقل)");

            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                var hash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);
                using var cmd = new NpgsqlCommand(
                    "UPDATE users SET password_hash = @h, must_change_password = TRUE WHERE id = @id", conn, tx);
                cmd.Parameters.AddWithValue("h", hash);
                cmd.Parameters.AddWithValue("id", userId);
                cmd.ExecuteNonQuery();

                LogAudit("RESET_PASSWORD", "user", userId, "Password reset by admin");
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        public static void ChangeOwnPassword(int userId, string oldPassword, string newPassword)
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            // تحقق من كلمة المرور القديمة
            string currentHash;
            using (var cmd = new NpgsqlCommand("SELECT password_hash FROM users WHERE id = @id", conn))
            {
                cmd.Parameters.AddWithValue("id", userId);
                currentHash = cmd.ExecuteScalar()?.ToString() ?? "";
            }
            if (!BCrypt.Net.BCrypt.Verify(oldPassword, currentHash))
                throw new InvalidOperationException("كلمة المرور الحالية غير صحيحة");

            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 4)
                throw new InvalidOperationException("كلمة المرور الجديدة قصيرة جدًا");

            using var tx = conn.BeginTransaction();
            try
            {
                var newHash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);
                using var cmd = new NpgsqlCommand(
                    "UPDATE users SET password_hash = @h, must_change_password = FALSE WHERE id = @id", conn, tx);
                cmd.Parameters.AddWithValue("h", newHash);
                cmd.Parameters.AddWithValue("id", userId);
                cmd.ExecuteNonQuery();

                LogAudit("CHANGE_PASSWORD", "user", userId, "User changed own password");
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        public static System.Collections.Generic.Dictionary<string, bool> GetUserPermissions(int userId)
        {
            var perms = new System.Collections.Generic.Dictionary<string, bool>();
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT permission_key, is_granted FROM user_permissions WHERE user_id = @uid", conn);
            cmd.Parameters.AddWithValue("uid", userId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                perms[r.GetString(0)] = r.IsDBNull(1) ? true : r.GetBoolean(1);
            }
            return perms;
        }

        public static void SetUserPermissions(int userId, System.Collections.Generic.Dictionary<string, bool> permissions)
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                // حذف الصلاحيات القديمة
                using (var cmd = new NpgsqlCommand("DELETE FROM user_permissions WHERE user_id = @uid", conn, tx))
                {
                    cmd.Parameters.AddWithValue("uid", userId);
                    cmd.ExecuteNonQuery();
                }
                // إدراج الصلاحيات الجديدة
                foreach (var kvp in permissions)
                {
                    using var cmd = new NpgsqlCommand(
                        "INSERT INTO user_permissions (user_id, permission_key, is_granted, created_at) " +
                        "VALUES (@uid, @key, @granted, NOW())", conn, tx);
                    cmd.Parameters.AddWithValue("uid", userId);
                    cmd.Parameters.AddWithValue("key", kvp.Key);
                    cmd.Parameters.AddWithValue("granted", kvp.Value);
                    cmd.ExecuteNonQuery();
                }

                LogAudit("SET_PERMISSIONS", "user", userId, "Permissions updated: " + permissions.Count + " keys");
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }

        // كل مفاتيح الصلاحيات المتاحة في النظام
        public static System.Collections.Generic.List<string> GetAllPermissionKeys()
        {
            return new System.Collections.Generic.List<string>
            {
                // المبيعات والمشتريات
                "sales", "returns", "discounts", "edit_price", "cancel_invoice",
                // المالية
                "expenses", "profit_reports", "backup", "settings",
                // المستخدمين
                "manage_users",
                // المخزون القديم
                "stock_adjustment",
                // المخزون الجديد - الجرد
                "inventory.count.create", "inventory.count.count", "inventory.count.review", "inventory.count.approve",
                // المخزون الجديد - التسويات
                "inventory.adjustment.create", "inventory.adjustment.approve", "inventory.adjustment.execute",
                // المخزون الجديد - التالف
                "inventory.damaged.create", "inventory.damaged.inspect",
                // المخزون الجديد - الشطب
                "inventory.writeoff.approve", "inventory.writeoff.execute",
                // التقارير
                "inventory.reports.view", "inventory.audit.view"
            };
        }

        public static void CheckUserActiveStatus(int userId)
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT is_active FROM users WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", userId);
            var result = cmd.ExecuteScalar();
            if (result == null) throw new InvalidOperationException("المستخدم غير موجود");
            if (!(bool)result) throw new InvalidOperationException("الحساب معطل. تواصل مع المدير");
        }
    }
}