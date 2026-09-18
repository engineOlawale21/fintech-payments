using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace FintechPayments.Infrastructure.Health;

internal sealed class RedisConnectivityHealthCheck(IConnectionMultiplexer redis) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            TimeSpan latency = await redis.GetDatabase().PingAsync();
            return HealthCheckResult.Healthy(
                "Redis responded to PING.", new Dictionary<string, object> { ["latency_ms"] = latency.TotalMilliseconds });
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Redis PING failed.", exception);
        }
    }
}
