using FintechPayments.Domain.Webhooks;

namespace FintechPayments.Application.Abstractions.Persistence;

public interface IWebhookEventRepository
{
    Task<WebhookStoreOutcome> StoreOnceAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken);
}

public sealed record WebhookStoreOutcome(WebhookStoreResult Result, Guid EventId);

public enum WebhookStoreResult
{
    Stored,
    Duplicate,
    PayloadConflict,
}
