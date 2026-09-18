using Microsoft.Extensions.DependencyInjection;
using FintechPayments.Application.Authentication;
using FintechPayments.Application.Wallets;
using FintechPayments.Application.Ledger;
using FintechPayments.Application.Transfers;
using FintechPayments.Application.Webhooks;
using FintechPayments.Application.Reconciliation;
using FintechPayments.Application.Settlements;
using FintechPayments.Application.Auditing;

namespace FintechPayments.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<AuthenticationService>();
        services.AddScoped<WalletService>();
        services.AddScoped<LedgerService>();
        services.AddScoped<TransferService>();
        services.AddScoped<WebhookService>();
        services.AddScoped<ReconciliationService>();
        services.AddScoped<SettlementService>();
        services.AddScoped<AuditService>();
        return services;
    }
}
