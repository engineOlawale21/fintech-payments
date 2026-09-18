using FintechPayments.Application.Abstractions.Persistence;
using FintechPayments.Domain.Webhooks;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FintechPayments.Infrastructure.Persistence;

public sealed class WebhookEventRepository(PaymentsDbContext dbContext) : IWebhookEventRepository
{
    public async Task<WebhookStoreOutcome> StoreOnceAsync(
        WebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        dbContext.WebhookEvents.Add(webhookEvent);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return new WebhookStoreOutcome(WebhookStoreResult.Stored, webhookEvent.Id);
        }
        catch (DbUpdateException exception) when
            (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            dbContext.Entry(webhookEvent).State = EntityState.Detached;
            var existing = await dbContext.WebhookEvents
                .AsNoTracking()
                .Where(x => x.Provider == webhookEvent.Provider &&
                            x.ExternalEventId == webhookEvent.ExternalEventId)
                .Select(x => new { x.Id, x.PayloadHash })
                .SingleOrDefaultAsync(cancellationToken);
            return existing is not null && string.Equals(
                    existing.PayloadHash, webhookEvent.PayloadHash, StringComparison.Ordinal)
                ? new WebhookStoreOutcome(WebhookStoreResult.Duplicate, existing.Id)
                : new WebhookStoreOutcome(WebhookStoreResult.PayloadConflict, Guid.Empty);
        }
    }
}
