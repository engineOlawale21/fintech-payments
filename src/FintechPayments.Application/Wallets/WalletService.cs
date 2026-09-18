using FintechPayments.Application.Abstractions.Persistence;
using FintechPayments.Application.Abstractions.Time;
using FintechPayments.Domain.Common;
using FintechPayments.Domain.Wallets;

namespace FintechPayments.Application.Wallets;

public sealed class WalletService(IWalletRepository wallets, IClock clock)
{
    public async Task<CreateWalletResult> CreateAsync(
        Guid ownerId,
        string currencyCode,
        CancellationToken cancellationToken)
    {
        Currency currency = Currency.FromCode(currencyCode);
        Wallet wallet = Wallet.Create(ownerId, currency, clock.UtcNow);

        bool created = await wallets.CreateIfCurrencyAvailableAsync(wallet, cancellationToken);
        return created
            ? CreateWalletResult.Success(wallet)
            : CreateWalletResult.Failure("wallet_currency_already_exists");
    }

    public Task<Wallet?> GetOwnedAsync(
        Guid walletId,
        Guid ownerId,
        CancellationToken cancellationToken) =>
        wallets.FindOwnedAsync(walletId, ownerId, cancellationToken);

    public Task<IReadOnlyList<Wallet>> ListOwnedAsync(
        Guid ownerId,
        CancellationToken cancellationToken) =>
        wallets.ListOwnedAsync(ownerId, cancellationToken);
}

public sealed record CreateWalletResult(bool Succeeded, Wallet? Wallet, string? ErrorCode)
{
    public static CreateWalletResult Success(Wallet wallet) => new(true, wallet, null);
    public static CreateWalletResult Failure(string errorCode) => new(false, null, errorCode);
}
