using FintechPayments.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FintechPayments.Infrastructure.Health;

internal sealed class DatabaseConnectivityHealthCheck(PaymentsDbContext context) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext healthContext, CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("PostgreSQL accepted a connection.")
                : HealthCheckResult.Unhealthy("PostgreSQL connection failed.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL connection failed.", exception);
        }
    }
}
