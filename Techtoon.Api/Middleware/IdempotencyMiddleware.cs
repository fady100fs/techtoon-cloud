using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.IO;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Techtoon.Api.Middleware;

/// <summary>
/// H7: idempotent financial writes.
///
/// The key is claimed atomically (INSERT ... ON CONFLICT DO NOTHING) *before* the
/// request runs, so two concurrent retries cannot both reach the handler. The stored
/// request hash and endpoint are compared on replay; a reused key with a different
/// payload is rejected with 409 instead of returning a misleading cached response.
/// Only successful responses are persisted for 24h — errors release the key so the
/// caller can retry immediately. The authenticated user id is recorded.
/// </summary>
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
        if (!IsWriteMethod(context.Request.Method) ||
            !context.Request.Headers.TryGetValue("Idempotency-Key", out var keyHeader) ||
            string.IsNullOrWhiteSpace(keyHeader))
        {
            await _next(context);
            return;
        }

        var idempotencyKey = keyHeader.ToString().Trim();
        if (idempotencyKey.Length > 100) idempotencyKey = idempotencyKey.Substring(0, 100);

        var endpoint = context.Request.Path.Value ?? "";
        if (endpoint.Length > 200) endpoint = endpoint.Substring(0, 200);

        // H7: user id can only be trusted after authentication; Program.cs places this
        // middleware after UseAuthentication.
        var userId = int.TryParse(context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid)
            ? uid
            : (int?)null;

        context.Request.EnableBuffering();
        string requestHash;
        using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true))
        {
            var body = await reader.ReadToEndAsync();
            requestHash = ComputeSha256((context.Request.Method ?? "") + "\n" + endpoint + "\n" + body);
            context.Request.Body.Position = 0;
        }

        if (string.IsNullOrEmpty(_connStr))
        {
            await _next(context);
            return;
        }

        Existing? existing = null;
        try
        {
            existing = await GetExistingAsync(idempotencyKey);
        }
        catch (Exception ex)
        {
            // Fail open on infrastructure problems: never block a legitimate write
            // just because the idempotency table is unreachable.
            _logger.LogWarning(ex, "Idempotency lookup failed for key {Key}; proceeding", idempotencyKey);
            await _next(context);
            return;
        }

        if (existing != null)
        {
            if (existing.RequestHash != requestHash || existing.Endpoint != endpoint)
            {
                await WriteProblemAsync(context, StatusCodes.Status409Conflict,
                    "Idempotency-Key was already used for a different request");
                return;
            }
            if (existing.ResponseStatus == null)
            {
                await WriteProblemAsync(context, StatusCodes.Status409Conflict,
                    "A request with this Idempotency-Key is still in progress");
                return;
            }
            context.Response.StatusCode = existing.ResponseStatus.Value;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(existing.ResponseBody ?? "");
            return;
        }

        // Atomic claim of the key before any side effect runs.
        bool claimed;
        try
        {
            claimed = await TryClaimAsync(idempotencyKey, userId, endpoint, requestHash);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Idempotency claim failed for key {Key}; proceeding", idempotencyKey);
            await _next(context);
            return;
        }

        if (!claimed)
        {
            // Lost the race with an identical concurrent request.
            var raced = await GetExistingAsync(idempotencyKey);
            if (raced?.ResponseStatus != null && raced.RequestHash == requestHash && raced.Endpoint == endpoint)
            {
                context.Response.StatusCode = raced.ResponseStatus.Value;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(raced.ResponseBody ?? "");
            }
            else
            {
                await WriteProblemAsync(context, StatusCodes.Status409Conflict,
                    "A request with this Idempotency-Key is still in progress");
            }
            return;
        }

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

        var status = context.Response.StatusCode;
        if (status >= 400)
        {
            // Do not pin failures for 24 hours; release the key for a clean retry.
            await ReleaseAsync(idempotencyKey);
        }
        else
        {
            await CompleteAsync(idempotencyKey, status, responseText);
        }

        await responseBody.CopyToAsync(originalBodyStream);
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

    private sealed record Existing(string? Endpoint, string? RequestHash, int? ResponseStatus, string? ResponseBody);

    private async Task<Existing?> GetExistingAsync(string key)
    {
        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        using var cmd = new NpgsqlCommand(
            "SELECT endpoint, request_hash, response_status, response_body FROM idempotency_keys WHERE key = @k AND expires_at > NOW()",
            conn);
        cmd.Parameters.AddWithValue("k", key);
        using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        return new Existing(
            r.IsDBNull(0) ? null : r.GetString(0),
            r.IsDBNull(1) ? null : r.GetString(1),
            r.IsDBNull(2) ? null : r.GetInt32(2),
            r.IsDBNull(3) ? null : r.GetString(3));
    }

    /// <summary>Returns true when this request owns the key; false when another one already does.</summary>
    private async Task<bool> TryClaimAsync(string key, int? userId, string endpoint, string requestHash)
    {
        using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        using var cmd = new NpgsqlCommand(
            @"INSERT INTO idempotency_keys (key, user_id, endpoint, request_hash, response_status, response_body, created_at, expires_at)
              VALUES (@k, @u, @e, @h, NULL, NULL, NOW(), NOW() + INTERVAL '24 hours')
              ON CONFLICT (key) DO NOTHING", conn);
        cmd.Parameters.AddWithValue("k", key);
        cmd.Parameters.AddWithValue("u", (object?)userId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("e", endpoint);
        cmd.Parameters.AddWithValue("h", requestHash);
        return await cmd.ExecuteNonQueryAsync() == 1;
    }

    private async Task CompleteAsync(string key, int status, string body)
    {
        try
        {
            using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(
                "UPDATE idempotency_keys SET response_status=@s, response_body=@b, expires_at = NOW() + INTERVAL '24 hours' WHERE key=@k",
                conn);
            cmd.Parameters.AddWithValue("k", key);
            cmd.Parameters.AddWithValue("s", status);
            cmd.Parameters.AddWithValue("b", body ?? "");
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to store idempotency response for {Key}", key);
        }
    }

    private async Task ReleaseAsync(string key)
    {
        try
        {
            using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand("DELETE FROM idempotency_keys WHERE key=@k AND response_status IS NULL", conn);
            cmd.Parameters.AddWithValue("k", key);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to release idempotency key {Key}", key);
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int status, string detail)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("{\"message\":\"" + detail.Replace("\"", "'") + "\"}");
    }
}
