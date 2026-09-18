using FintechPayments.Domain.Reconciliation;

namespace FintechPayments.Application.Abstractions.Persistence;

public interface IReconciliationRepository
{
    Task<IReadOnlyList<InternalTransferRecord>> ListCompletedTransfersAsync(
        DateTimeOffset periodStart, DateTimeOffset periodEnd, CancellationToken cancellationToken);
    Task<ReconciliationStoreResult> StoreOnceAsync(
        ReconciliationRun run, CancellationToken cancellationToken);
    Task<ReconciliationRun?> FindAsync(Guid runId, bool includeItems, CancellationToken cancellationToken);
    Task<ReconciliationItem?> FindItemAsync(Guid runId, Guid itemId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record InternalTransferRecord(string Reference, decimal Amount, string Currency);
public sealed record ReconciliationStoreResult(ReconciliationRun Run, bool IsReplay);
