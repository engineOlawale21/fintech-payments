using FintechPayments.Application.Abstractions.Persistence;
using FintechPayments.Application.Abstractions.Time;
using FintechPayments.Application.Wallets;
using FintechPayments.Domain.Common;
using FintechPayments.Domain.Wallets;

namespace FintechPayments.UnitTests;

public sealed class WalletServiceTests
{
    [Fact]
    public async Task CreateBuildsOwnedWallet()
    {
        FakeWalletRepository repository = new();
        WalletService service = new(repository, new FixedClock());
        Guid ownerId = Guid.NewGuid();

        CreateWalletResult result = await service.CreateAsync(ownerId, "ngn", CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(ownerId, result.Wallet?.OwnerId);
        Assert.Equal("NGN", result.Wallet?.Currency);
        Assert.Equal(0m, result.Wallet?.AvailableBalance);
    }

    [Fact]
    public async Task CreateReturnsConflictWhenCurrencyExists()
    {
        FakeWalletRepository repository = new() { AllowCreate = false };
        WalletService service = new(repository, new FixedClock());

        CreateWalletResult result = await service.CreateAsync(
            Guid.NewGuid(), "USD", CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("wallet_currency_already_exists", result.ErrorCode);
    }

    [Fact]
    public async Task CreateRejectsUnsupportedCurrency()
    {
        WalletService service = new(new FakeWalletRepository(), new FixedClock());

        await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(
            Guid.NewGuid(), "EUR", CancellationToken.None));
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 9, 12, 1, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeWalletRepository : IWalletRepository
    {
        public bool AllowCreate { get; init; } = true;

        public Task<bool> CreateIfCurrencyAvailableAsync(Wallet wallet, CancellationToken cancellationToken) =>
            Task.FromResult(AllowCreate);

        public Task<Wallet?> FindOwnedAsync(Guid walletId, Guid ownerId, CancellationToken cancellationToken) =>
            Task.FromResult<Wallet?>(null);

        public Task<IReadOnlyList<Wallet>> ListOwnedAsync(Guid ownerId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Wallet>>([]);
    }
}
