using FintechPayments.Domain.Common;

namespace FintechPayments.Domain.Auditing;

public sealed class OutboxMessage
{
    private OutboxMessage()
    {
    }

    private OutboxMessage(
        Guid id,
        string messageType,
        string payload,
        DateTimeOffset occurredAt)
    {
        Id = id;
        MessageType = messageType;
        Payload = payload;
        OccurredAt = occurredAt.ToUniversalTime();
    }

    public Guid Id { get; private set; }
    public string MessageType { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public int AttemptCount { get; private set; }

    public static OutboxMessage Create(
        string messageType,
        string payload,
        DateTimeOffset occurredAt)
    {
        if (string.IsNullOrWhiteSpace(messageType) || messageType.Length > 100 ||
            string.IsNullOrWhiteSpace(payload))
        {
            throw new DomainException("Outbox message is invalid.");
        }

        return new OutboxMessage(
            Guid.NewGuid(), messageType.Trim(), payload, occurredAt);
    }
}
