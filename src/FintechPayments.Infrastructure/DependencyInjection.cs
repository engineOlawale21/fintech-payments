using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FintechPayments.Infrastructure.Configuration;
using FintechPayments.Infrastructure.Health;
using FintechPayments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using FintechPayments.Application.Abstractions.Authentication;
using FintechPayments.Application.Abstractions.Persistence;
using FintechPayments.Application.Abstractions.Time;
using FintechPayments.Domain.Users;
using FintechPayments.Infrastructure.Authentication;
using FintechPayments.Infrastructure.Time;
using FintechPayments.Application.Abstractions.Webhooks;
using FintechPayments.Infrastructure.Webhooks;
using Microsoft.AspNetCore.Identity;
using StackExchange.Redis;

namespace FintechPayments.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<RedisOptions>()
            .Bind(configuration.GetSection(RedisOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<IdempotencyOptions>()
            .Bind(configuration.GetSection(IdempotencyOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<WebhookOptions>()
            .Bind(configuration.GetSection(WebhookOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddDbContext<PaymentsDbContext>((provider, options) =>
        {
            DatabaseOptions database = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options.UseNpgsql(database.ConnectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(PaymentsDbContext).Assembly.FullName));
        });
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            RedisOptions redis = provider.GetRequiredService<IOptions<RedisOptions>>().Value;
            return ConnectionMultiplexer.Connect(redis.ConnectionString);
        });
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<IWalletQuery, CachedWalletQuery>();
        services.AddScoped<WalletCacheInvalidator>();
        services.AddScoped<ILedgerRepository, LedgerRepository>();
        services.AddScoped<ITransferExecutor, TransferExecutor>();
        services.AddScoped<ITransferReader, TransferReader>();
        services.AddScoped<IWebhookEventRepository, WebhookEventRepository>();
        services.AddScoped<IWebhookSignatureVerifier, MockProviderWebhookSignatureVerifier>();
        services.AddScoped<IReconciliationRepository, ReconciliationRepository>();
        services.AddScoped<ISettlementRepository, SettlementRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddSingleton<IClock, SystemClock>();

        IHealthChecksBuilder healthChecks = services
            .AddHealthChecks()
            .AddCheck<ConfiguredDependenciesHealthCheck>(
                "dependency_configuration", tags: ["ready"]);
        if (!configuration.GetValue<bool>("Testing:SkipDependencyConnectivityChecks"))
        {
            healthChecks
                .AddCheck<DatabaseConnectivityHealthCheck>("postgresql", tags: ["ready"])
                .AddCheck<RedisConnectivityHealthCheck>("redis", tags: ["ready"]);
        }

        return services;
    }
}
