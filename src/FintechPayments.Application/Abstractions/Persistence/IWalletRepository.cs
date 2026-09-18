using FintechPayments.Domain.Wallets;

namespace FintechPayments.Application.Abstractions.Persistence;

public interface IWalletRepository
{
    Task<bool> CreateIfCurrencyAvailableAsync(Wallet wallet, CancellationToken cancellationToken);
    Task<Wallet?> FindOwnedAsync(Guid walletId, Guid ownerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Wallet>> ListOwnedAsync(Guid ownerId, CancellationToken cancellationToken);
}
