using FintechPayments.Application.Abstractions.Persistence;
using FintechPayments.Domain.Wallets;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FintechPayments.Infrastructure.Persistence;

internal sealed class WalletRepository(
    PaymentsDbContext context,
    WalletCacheInvalidator cache) : IWalletRepository
{
    public async Task<bool> CreateIfCurrencyAvailableAsync(
        Wallet wallet,
        CancellationToken cancellationToken)
    {
        context.Wallets.Add(wallet);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            await cache.InvalidateAsync(wallet.OwnerId, wallet.Id);
            return true;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            context.Entry(wallet).State = EntityState.Detached;
            return false;
        }
    }

    public Task<Wallet?> FindOwnedAsync(
        Guid walletId,
        Guid ownerId,
        CancellationToken cancellationToken) =>
        context.Wallets
            .AsNoTracking()
            .SingleOrDefaultAsync(
                wallet => wallet.Id == walletId && wallet.OwnerId == ownerId,
                cancellationToken);

    public async Task<IReadOnlyList<Wallet>> ListOwnedAsync(
        Guid ownerId,
        CancellationToken cancellationToken) =>
        await context.Wallets
            .AsNoTracking()
            .Where(wallet => wallet.OwnerId == ownerId)
            .OrderBy(wallet => wallet.Currency)
            .ToListAsync(cancellationToken);
}
