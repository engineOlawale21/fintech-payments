using FintechPayments.Infrastructure.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace FintechPayments.Infrastructure.Health;

internal sealed class ConfiguredDependenciesHealthCheck(
    IOptions<DatabaseOptions> databaseOptions,
    IOptions<RedisOptions> redisOptions) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        bool configured =
            !string.IsNullOrWhiteSpace(databaseOptions.Value.ConnectionString) &&
            !string.IsNullOrWhiteSpace(redisOptions.Value.ConnectionString);

        HealthCheckResult result = configured
            ? HealthCheckResult.Healthy("PostgreSQL and Redis configuration is present.")
            : HealthCheckResult.Unhealthy("PostgreSQL or Redis configuration is missing.");

        return Task.FromResult(result);
    }
}
