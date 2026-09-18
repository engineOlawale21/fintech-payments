namespace FintechPayments.Domain.Ledger;

public sealed class LedgerEntry
{
    private LedgerEntry()
    {
    }

    internal LedgerEntry(
        Guid id,
        Guid walletId,
        EntryDirection direction,
        decimal amount,
        string currency,
        DateTimeOffset createdAt)
    {
        Id = id;
        WalletId = walletId;
        Direction = direction;
        Amount = amount;
        Currency = currency;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid LedgerTransactionId { get; private set; }
    public Guid WalletId { get; private set; }
    public EntryDirection Direction { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
}
