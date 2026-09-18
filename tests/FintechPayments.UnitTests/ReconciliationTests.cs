using FintechPayments.Application.Abstractions.Persistence;
using FintechPayments.Application.Abstractions.Time;
using FintechPayments.Application.Reconciliation;
using FintechPayments.Domain.Common;
using FintechPayments.Domain.Reconciliation;

namespace FintechPayments.UnitTests;

public sealed class ReconciliationTests
{
    [Fact]
    public async Task ComparisonProducesEveryDeterministicCategoryAndTotals()
    {
        FakeRepository repository = new([
            new("matched", 10m, "USD"),
            new("wrong-amount", 20m, "USD"),
            new("internal-only", 30m, "USD"),
        ]);
        ReconciliationService service = new(repository, new FixedClock());

        ReconciliationStoreResult result = await service.CreateAsync(
            "Mock-Provider", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(1),
            [
                new("matched", 10m, "usd"),
                new("wrong-amount", 21m, "USD"),
                new("external-only", 40m, "USD"),
            ], CancellationToken.None);

        Assert.Equal(1, result.Run.MatchedCount);
        Assert.Equal(3, result.Run.DiscrepancyCount);
        Assert.Equal(60m, result.Run.InternalTotal);
        Assert.Equal(71m, result.Run.ExternalTotal);
        Assert.Contains(result.Run.Items, x => x.Category == ReconciliationItemCategory.Matched);
        Assert.Contains(result.Run.Items, x => x.Category == ReconciliationItemCategory.AmountMismatch);
        Assert.Contains(result.Run.Items, x => x.Category == ReconciliationItemCategory.MissingInternal);
        Assert.Contains(result.Run.Items, x => x.Category == ReconciliationItemCategory.MissingExternal);
        Assert.Equal(64, result.Run.InputChecksum.Length);
    }

    [Fact]
    public async Task StatementReferencesMustBeUniqueAfterNormalization()
    {
        ReconciliationService service = new(new FakeRepository([]), new FixedClock());

        await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(
            "mock-provider", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(1),
            [new("same", 1m, "USD"), new(" same ", 1m, "USD")], CancellationToken.None));
    }

    [Fact]
    public void ResolutionIsAppendOnlyAndMatchedItemsCannotBeResolved()
    {
        Guid actor = Guid.NewGuid();
        ReconciliationItem discrepancy = ReconciliationItem.Create(
            "missing", ReconciliationItemCategory.MissingExternal, 12m, null, "USD");
        discrepancy.Resolve(actor, "Confirmed provider delay", DateTimeOffset.UnixEpoch);

        Assert.Equal(actor, discrepancy.ResolvedBy);
        Assert.Throws<DomainException>(() =>
            discrepancy.Resolve(actor, "overwrite", DateTimeOffset.UnixEpoch.AddMinutes(1)));
        Assert.Throws<DomainException>(() => ReconciliationItem.Create(
            "matched", ReconciliationItemCategory.Matched, 1m, 1m, "USD")
            .Resolve(actor, "not needed", DateTimeOffset.UnixEpoch));
    }

    private sealed class FakeRepository(IReadOnlyList<InternalTransferRecord> transfers)
        : IReconciliationRepository
    {
        public Task<IReadOnlyList<InternalTransferRecord>> ListCompletedTransfersAsync(
            DateTimeOffset periodStart, DateTimeOffset periodEnd, CancellationToken cancellationToken) =>
            Task.FromResult(transfers);

        public Task<ReconciliationStoreResult> StoreOnceAsync(
            ReconciliationRun run, CancellationToken cancellationToken) =>
            Task.FromResult(new ReconciliationStoreResult(run, false));

        public Task<ReconciliationRun?> FindAsync(
            Guid runId, bool includeItems, CancellationToken cancellationToken) => Task.FromResult<ReconciliationRun?>(null);

        public Task<ReconciliationItem?> FindItemAsync(
            Guid runId, Guid itemId, CancellationToken cancellationToken) =>
            Task.FromResult<ReconciliationItem?>(null);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UnixEpoch.AddDays(2);
    }
}
