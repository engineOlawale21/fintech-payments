namespace FintechPayments.Application.Abstractions.Persistence;

public interface IWalletQuery
{
    Task<WalletReadModel?> FindOwnedAsync(Guid walletId, Guid ownerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<WalletReadModel>> ListOwnedAsync(Guid ownerId, CancellationToken cancellationToken);
}

public sealed record WalletReadModel(
    Guid Id, string Currency, decimal AvailableBalance,
    string Status, DateTimeOffset CreatedAt);
