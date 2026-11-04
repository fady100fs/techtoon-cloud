 $ErrorActionPreference = "Stop"
 $root = "D:\PROG\Accounting Prog\Techtoon Accounting C+"
 $apiDir = Join-Path $root "Techtoon.Api"
 $utf8 = New-Object System.Text.UTF8Encoding($false)

function WF($path, $content) {
    [System.IO.File]::WriteAllText($path, $content, $utf8)
    $rel = $path.Replace($root + '\', '')
    Write-Host "  + $rel" -ForegroundColor Green
}

Write-Host "=== B-2b: Idempotency + Auth + Migration ===" -ForegroundColor Cyan
Write-Host ""

# 1. Migrations folder + SQL
Write-Host "[1/7] Creating migration SQL..." -ForegroundColor Yellow
New-Item -ItemType Directory -Path "$apiDir\Migrations" -Force | Out-Null

 $migration = @'
-- ============================================================================
-- Techtoon API B-2b: Auth Security & Idempotency Tables
-- SAFE: uses IF NOT EXISTS everywhere. No data loss, no column renames.
-- ============================================================================

-- 1. user_devices: manages logged-in devices (JWT refresh tokens)
CREATE TABLE IF NOT EXISTS user_devices (
    id SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    device_id VARCHAR(100) NOT NULL,
    device_name VARCHAR(200),
    refresh_token_hash VARCHAR(500) NOT NULL,
    last_seen TIMESTAMP DEFAULT NOW(),
    is_revoked BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT NOW(),
    UNIQUE(user_id, device_id)
);
CREATE INDEX IF NOT EXISTS idx_user_devices_user ON user_devices(user_id) WHERE is_revoked = FALSE;
CREATE INDEX IF NOT EXISTS idx_user_devices_refresh ON user_devices(refresh_token_hash);

-- 2. login_attempts: rate limiting + audit
CREATE TABLE IF NOT EXISTS login_attempts (
    id SERIAL PRIMARY KEY,
    username VARCHAR(100),
    ip_address VARCHAR(45),
    success BOOLEAN,
    failure_reason VARCHAR(200),
    attempted_at TIMESTAMP DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_login_attempts_username ON login_attempts(username, attempted_at);
CREATE INDEX IF NOT EXISTS idx_login_attempts_ip ON login_attempts(ip_address, attempted_at);

-- 3. idempotency_keys: prevents duplicate financial operations
CREATE TABLE IF NOT EXISTS idempotency_keys (
    key VARCHAR(100) PRIMARY KEY,
    user_id INTEGER,
    endpoint VARCHAR(200),
    request_hash VARCHAR(64),
    response_status INTEGER,
    response_body TEXT,
    created_at TIMESTAMP DEFAULT NOW(),
    expires_at TIMESTAMP NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_idempotency_expires ON idempotency_keys(expires_at);
CREATE INDEX IF NOT EXISTS idx_idempotency_user ON idempotency_keys(user_id);

-- 4. Ensure audit_log table exists (matches AuthService.cs usage)
CREATE TABLE IF NOT EXISTS audit_log (
    id SERIAL PRIMARY KEY,
    user_id INTEGER,
    username VARCHAR(100),
    action VARCHAR(50),
    entity_type VARCHAR(50),
    entity_id INTEGER,
    details TEXT,
    created_at TIMESTAMP DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_audit_log_user ON audit_log(user_id);
CREATE INDEX IF NOT EXISTS idx_audit_log_created ON audit_log(created_at);
'@
WF "$apiDir\Migrations\01_auth_security.sql" $migration

# 2. IdempotencyMiddleware
Write-Host "[2/7] Creating IdempotencyMiddleware..." -ForegroundColor Yellow
 $idempotency = @'
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Techtoon.Api.Middleware;

public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IdempotencyMiddleware> _logger;
    private readonly string _connStr;

    public IdempotencyMiddleware(RequestDelegate next, ILogger<IdempotencyMiddleware> logger, IConfiguration config)
    {
        _next = next;
        _logger = logger;
        _connStr = config.GetConnectionString("DefaultConnection") ?? "";
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only apply to POST/PUT/DELETE with Idempotency-Key header
        if (!IsWriteMethod(context.Request.Method) ||
            !context.Request.Headers.TryGetValue("Idempotency-Key", out var key) ||
            string.IsNullOrEmpty(key))
        {
            await _next(context);
            return;
        }

        var idempotencyKey = key.ToString();

        // Read and buffer request body for hashing
        context.Request.EnableBuffering();
        string requestBodyHash;
        using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true))
        {
            var body = await reader.ReadToEndAsync();
            requestBodyHash = ComputeSha256(body);
            context.Request.Body.Position = 0;
        }

        // Check if key already processed
        var existing = await GetExistingResponseAsync(idempotencyKey);
        if (existing != null)
        {
            _logger.LogInformation("Idempotency hit: key={Key} returning cached response", idempotencyKey);
            context.Response.StatusCode = existing.Value.status;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(existing.Value.body);
            return;
        }

        // Capture response
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }

        responseBody.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(responseBody).ReadToEndAsync();
        responseBody.Seek(0, SeekOrigin.Begin);
        await responseBody.CopyToAsync(originalBodyStream);

        // Store response for future idempotency hits
        await StoreResponseAsync(idempotencyKey, context.Response.StatusCode, responseText, requestBodyHash, context.Request.Path);
    }

    private static bool IsWriteMethod(string method)
    {
        return method.Equals("POST", StringComparison.OrdinalIgnoreCase) ||
               method.Equals("PUT", StringComparison.OrdinalIgnoreCase) ||
               method.Equals("DELETE", StringComparison.OrdinalIgnoreCase) ||
               method.Equals("PATCH", StringComparison.OrdinalIgnoreCase);
    }

    private static string ComputeSha256(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private async Task<(int status, string body)?> GetExistingResponseAsync(string key)
    {
        if (string.IsNullOrEmpty(_connStr)) return null;
        try
        {
            using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(
                "SELECT response_status, response_body FROM idempotency_keys WHERE key = @k AND expires_at > NOW()",
                conn);
            cmd.Parameters.AddWithValue("k", key);
            using var r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync())
            {
                var status = r.IsDBNull(0) ? 200 : r.GetInt32(0);
                var body = r.IsDBNull(1) ? "" : r.GetString(1);
                return (status, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check idempotency key {Key}", key);
        }
        return null;
    }

    private async Task StoreResponseAsync(string key, int status, string body, string requestHash, string endpoint)
    {
        if (string.IsNullOrEmpty(_connStr)) return;
        try
        {
            using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(
                @"INSERT INTO idempotency_keys (key, endpoint, request_hash, response_status, response_body, created_at, expires_at)
                  VALUES (@k, @e, @h, @s, @b, NOW(), NOW() + INTERVAL '24 hours')
                  ON CONFLICT (key) DO NOTHING", conn);
            cmd.Parameters.AddWithValue("k", key);
            cmd.Parameters.AddWithValue("e", endpoint ?? "");
            cmd.Parameters.AddWithValue("h", requestHash ?? "");
            cmd.Parameters.AddWithValue("s", status);
            cmd.Parameters.AddWithValue("b", body ?? "");
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to store idempotency key {Key}", key);
        }
    }
}
'@
WF "$apiDir\Middleware\IdempotencyMiddleware.cs" $idempotency

# 3. DeviceDto
Write-Host "[3/7] Creating DeviceDto..." -ForegroundColor Yellow
 $deviceDto = @'
namespace Techtoon.Api.DTOs.Auth;

public class DeviceDto
{
    public int Id { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public DateTime LastSeen { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRevoked { get; set; }
}
'@
WF "$apiDir\DTOs\Auth\DeviceDto.cs" $deviceDto

# 4. AuthAppService
Write-Host "[4/7] Creating AuthAppService..." -ForegroundColor Yellow
 $authAppSvc = @'
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
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

        if (!string.IsNullOrEmpty(hash) && !BCrypt.Net.BCrypt.Verify(request.Password, hash))
        {
            await LogLoginAttemptAsync(username, ipAddress, false, "Wrong password");
            throw new UnauthorizedAccessException("Invalid username or password");
        }

        // Load permissions
        var permissions = await LoadPermissionsAsync(conn, userId);

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

        int userId;
        string username, fullName, role, deviceId;

        using (var cmd = new NpgsqlCommand(
            "SELECT ud.id, ud.user_id, u.username, COALESCE(u.full_name,''), COALESCE(u.role,'SALESPERSON'), ud.device_id FROM user_devices ud JOIN users u ON ud.user_id = u.id WHERE ud.refresh_token_hash = @h AND ud.is_revoked = FALSE AND u.is_active = TRUE",
            conn))
        {
            cmd.Parameters.AddWithValue("h", refreshHash);
            using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync())
                throw new UnauthorizedAccessException("Invalid or expired refresh token");
            userId = r.GetInt32(1);
            username = r.GetString(2);
            fullName = r.IsDBNull(3) ? "" : r.GetString(3);
            role = r.IsDBNull(4) ? "SALESPERSON" : r.GetString(4);
            deviceId = r.GetString(5);
        }

        var permissions = await LoadPermissionsAsync(conn, userId);
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

        user.Permissions = await LoadPermissionsAsync(conn, userId);
        return user;
    }

    private async Task<List<string>> LoadPermissionsAsync(NpgsqlConnection conn, int userId)
    {
        var perms = new List<string>();
        using var cmd = new NpgsqlCommand(
            "SELECT permission_key FROM user_permissions WHERE user_id=@id AND is_granted=TRUE",
            conn);
        cmd.Parameters.AddWithValue("id", userId);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            perms.Add(r.GetString(0));
        return perms;
    }

    private async Task StoreDeviceAsync(NpgsqlConnection conn, int userId, string deviceId, string? deviceName, string refreshToken)
    {
        var refreshHash = _jwt.HashToken(refreshToken);
        using var cmd = new NpgsqlCommand(
            "INSERT INTO user_devices (user_id, device_id, device_name, refresh_token_hash, last_seen, is_revoked, created_at) VALUES (@uid, @did, @dname, @h, NOW(), FALSE, NOW()) ON CONFLICT (user_id, device_id) DO UPDATE SET refresh_token_hash = EXCLUDED.refresh_token_hash, device_name = EXCLUDED.device_name, last_seen = NOW(), is_revoked = FALSE",
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
        catch { }
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
        catch { }
    }
}
'@
WF "$apiDir\Services\AuthAppService.cs" $authAppSvc

# 5. Rewrite AuthController
Write-Host "[5/7] Rewriting AuthController..." -ForegroundColor Yellow
 $authCtrl = @'
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Techtoon.Api.DTOs.Auth;
using Techtoon.Api.Services;

namespace Techtoon.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthAppService _auth;
    private readonly ICurrentUserService _current;

    public AuthController(IAuthAppService auth, ICurrentUserService current)
    {
        _auth = auth;
        _current = current;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await _auth.LoginAsync(request, ip);
        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await _auth.RefreshAsync(request.RefreshToken, ip);
        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        if (_current.UserId == null || _current.DeviceId == null)
            return Unauthorized();
        await _auth.LogoutAsync(_current.UserId.Value, _current.DeviceId);
        return Ok(new { message = "Logged out successfully" });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        if (_current.UserId == null)
            return Unauthorized();
        var user = await _auth.GetCurrentUserAsync(_current.UserId.Value);
        return Ok(user);
    }

    [HttpGet("devices")]
    [Authorize]
    public async Task<IActionResult> GetMyDevices()
    {
        if (_current.UserId == null)
            return Unauthorized();
        var devices = await _auth.GetDevicesAsync(_current.UserId.Value);
        return Ok(devices);
    }

    [HttpDelete("devices/{id}")]
    [Authorize]
    public async Task<IActionResult> RevokeDevice(int id)
    {
        if (_current.UserId == null)
            return Unauthorized();
        var revoked = await _auth.RevokeDeviceAsync(_current.UserId.Value, id);
        if (!revoked) return NotFound(new { message = "Device not found" });
        return Ok(new { message = "Device revoked" });
    }
}
'@
WF "$apiDir\Controllers\AuthController.cs" $authCtrl

# 6. Update Program.cs - register services + middleware
Write-Host "[6/7] Updating Program.cs..." -ForegroundColor Yellow
 $program = @'
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Techtoon.Api.Extensions;
using Techtoon.Api.HealthChecks;
using Techtoon.Api.Middleware;
using Techtoon.Api.Options;
using Techtoon.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Bind options
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddSingleton(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<JwtOptions>>().Value);

// Core services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();

// Application services
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAuthAppService, AuthAppService>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

// JWT Authentication
builder.Services.AddTechtoonJwt(builder.Configuration);

// Authorization (Permission-based)
builder.Services.AddTechtoonAuthorization();

// CORS (specific origins from config)
builder.Services.AddTechtoonCors(builder.Configuration);

// Swagger with JWT support
builder.Services.AddTechtoonSwagger();

// Health checks (custom Neon PostgreSQL check)
var connStr = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
builder.Services.AddHealthChecks()
    .AddCheck<NeonDbHealthCheck>("neon-postgres", tags: new[] { "db", "neon" });

var app = builder.Build();

// Pipeline
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<IdempotencyMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseCors("TechtoonMobile");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health endpoints
app.MapHealthChecks("/health");

app.Run();
'@
WF "$apiDir\Program.cs" $program

# 7. Build
Write-Host "[7/7] Building..." -ForegroundColor Yellow
Push-Location $root
try {
    $buildOutput = & dotnet build Techtoon.Api 2>&1
    $buildResult = $LASTEXITCODE
    $buildOutput | ForEach-Object { Write-Host $_ }
    if ($buildResult -eq 0) {
        Write-Host ""
        Write-Host "=== B-2b BUILD SUCCEEDED ===" -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "=== B-2b BUILD FAILED ===" -ForegroundColor Red
    }
} finally {
    Pop-Location
}