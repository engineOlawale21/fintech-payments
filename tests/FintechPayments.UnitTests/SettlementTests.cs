using FintechPayments.Application.Abstractions.Persistence;
using FintechPayments.Application.Abstractions.Time;
using FintechPayments.Application.Settlements;
using FintechPayments.Domain.Common;
using FintechPayments.Domain.Settlements;

namespace FintechPayments.UnitTests;

public sealed class SettlementTests
{
    [Fact]
    public async Task DraftUsesDeterministicFeeFormulaAndExactInputs()
    {
        FakeRepository repository = new(new SettlementSource(
            DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(1),
            [new(Guid.NewGuid(), 100m), new(Guid.NewGuid(), 50m)], false));
        SettlementService service = new(repository, new FixedClock());

        SettlementCreateResult result = await service.CreateDraftAsync(
            Guid.NewGuid(), "usd", 1m, "manual credit", CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(150m, result.Batch!.GrossAmount);
        Assert.Equal(2.45m, result.Batch.FeeAmount);
        Assert.Equal(148.55m, result.Batch.NetAmount);
        Assert.Equal(2, result.Batch.Items.Count);
        Assert.Equal(SettlementStatus.Draft, result.Batch.Status);
    }

    [Fact]
    public async Task PreviouslySettledInputsCannotCreateAnotherBatch()
    {
        FakeRepository repository = new(new SettlementSource(
            DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(1),
            [new(Guid.NewGuid(), 10m)], true));

        SettlementCreateResult result = await new SettlementService(repository, new FixedClock())
            .CreateDraftAsync(Guid.NewGuid(), "USD", 0, null, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("settlement_inputs_unavailable", result.ErrorCode);
        Assert.Null(repository.StoredBatch);
    }

    [Fact]
    public void FinalizedBatchIsImmutable()
    {
        SettlementBatch batch = SettlementBatch.CreateDraft(
            Guid.NewGuid(), DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(1),
            "USD", 10m, 0.25m, 0, null, DateTimeOffset.UnixEpoch, [Guid.NewGuid()]);
        batch.Finalize(Guid.NewGuid(), DateTimeOffset.UnixEpoch.AddDays(2));

        Assert.Equal(SettlementStatus.Finalized, batch.Status);
        Assert.Throws<DomainException>(() =>
            batch.Finalize(Guid.NewGuid(), DateTimeOffset.UnixEpoch.AddDays(3)));
    }

    private sealed class FakeRepository(SettlementSource? source) : ISettlementRepository
    {
        public SettlementBatch? StoredBatch { get; private set; }
        public Task<SettlementSource?> GetSourceAsync(Guid reconciliationRunId, string currency, CancellationToken cancellationToken) =>
            Task.FromResult(source);
        public Task<SettlementCreateResult> StoreDraftAsync(SettlementBatch batch, CancellationToken cancellationToken)
        {
            StoredBatch = batch;
            return Task.FromResult(new SettlementCreateResult(batch, null));
        }
        public Task<SettlementBatch?> FindAsync(Guid batchId, bool includeItems, CancellationToken cancellationToken) =>
            Task.FromResult<SettlementBatch?>(null);
        public Task<IReadOnlyList<SettlementBatch>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SettlementBatch>>([]);
        public Task<SettlementFinalizeResult> FinalizeAsync(Guid batchId, Guid actorId, DateTimeOffset finalizedAt, CancellationToken cancellationToken) =>
            Task.FromResult(new SettlementFinalizeResult(null, "settlement_not_found"));
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UnixEpoch;
    }
}
