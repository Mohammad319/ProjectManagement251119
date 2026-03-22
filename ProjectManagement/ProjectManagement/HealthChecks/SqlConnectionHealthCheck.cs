using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ProjectManagement.HealthChecks;

public sealed class SqlConnectionHealthCheck(string name, string connectionString) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return HealthCheckResult.Unhealthy($"{name} connection string is missing.");

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = 5;

            var result = await command.ExecuteScalarAsync(cancellationToken);
            if (result is int value && value == 1)
                return HealthCheckResult.Healthy($"{name} is reachable.");

            return HealthCheckResult.Degraded($"{name} responded unexpectedly.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"{name} is not reachable.", ex);
        }
    }
}
