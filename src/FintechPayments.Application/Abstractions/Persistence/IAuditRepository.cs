using FintechPayments.Domain.Auditing;

namespace FintechPayments.Application.Abstractions.Persistence;

public interface IAuditRepository
{
    Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken);
    Task<IReadOnlyList<AuditEvent>> ListAsync(
        string? correlationId, DateTimeOffset? before, int pageSize,
        CancellationToken cancellationToken);
}
