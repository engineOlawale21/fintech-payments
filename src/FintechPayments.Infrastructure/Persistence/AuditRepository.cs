using FintechPayments.Application.Abstractions.Persistence;
using FintechPayments.Domain.Auditing;
using Microsoft.EntityFrameworkCore;

namespace FintechPayments.Infrastructure.Persistence;

internal sealed class AuditRepository(PaymentsDbContext context) : IAuditRepository
{
    public async Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        context.AuditEvents.Add(auditEvent);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditEvent>> ListAsync(
        string? correlationId, DateTimeOffset? before, int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<AuditEvent> query = context.AuditEvents.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(correlationId))
            query = query.Where(x => x.CorrelationId == correlationId);
        if (before.HasValue) query = query.Where(x => x.CreatedAt < before.Value);
        return await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Take(pageSize).ToListAsync(cancellationToken);
    }
}
