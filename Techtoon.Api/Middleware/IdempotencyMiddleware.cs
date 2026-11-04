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