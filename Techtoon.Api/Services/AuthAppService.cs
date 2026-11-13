using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using Techtoon.Core.Permissions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Techtoon.Api.DTOs.Auth;
using Techtoon.Api.Options;

namespace Techtoon.Api.Services;

public interface IAuthAppService
{
    Task<TokenResponseDto> LoginAsync(LoginRequestDto request, string ipAddress);
    Task<TokenResponseDto> RefreshAsync(string refreshToken, string ipAddress);
    Task LogoutAsync(int userId, string deviceId);
    Task<List<DeviceDto>> GetDevicesAsync(int userId);
    Task<bool> RevokeDeviceAsync(int userId, int deviceRecordId);
    Task<UserDto> GetCurrentUserAsync(int userId);
}

public class AuthAppService : IAuthAppService
{
    private readonly string _connStr;
    private readonly IJwtTokenService _jwt;
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<AuthAppService> _logger;

    public AuthAppService(IConfiguration config, IJwtTokenService jwt, JwtOptions jwtOptions, ILogger<AuthAppService> logger)
    {
        _connStr = config.GetConnectionString("DefaultConnection") ?? "";
        _jwt = jwt;
        _jwtOptions = jwtOptions;
        _logger = logger;
    }

    /// <summary>C2: reject missing/empty/malformed bcrypt hashes (fail closed).</summary>
    internal static bool IsValidPasswordHash(string? hash)
    {
        if (string.IsNullOrWhiteSpace(hash)) return false;
        if (hash.Length != 60) return false;
        if (!(hash.StartsWith("$2a$") || hash.StartsWith("$2b$") || hash.StartsWith("$2y$"))) return false;
        return true;
    }

    public async Task<TokenResponseDto> LoginAsync(LoginRequestDto request, string ipAddress)
    {
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            throw new ArgumentException("Username and password are required");

        var deviceId = string.IsNullOrEmpty(request.DeviceId) ? Guid.NewGuid().ToString() : request.DeviceId;

        // Rate limit: max 5 failed attempts in 5 minutes
        await CheckRateLimitAsync(request.Username, ipAddress);

        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();

        int userId;
        string username, fullName, role, hash;
        bool isActive;

        using (var cmd = new NpgsqlCommand(
            "SELECT id, username, COALESCE(password_hash,''), COALESCE(full_name,''), COALESCE(role,'SALESPERSON'), COALESCE(is_active, FALSE) FROM users WHERE username=@u",
            conn))
        {
            cmd.Parameters.AddWithValue("u", request.Username);
            using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync())
            {
                await LogLoginAttemptAsync(request.Username, ipAddress, false, "User not found");
                throw new UnauthorizedAccessException("Invalid username or password");
            }
            userId = r.GetInt32(0);
            username = r.GetString(1);
            hash = r.GetString(2);
            fullName = r.IsDBNull(3) ? "" : r.GetString(3);
            role = r.IsDBNull(4) ? "SALESPERSON" : r.GetString(4);
            isActive = r.IsDBNull(5) ? false : r.GetBoolean(5);
        }

        if (!isActive)
        {
            await LogLoginAttemptAsync(username, ipAddress, false, "Account disabled");
            throw new UnauthorizedAccessException("Account is disabled");
        }

        // C2: fail closed on missing/empty/malformed password hashes.
        // A user row without a usable bcrypt hash must never authenticate.
        bool passwordOk;
        try
        {
            passwordOk = IsValidPasswordHash(hash) &&
                BCrypt.Net.BCrypt.Verify(request.Password ?? string.Empty, hash);
        }
        catch { passwordOk = false; }

        if (!passwordOk)
        {
            await LogLoginAttemptAsync(username, ipAddress, false, "Wrong password");
            throw new UnauthorizedAccessException("Invalid username or password");
        }

        // Load permissions
        var permissions = await LoadPermissionsAsync(conn, userId, role);

        // Generate tokens
        var (accessToken, refreshToken) = _jwt.GenerateTokens(userId, username, role, deviceId, permissions);

