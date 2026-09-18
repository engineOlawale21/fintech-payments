using FintechPayments.Application.Abstractions.Persistence;
using FintechPayments.Application.Ledger;
using FintechPayments.Domain.Common;
using FintechPayments.Domain.Ledger;
using FintechPayments.Domain.Wallets;

namespace FintechPayments.UnitTests;

public sealed class LedgerServiceTests
{
    [Fact]
    public async Task GetWalletLedgerHidesWalletNotOwnedByCaller()
    {
        LedgerService service = new(new FakeWalletRepository(null), new FakeLedgerRepository([]));

        WalletLedgerResult result = await service.GetWalletLedgerAsync(
            Guid.NewGuid(), Guid.NewGuid(), null, null, CancellationToken.None);

        Assert.False(result.Exists);
        Assert.Empty(result.Entries);
    }

    [Fact]
    public async Task GetWalletLedgerReturnsStableNextCursor()
    {
        Guid ownerId = Guid.NewGuid();
        Wallet wallet = Wallet.Create(ownerId, Currency.FromCode("USD"), DateTimeOffset.UtcNow);
        LedgerTransaction transaction = LedgerTransaction.CreateTransfer(
            Guid.NewGuid(),
            wallet.Id,
            Guid.NewGuid(),
            Money.Create(10m, Currency.FromCode("USD")),
            "cursor-test",
            DateTimeOffset.UtcNow);
        LedgerService service = new(
            new FakeWalletRepository(wallet),
            new FakeLedgerRepository(transaction.Entries.ToArray()));

        WalletLedgerResult result = await service.GetWalletLedgerAsync(
            wallet.Id, ownerId, null, 1, CancellationToken.None);

        Assert.True(result.Exists);
        Assert.Single(result.Entries);
        Assert.NotNull(result.NextCursor);
    }

    [Fact]
    public async Task GetWalletLedgerRejectsInvalidCursor()
    {
        Guid ownerId = Guid.NewGuid();
        Wallet wallet = Wallet.Create(ownerId, Currency.FromCode("USD"), DateTimeOffset.UtcNow);
        LedgerService service = new(new FakeWalletRepository(wallet), new FakeLedgerRepository([]));

        await Assert.ThrowsAsync<DomainException>(() => service.GetWalletLedgerAsync(
            wallet.Id, ownerId, "not-a-valid-cursor", null, CancellationToken.None));
    }

    private sealed class FakeWalletRepository(Wallet? wallet) : IWalletRepository
    {
        public Task<bool> CreateIfCurrencyAvailableAsync(Wallet value, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<Wallet?> FindOwnedAsync(Guid walletId, Guid ownerId, CancellationToken cancellationToken) =>
            Task.FromResult(wallet is not null && wallet.Id == walletId && wallet.OwnerId == ownerId ? wallet : null);

        public Task<IReadOnlyList<Wallet>> ListOwnedAsync(Guid ownerId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Wallet>>(wallet is null ? [] : [wallet]);
    }

    private sealed class FakeLedgerRepository(IReadOnlyList<LedgerEntry> entries) : ILedgerRepository
    {
        public Task<IReadOnlyList<LedgerEntry>> ListWalletEntriesAsync(
            Guid walletId,
            LedgerPosition? before,
            int take,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<LedgerEntry>>(entries.Take(take).ToArray());
    }
}
