using FintechPayments.Domain.Common;

namespace FintechPayments.Domain.Transfers;

public sealed class Transfer
{
    private Transfer()
    {
    }

    private Transfer(
        Guid id,
        Guid sourceWalletId,
        Guid destinationWalletId,
        Money money,
        string reference,
        DateTimeOffset createdAt)
    {
        Id = id;
        SourceWalletId = sourceWalletId;
        DestinationWalletId = destinationWalletId;
        Amount = money.Amount;
        Currency = money.Currency.Code;
        Reference = reference;
        Status = TransferStatus.Pending;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid SourceWalletId { get; private set; }
    public Guid DestinationWalletId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public string Reference { get; private set; } = string.Empty;
    public TransferStatus Status { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static Transfer Create(
        Guid sourceWalletId,
        Guid destinationWalletId,
        Money money,
        string reference,
        DateTimeOffset createdAt)
    {
        if (sourceWalletId == Guid.Empty || destinationWalletId == Guid.Empty)
        {
            throw new DomainException("Both wallets are required.");
        }

        if (sourceWalletId == destinationWalletId)
        {
            throw new DomainException("Source and destination wallets must differ.");
        }

        if (string.IsNullOrWhiteSpace(reference) || reference.Length > 100)
        {
            throw new DomainException("Transfer reference is required and cannot exceed 100 characters.");
        }

        DateTimeOffset timestamp = createdAt.ToUniversalTime();
        return new Transfer(
            Guid.NewGuid(), sourceWalletId, destinationWalletId,
            money, reference.Trim(), timestamp);
    }

    public void Complete(DateTimeOffset completedAt)
    {
        if (Status != TransferStatus.Pending)
        {
            throw new DomainException("Only a pending transfer can be completed.");
        }

        Status = TransferStatus.Completed;
        UpdatedAt = completedAt.ToUniversalTime();
    }
}
