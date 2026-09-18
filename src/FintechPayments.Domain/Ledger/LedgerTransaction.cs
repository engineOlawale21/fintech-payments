using FintechPayments.Domain.Common;

namespace FintechPayments.Domain.Ledger;

public sealed class LedgerTransaction
{
    private readonly List<LedgerEntry> entries = [];

    private LedgerTransaction()
    {
    }

    private LedgerTransaction(
        Guid id,
        Guid? transferId,
        LedgerTransactionType type,
        string reference,
        DateTimeOffset createdAt)
    {
        Id = id;
        TransferId = transferId;
        Type = type;
        Reference = reference;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid? TransferId { get; private set; }
    public LedgerTransactionType Type { get; private set; }
    public string Reference { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyCollection<LedgerEntry> Entries => entries.AsReadOnly();

    public static LedgerTransaction CreateTransfer(
        Guid transferId,
        Guid sourceWalletId,
        Guid destinationWalletId,
        Money money,
        string reference,
        DateTimeOffset createdAt)
    {
        if (transferId == Guid.Empty || sourceWalletId == Guid.Empty || destinationWalletId == Guid.Empty)
        {
            throw new DomainException("Both wallets are required for a ledger transfer.");
        }

        if (sourceWalletId == destinationWalletId)
        {
            throw new DomainException("Source and destination wallets must differ.");
        }

        if (string.IsNullOrWhiteSpace(reference) || reference.Length > 100)
        {
            throw new DomainException("Ledger reference is required and cannot exceed 100 characters.");
        }

        DateTimeOffset timestamp = createdAt.ToUniversalTime();
        LedgerTransaction transaction = new(
            Guid.NewGuid(), transferId, LedgerTransactionType.Transfer, reference.Trim(), timestamp);
        transaction.entries.Add(new LedgerEntry(
            Guid.NewGuid(), sourceWalletId, EntryDirection.Debit,
            money.Amount, money.Currency.Code, timestamp));
        transaction.entries.Add(new LedgerEntry(
            Guid.NewGuid(), destinationWalletId, EntryDirection.Credit,
            money.Amount, money.Currency.Code, timestamp));
        transaction.EnsureBalanced();

        return transaction;
    }

    private void EnsureBalanced()
    {
        foreach (IGrouping<string, LedgerEntry> currencyEntries in entries.GroupBy(entry => entry.Currency))
        {
            decimal debits = currencyEntries
                .Where(entry => entry.Direction == EntryDirection.Debit)
                .Sum(entry => entry.Amount);
            decimal credits = currencyEntries
                .Where(entry => entry.Direction == EntryDirection.Credit)
                .Sum(entry => entry.Amount);

            if (debits != credits)
            {
                throw new DomainException("Ledger transaction is not balanced.");
            }
        }
    }
}
