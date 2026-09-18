using FintechPayments.Application.Abstractions.Persistence;
using FintechPayments.Domain.Reconciliation;
using FintechPayments.Domain.Transfers;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FintechPayments.Infrastructure.Persistence;

public sealed class ReconciliationRepository(PaymentsDbContext dbContext) : IReconciliationRepository
{
    public async Task<IReadOnlyList<InternalTransferRecord>> ListCompletedTransfersAsync(
        DateTimeOffset periodStart, DateTimeOffset periodEnd, CancellationToken cancellationToken) =>
        await dbContext.Transfers.AsNoTracking()
            .Where(x => x.Status == TransferStatus.Completed &&
                        x.CreatedAt >= periodStart && x.CreatedAt < periodEnd)
            .OrderBy(x => x.Reference)
            .Select(x => new InternalTransferRecord(x.Reference, x.Amount, x.Currency))
            .ToListAsync(cancellationToken);

    public async Task<ReconciliationStoreResult> StoreOnceAsync(
        ReconciliationRun run, CancellationToken cancellationToken)
    {
        dbContext.ReconciliationRuns.Add(run);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return new ReconciliationStoreResult(run, false);
        }
        catch (DbUpdateException exception) when
            (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            dbContext.ChangeTracker.Clear();
            ReconciliationRun existing = await dbContext.ReconciliationRuns.AsNoTracking()
                .SingleAsync(x => x.Provider == run.Provider &&
                                  x.PeriodStart == run.PeriodStart && x.PeriodEnd == run.PeriodEnd &&
                                  x.InputChecksum == run.InputChecksum, cancellationToken);
            return new ReconciliationStoreResult(existing, true);
        }
    }

    public async Task<ReconciliationRun?> FindAsync(
        Guid runId, bool includeItems, CancellationToken cancellationToken)
    {
        IQueryable<ReconciliationRun> query = dbContext.ReconciliationRuns.AsNoTracking();
        if (includeItems) query = query.Include(x => x.Items);
        return await query.SingleOrDefaultAsync(x => x.Id == runId, cancellationToken);
    }

    public Task<ReconciliationItem?> FindItemAsync(
        Guid runId, Guid itemId, CancellationToken cancellationToken) =>
        dbContext.ReconciliationItems.SingleOrDefaultAsync(
            x => x.ReconciliationRunId == runId && x.Id == itemId, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
