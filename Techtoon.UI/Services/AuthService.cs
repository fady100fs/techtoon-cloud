using System;
using System.Collections.Generic;
using System.Linq;
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
        /// <summary>معرّف الدور (nullable) — مصدر الصلاحيات الفعّالة.</summary>
        public int? RoleId { get; set; }
        /// <summary>الاسم المعروض للدور.</summary>
        public string RoleDisplayName { get; set; }
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

        /// <summary>P3: delegates to PasswordPolicy (extracted).</summary>
        public static bool IsValidPasswordHash(string hash) => PasswordPolicy.IsValidPasswordHash(hash);

        public static LoginResult Login(string username, string password)
        {
            var result = new LoginResult();
            try
            {
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        "SELECT u.id, u.username, COALESCE(u.password_hash,''), COALESCE(u.full_name,''), " +
                        "       COALESCE(NULLIF(u.role,''), r.name, 'VIEWER'), COALESCE(u.is_active, FALSE), " +
                        "       u.role_id, COALESCE(r.display_name,'') " +
                        "FROM users u LEFT JOIN roles r ON u.role_id = r.id " +
                        "WHERE u.username=@u", conn))
                    {
                        cmd.Parameters.AddWithValue("u", username);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (!r.Read()) { result.ErrorMessage = "User not found"; return result; }
                            if (!r.GetBoolean(5)) { result.ErrorMessage = "User inactive"; return result; }

                            string hash = r.GetString(2);
                            if (!IsValidPasswordHash(hash))
                            {
                                result.ErrorMessage = "Invalid credentials";
                                return result;
                            }
                            bool passwordOk;
                            try { passwordOk = BCrypt.Net.BCrypt.Verify(password ?? string.Empty, hash); }
                            catch { passwordOk = false; }
                            if (!passwordOk)
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
                                RoleId = r.IsDBNull(6) ? (int?)null : r.GetInt32(6),
                                RoleDisplayName = r.GetString(7),
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
                // الصلاحيات الفعلية = صلاحيات الدور + استثناءات المستخدم (منح/منع).
                _permissions = RoleService.ResolveEffectivePermissions(
                    CurrentUser.Id, CurrentUser.RoleId, CurrentUser.Role);
            }
            catch (Exception ex)
            {
                // M4: permission loading failure must be visible; the user keeps no permissions.
                App.Log("Failed to load permissions for user " + CurrentUser?.Id + ": " + ex.Message);
            }
        }

        /// <summary>
        /// هل يملك المستخدم صلاحية معيّنة؟
        /// يدعم المفاتيح القديمة (ما قبل نظام الأدوار) عبر <see cref="PermissionCatalog.LegacyAliases"/>.
        /// </summary>
        public static bool HasPermission(string key)
        {
            if (!IsLoggedIn) return false;
            if (string.IsNullOrWhiteSpace(key)) return false;

            // دور ADMIN يملك كل شيء
            if (string.Equals(CurrentUser.Role, "ADMIN", StringComparison.OrdinalIgnoreCase)) return true;

            if (_permissions.Contains(key)) return true;

            // مفتاح قديم ← الصلاحيات الحديثة المقابلة
            if (Techtoon.Core.Permissions.PermissionCatalog.LegacyAliases.TryGetValue(key, out var modern))
                foreach (var m in modern)
                    if (_permissions.Contains(m)) return true;

            // صلاحية حديثة ← المفتاح القديم الممنوح (توافق مع الصلاحيات القديمة المخزّنة)
            if (Techtoon.Core.Permissions.PermissionCatalog.ReverseAliases.TryGetValue(key, out var legacy))
                foreach (var l in legacy)
                    if (_permissions.Contains(l)) return true;

            return false;
        }

        /// <summary>الصلاحيات الفعلية للمستخدم الحالي (بعد الدور والاستثناءات).</summary>
        public static IReadOnlyCollection<string> GetEffectivePermissions() => _permissions.ToList();

        /// <summary>يعيد تحميل صلاحيات المستخدم الحالي (بعد تغيير دوره أو استثناءاته).</summary>
        public static void ReloadPermissions() => LoadPermissions();

        /// <summary>اسم دور المستخدم الحالي.</summary>
        public static string CurrentRoleName => CurrentUser?.Role ?? "";

        /// <summary>اسم دور المستخدم الحالي (للعرض).</summary>
        public static string CurrentRoleDisplayName
        {
            get
            {
                if (CurrentUser == null) return "";
                if (!string.IsNullOrWhiteSpace(CurrentUser.RoleDisplayName)) return CurrentUser.RoleDisplayName;
                return CurrentUser.Role ?? "";
            }
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
            catch (Exception ex)
            {
                App.Log("Audit log write failed for action " + action + ": " + ex.Message);
            }
        }

        public static async Task EnsureDefaultAdminAsync()
        {
            // M1: never auto-seed a well-known admin/admin123 account. A bootstrap admin is
            // created only when the operator supplies a strong password via the environment.
            const string envVar = "TECHTOON_BOOTSTRAP_ADMIN_PASSWORD";
            try
            {
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM users WHERE username='admin'", conn))
                    {
                        if (Convert.ToInt32(cmd.ExecuteScalar()) > 0) return;
                    }

                    var bootstrapPassword = Environment.GetEnvironmentVariable(envVar);
                    if (string.IsNullOrWhiteSpace(bootstrapPassword) || bootstrapPassword.Length < 12)
                    {
                        App.Log("No admin user exists. Set " + envVar + " (min 12 chars) once to bootstrap an admin account.");
                        return;
                    }

                    string hash = BCrypt.Net.BCrypt.HashPassword(bootstrapPassword, workFactor: 12);
                    using (var c2 = new NpgsqlCommand(
                        "INSERT INTO users (username, password_hash, full_name, role, role_id, is_active, created_at) " +
                        "VALUES ('admin', @h, 'Administrator', 'ADMIN', (SELECT id FROM roles WHERE name='ADMIN'), TRUE, NOW())", conn))
                    {
                        c2.Parameters.AddWithValue("h", hash);
                        c2.ExecuteNonQuery();
                    }
                    App.Log("Bootstrap admin account created from " + envVar + " (change the password after first login).");
                }
            }
            catch (Exception ex)
            {
                App.Log("EnsureDefaultAdminAsync failed: " + ex.Message);
            }
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
                    using (var cmd = new NpgsqlCommand("INSERT INTO users (username, password_hash, full_name, role, role_id, is_active, created_at) VALUES (@u, @h, @fn, @r, (SELECT id FROM roles WHERE name=@r), TRUE, NOW())", conn))
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
            return RoleService.GetUserOverrides(userId);
        }

        public static void SetUserPermissions(int userId, System.Collections.Generic.Dictionary<string, bool> permissions)
        {
            RoleService.SetUserOverrides(userId, permissions);
            LogAudit("SET_PERMISSIONS", "user", userId, "Permission overrides updated: " + (permissions?.Count ?? 0));
        }
        /// <summary>كل مفاتيح الصلاحيات المتاحة في النظام (من الكتالوج المركزي).</summary>
        public static System.Collections.Generic.List<string> GetAllPermissionKeys()
        {
            return new System.Collections.Generic.List<string>(Techtoon.Core.Permissions.PermissionCatalog.AllKeys);
        }

        // كل مفاتيح الصلاحيات المتاحة في النظام

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