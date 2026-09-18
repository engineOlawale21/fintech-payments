using System.Text.Json;
using FintechPayments.Application.Abstractions.Persistence;
using FintechPayments.Application.Abstractions.Time;
using FintechPayments.Domain.Auditing;
using Microsoft.Extensions.Logging;

namespace FintechPayments.Application.Auditing;

public sealed class AuditService(
    IAuditRepository repository, IClock clock, ILogger<AuditService> logger)
{
    private static readonly Action<ILogger, string, Exception?> AppendFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(3101, nameof(AppendFailed)),
            "Audit append failed for action {AuditAction}");

    public async Task RecordAsync(
        Guid? actorId, string action, string entityType, Guid? entityId,
        string outcome, string correlationId, string? errorCode,
        CancellationToken cancellationToken)
    {
        string metadata = JsonSerializer.Serialize(new { errorCode });
        AuditEvent auditEvent = AuditEvent.Create(
            actorId, action, entityType, entityId, outcome,
            correlationId, metadata, clock.UtcNow);
        try
        {
            await repository.AppendAsync(auditEvent, cancellationToken);
        }
        catch (Exception exception)
        {
            AppendFailed(logger, action, exception);
        }
    }

    public Task<IReadOnlyList<AuditEvent>> ListAsync(
        string? correlationId, DateTimeOffset? before, int pageSize,
        CancellationToken cancellationToken)
    {
        int size = pageSize is >= 1 and <= 100 ? pageSize : 50;
        return repository.ListAsync(correlationId, before, size, cancellationToken);
    }
}
