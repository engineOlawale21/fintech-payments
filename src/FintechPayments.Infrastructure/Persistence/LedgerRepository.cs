using FintechPayments.Application.Abstractions.Persistence;
using FintechPayments.Domain.Ledger;
using Microsoft.EntityFrameworkCore;

namespace FintechPayments.Infrastructure.Persistence;

internal sealed class LedgerRepository(PaymentsDbContext context) : ILedgerRepository
{
    public async Task<IReadOnlyList<LedgerEntry>> ListWalletEntriesAsync(
        Guid walletId,
        LedgerPosition? before,
        int take,
        CancellationToken cancellationToken)
    {
        IQueryable<LedgerEntry> query = context.LedgerEntries
            .AsNoTracking()
            .Where(entry => entry.WalletId == walletId);

        if (before is not null)
        {
            query = query.Where(entry =>
                entry.CreatedAt < before.CreatedAt ||
                (entry.CreatedAt == before.CreatedAt && entry.Id.CompareTo(before.EntryId) < 0));
        }

        return await query
            .OrderByDescending(entry => entry.CreatedAt)
            .ThenByDescending(entry => entry.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
