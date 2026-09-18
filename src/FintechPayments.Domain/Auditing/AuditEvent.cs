using FintechPayments.Domain.Common;

namespace FintechPayments.Domain.Auditing;

public sealed class AuditEvent
{
    private AuditEvent() { }
    private AuditEvent(
        Guid id, Guid? actorId, string action, string entityType, Guid? entityId,
        string outcome, string correlationId, string metadata, DateTimeOffset createdAt)
    {
        Id = id;
        ActorId = actorId;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        Outcome = outcome;
        CorrelationId = correlationId;
        Metadata = metadata;
        CreatedAt = createdAt.ToUniversalTime();
    }

    public Guid Id { get; private set; }
    public Guid? ActorId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public Guid? EntityId { get; private set; }
    public string Outcome { get; private set; } = string.Empty;
    public string CorrelationId { get; private set; } = string.Empty;
    public string Metadata { get; private set; } = "{}";
    public DateTimeOffset CreatedAt { get; private set; }

    public static AuditEvent Create(
        Guid? actorId, string action, string entityType, Guid? entityId,
        string outcome, string correlationId, string metadata, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(action) || action.Length > 100 ||
            string.IsNullOrWhiteSpace(entityType) || entityType.Length > 64 ||
            string.IsNullOrWhiteSpace(outcome) || outcome.Length > 32 ||
            string.IsNullOrWhiteSpace(correlationId) || correlationId.Length > 128 ||
            string.IsNullOrWhiteSpace(metadata))
        {
            throw new DomainException("Audit event is invalid.");
        }
        return new AuditEvent(
            Guid.NewGuid(), actorId, action, entityType, entityId, outcome,
            correlationId, metadata, createdAt);
    }
}
