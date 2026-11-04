using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Techtoon.Api.HealthChecks;

public class NeonDbHealthCheck : IHealthCheck
{
    private readonly string _connectionString;

    public NeonDbHealthCheck(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_connectionString))
        {
            return HealthCheckResult.Unhealthy("Connection string is not configured");
        }

        try
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            using var cmd = new NpgsqlCommand("SELECT 1", conn);
            await cmd.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy("Neon PostgreSQL is reachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Neon PostgreSQL is unreachable", ex);
        }
    }
}