using FintechPayments.Domain.Ledger;

namespace FintechPayments.Application.Abstractions.Persistence;

public interface ILedgerRepository
{
    Task<IReadOnlyList<LedgerEntry>> ListWalletEntriesAsync(
        Guid walletId,
        LedgerPosition? before,
        int take,
        CancellationToken cancellationToken);
}

public sealed record LedgerPosition(DateTimeOffset CreatedAt, Guid EntryId);
