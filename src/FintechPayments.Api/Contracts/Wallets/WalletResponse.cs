using FintechPayments.Domain.Wallets;
using FintechPayments.Application.Abstractions.Persistence;

namespace FintechPayments.Api.Contracts.Wallets;

public sealed record WalletResponse(
    Guid Id,
    string Currency,
    decimal AvailableBalance,
    string Status,
    DateTimeOffset CreatedAt)
{
    public static WalletResponse FromWallet(Wallet wallet) => new(
        wallet.Id,
        wallet.Currency,
        wallet.AvailableBalance,
        wallet.Status.ToString(),
        wallet.CreatedAt);

    public static WalletResponse FromReadModel(WalletReadModel wallet) => new(
        wallet.Id, wallet.Currency, wallet.AvailableBalance, wallet.Status, wallet.CreatedAt);
}
