using FintechPayments.Domain.Common;

namespace FintechPayments.Domain.Payments;

public sealed class IdempotencyRecord
{
    private IdempotencyRecord()
    {
    }

    private IdempotencyRecord(
        Guid id,
        Guid actorId,
        string operation,
        string key,
        string requestHash,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        Id = id;
        ActorId = actorId;
        Operation = operation;
        Key = key;
        RequestHash = requestHash;
        State = IdempotencyState.InProgress;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public Guid Id { get; private set; }
    public Guid ActorId { get; private set; }
    public string Operation { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public string RequestHash { get; private set; } = string.Empty;
    public IdempotencyState State { get; private set; }
    public Guid? TransferId { get; private set; }
    public int? ResponseStatusCode { get; private set; }
    public string? ResponseBody { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }

    public static IdempotencyRecord Reserve(
        Guid actorId,
        string operation,
        string key,
        string requestHash,
        DateTimeOffset createdAt,
        TimeSpan retention)
    {
        if (actorId == Guid.Empty || string.IsNullOrWhiteSpace(operation) ||
            string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(requestHash))
        {
            throw new DomainException("Idempotency reservation is invalid.");
        }

        if (operation.Length > 100 || key.Length > 100 || requestHash.Length != 64)
        {
            throw new DomainException("Idempotency values exceed their allowed format.");
        }

        DateTimeOffset timestamp = createdAt.ToUniversalTime();
        return new IdempotencyRecord(
            Guid.NewGuid(), actorId, operation.Trim(), key.Trim(), requestHash,
            timestamp, timestamp.Add(retention));
    }

    public void Complete(Guid transferId, string responseBody, DateTimeOffset completedAt)
    {
        if (State != IdempotencyState.InProgress || transferId == Guid.Empty)
        {
            throw new DomainException("Idempotency record cannot be completed.");
        }

        TransferId = transferId;
        ResponseStatusCode = 201;
        ResponseBody = responseBody;
        State = IdempotencyState.Completed;
        UpdatedAt = completedAt.ToUniversalTime();
    }
}