        // Store device + refresh token hash
        await StoreDeviceAsync(conn, userId, deviceId, request.DeviceName, refreshToken);

        // Update last_login
        using (var cmd = new NpgsqlCommand("UPDATE users SET last_login=NOW() WHERE id=@id", conn))
        {
            cmd.Parameters.AddWithValue("id", userId);
            await cmd.ExecuteNonQueryAsync();
        }

        await LogAuditAsync(conn, userId, username, "login", "user", userId, "Login from " + ipAddress + ", device " + deviceId);
        await LogLoginAttemptAsync(username, ipAddress, true, null);

        return new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenMinutes),
            User = new UserDto
            {
                Id = userId,
                Username = username,
                FullName = fullName,
                Role = role,
                Permissions = permissions
            }
        };
    }

    public async Task<TokenResponseDto> RefreshAsync(string refreshToken, string ipAddress)
    {
        if (string.IsNullOrEmpty(refreshToken))
            throw new ArgumentException("Refresh token is required");

        var refreshHash = _jwt.HashToken(refreshToken);

        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();

        int userId = 0;
        string username = "", fullName = "", role = "", deviceId = "";
        bool found;

        using (var cmd = new NpgsqlCommand(
            "SELECT ud.id, ud.user_id, u.username, COALESCE(u.full_name,''), COALESCE(u.role,'SALESPERSON'), ud.device_id FROM user_devices ud JOIN users u ON ud.user_id = u.id " +
            "WHERE ud.refresh_token_hash = @h AND ud.is_revoked = FALSE AND u.is_active = TRUE " +
            "AND ud.last_seen > NOW() - make_interval(days => @days)",
            conn))
        {
            cmd.Parameters.AddWithValue("h", refreshHash);
            cmd.Parameters.AddWithValue("days", Math.Max(1, _jwtOptions.RefreshTokenDays));
            using var r = await cmd.ExecuteReaderAsync();
            found = await r.ReadAsync();
            if (found)
            {
                userId = r.GetInt32(1);
                username = r.GetString(2);
                fullName = r.IsDBNull(3) ? "" : r.GetString(3);
                role = r.IsDBNull(4) ? "SALESPERSON" : r.GetString(4);
                deviceId = r.GetString(5);
            }
        }

        if (!found)
        {
            // H9: expire stale refresh tokens instead of accepting them forever.
            using var expire = new NpgsqlCommand(
                "UPDATE user_devices SET is_revoked = TRUE WHERE refresh_token_hash = @h AND last_seen <= NOW() - make_interval(days => @days)",
                conn);
            expire.Parameters.AddWithValue("h", refreshHash);
            expire.Parameters.AddWithValue("days", Math.Max(1, _jwtOptions.RefreshTokenDays));
            await expire.ExecuteNonQueryAsync();
            throw new UnauthorizedAccessException("Invalid or expired refresh token");
        }

        var permissions = await LoadPermissionsAsync(conn, userId, role);
        var (newAccess, newRefresh) = _jwt.GenerateTokens(userId, username, role, deviceId, permissions);

        // Rotate refresh token: update hash
        var newRefreshHash = _jwt.HashToken(newRefresh);
        using (var cmd = new NpgsqlCommand(
            "UPDATE user_devices SET refresh_token_hash=@h, last_seen=NOW() WHERE user_id=@uid AND device_id=@did AND is_revoked=FALSE",
            conn))
        {
            cmd.Parameters.AddWithValue("h", newRefreshHash);
            cmd.Parameters.AddWithValue("uid", userId);
            cmd.Parameters.AddWithValue("did", deviceId);
            await cmd.ExecuteNonQueryAsync();
        }

        await LogAuditAsync(conn, userId, username, "refresh_token", "user", userId, "Token refreshed from " + ipAddress);

        return new TokenResponseDto
        {
            AccessToken = newAccess,
            RefreshToken = newRefresh,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenMinutes),
            User = new UserDto
            {
                Id = userId,
                Username = username,
                FullName = fullName,
                Role = role,
                Permissions = permissions
            }
        };
    }

    public async Task LogoutAsync(int userId, string deviceId)
    {
        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        using var cmd = new NpgsqlCommand(
            "UPDATE user_devices SET is_revoked=TRUE WHERE user_id=@uid AND device_id=@did",
            conn);
        cmd.Parameters.AddWithValue("uid", userId);
        cmd.Parameters.AddWithValue("did", deviceId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<DeviceDto>> GetDevicesAsync(int userId)
    {
        var list = new List<DeviceDto>();
        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        using var cmd = new NpgsqlCommand(
            "SELECT id, device_id, COALESCE(device_name,''), last_seen, created_at, is_revoked FROM user_devices WHERE user_id=@uid ORDER BY last_seen DESC",
            conn);
        cmd.Parameters.AddWithValue("uid", userId);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            list.Add(new DeviceDto
            {
                Id = r.GetInt32(0),
                DeviceId = r.GetString(1),
                DeviceName = r.IsDBNull(2) ? null : r.GetString(2),
                LastSeen = r.GetDateTime(3),
                CreatedAt = r.GetDateTime(4),
                IsRevoked = r.GetBoolean(5)
            });
        }
        return list;
    }

    public async Task<bool> RevokeDeviceAsync(int userId, int deviceRecordId)
    {
        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        using var cmd = new NpgsqlCommand(
            "UPDATE user_devices SET is_revoked=TRUE WHERE id=@id AND user_id=@uid",
            conn);
        cmd.Parameters.AddWithValue("id", deviceRecordId);
        cmd.Parameters.AddWithValue("uid", userId);
        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0;
    }

    public async Task<UserDto> GetCurrentUserAsync(int userId)
    {
        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        using var cmd = new NpgsqlCommand(
            "SELECT id, username, COALESCE(full_name,''), COALESCE(role,'SALESPERSON') FROM users WHERE id=@id",
            conn);
        cmd.Parameters.AddWithValue("id", userId);
        using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync())
            throw new UnauthorizedAccessException("User not found");

        var user = new UserDto
        {
            Id = r.GetInt32(0),
            Username = r.GetString(1),
            FullName = r.IsDBNull(2) ? "" : r.GetString(2),
            Role = r.IsDBNull(3) ? "SALESPERSON" : r.GetString(3)
        };
        r.Close();

        user.Permissions = await LoadPermissionsAsync(conn, userId, user.Role);
        return user;
    }

    private async Task<List<string>> LoadPermissionsAsync(NpgsqlConnection conn, int userId, string? roleName = null)
    {
        var perms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1) دور ADMIN يملك كل الصلاحيات
        if (string.Equals(roleName?.Trim(), "ADMIN", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var k in PermissionCatalog.AllKeys) perms.Add(k);
        }
        else
        {
            // 2) صلاحيات الدور (من role_permissions) — إما عبر users.role_id أو باسم الدور
            using (var cmd = new NpgsqlCommand(
                "SELECT rp.permission_key FROM role_permissions rp " +
                "JOIN roles r ON r.id = rp.role_id " +
                "LEFT JOIN users u ON u.role_id = r.id AND u.id = @id " +
                "WHERE rp.is_granted = TRUE AND (u.id = @id OR UPPER(r.name) = UPPER(@role))",
                conn))
            {
                cmd.Parameters.AddWithValue("id", userId);
                cmd.Parameters.AddWithValue("role", roleName ?? "");
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                    perms.Add(r.GetString(0));
            }
        }

        // 3) استثناءات المستخدم فوق الدور: المنح يضيف والمنع يسحب
        using (var cmd = new NpgsqlCommand(
            "SELECT permission_key, COALESCE(is_granted, TRUE) FROM user_permissions WHERE user_id=@id",
            conn))
        {
            cmd.Parameters.AddWithValue("id", userId);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                var key = r.GetString(0);
                bool granted = r.GetBoolean(1);
                if (granted) perms.Add(key);
                else perms.Remove(key);
            }
        }

        return perms.ToList();
    }

    private async Task StoreDeviceAsync(NpgsqlConnection conn, int userId, string deviceId, string? deviceName, string refreshToken)
    {
        var refreshHash = _jwt.HashToken(refreshToken);

        // تحديث الجهاز إن كان موجودًا (لا نستخدم ON CONFLICT: لا يوجد قيد فريد
        // على (user_id, device_id) في المخطط ⇒ كان الاستيراد يفشل بـ 42P10).
        using (var upd = new NpgsqlCommand(
            "UPDATE user_devices SET refresh_token_hash=@h, device_name=@dname, last_seen=NOW(), is_revoked=FALSE " +
            "WHERE user_id=@uid AND device_id=@did", conn))
        {
            upd.Parameters.AddWithValue("uid", userId);
            upd.Parameters.AddWithValue("did", deviceId);
            upd.Parameters.AddWithValue("dname", (object?)deviceName ?? DBNull.Value);
            upd.Parameters.AddWithValue("h", refreshHash);
            if (await upd.ExecuteNonQueryAsync() > 0) return;
        }

        using var cmd = new NpgsqlCommand(
            "INSERT INTO user_devices (user_id, device_id, device_name, refresh_token_hash, last_seen, is_revoked, created_at) " +
            "VALUES (@uid, @did, @dname, @h, NOW(), FALSE, NOW())",
            conn);
        cmd.Parameters.AddWithValue("uid", userId);
        cmd.Parameters.AddWithValue("did", deviceId);
        cmd.Parameters.AddWithValue("dname", (object?)deviceName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("h", refreshHash);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task CheckRateLimitAsync(string username, string ipAddress)
    {
        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        using var cmd = new NpgsqlCommand(
            "SELECT COUNT(*) FROM login_attempts WHERE (username=@u OR ip_address=@ip) AND success = FALSE AND attempted_at > NOW() - INTERVAL '5 minutes'",
            conn);
        cmd.Parameters.AddWithValue("u", username);
        cmd.Parameters.AddWithValue("ip", ipAddress);
        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        if (count >= 5)
            throw new UnauthorizedAccessException("Too many failed login attempts. Try again in 5 minutes.");
    }

    private async Task LogLoginAttemptAsync(string? username, string ipAddress, bool success, string? reason)
    {
        try
        {
            using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(
                "INSERT INTO login_attempts (username, ip_address, success, failure_reason, attempted_at) VALUES (@u, @ip, @s, @r, NOW())",
                conn);
            cmd.Parameters.AddWithValue("u", (object?)username ?? DBNull.Value);
            cmd.Parameters.AddWithValue("ip", ipAddress);
            cmd.Parameters.AddWithValue("s", success);
            cmd.Parameters.AddWithValue("r", (object?)reason ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            // H5: a failed login-attempt write must never disable rate limiting silently.
            _logger.LogError(ex, "Failed to record login attempt for user {User} from {Ip}", username, ipAddress);
        }
    }

    private async Task LogAuditAsync(NpgsqlConnection conn, int userId, string username, string action, string entityType, int entityId, string details)
    {
        try
        {
            using var cmd = new NpgsqlCommand(
                "INSERT INTO audit_log (user_id, username, action, entity_type, entity_id, details, created_at) VALUES (@uid, @uname, @action, @etype, @eid, @details, NOW())",
                conn);
            cmd.Parameters.AddWithValue("uid", userId);
            cmd.Parameters.AddWithValue("uname", username);
            cmd.Parameters.AddWithValue("action", action);
            cmd.Parameters.AddWithValue("etype", entityType);
            cmd.Parameters.AddWithValue("eid", entityId);
            cmd.Parameters.AddWithValue("details", details);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log for action {Action} entity {Entity}", action, entityType);
        }
    }
}