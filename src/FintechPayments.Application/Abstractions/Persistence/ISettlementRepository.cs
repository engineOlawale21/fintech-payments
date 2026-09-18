using FintechPayments.Domain.Settlements;

namespace FintechPayments.Application.Abstractions.Persistence;

public interface ISettlementRepository
{
    Task<SettlementSource?> GetSourceAsync(Guid reconciliationRunId, string currency, CancellationToken cancellationToken);
    Task<SettlementCreateResult> StoreDraftAsync(SettlementBatch batch, CancellationToken cancellationToken);
    Task<SettlementBatch?> FindAsync(Guid batchId, bool includeItems, CancellationToken cancellationToken);
    Task<IReadOnlyList<SettlementBatch>> ListAsync(CancellationToken cancellationToken);
    Task<SettlementFinalizeResult> FinalizeAsync(
        Guid batchId, Guid actorId, DateTimeOffset finalizedAt, CancellationToken cancellationToken);
}

public sealed record SettlementSource(
    DateTimeOffset PeriodStart, DateTimeOffset PeriodEnd,
    IReadOnlyList<SettlementSourceItem> Items, bool HasPreviouslySettledItems);
public sealed record SettlementSourceItem(Guid ReconciliationItemId, decimal Amount);
public sealed record SettlementCreateResult(SettlementBatch? Batch, string? ErrorCode)
{
    public bool Succeeded => Batch is not null;
}
public sealed record SettlementFinalizeResult(SettlementBatch? Batch, string? ErrorCode)
{
    public bool Succeeded => Batch is not null;
}
