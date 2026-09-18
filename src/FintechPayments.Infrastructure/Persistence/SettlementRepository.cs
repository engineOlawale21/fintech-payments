using System.Data;
using FintechPayments.Application.Abstractions.Persistence;
using FintechPayments.Domain.Reconciliation;
using FintechPayments.Domain.Settlements;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FintechPayments.Infrastructure.Persistence;

public sealed class SettlementRepository(PaymentsDbContext dbContext) : ISettlementRepository
{
    public async Task<SettlementSource?> GetSourceAsync(
        Guid reconciliationRunId, string currency, CancellationToken cancellationToken)
    {
        var run = await dbContext.ReconciliationRuns.AsNoTracking()
            .Where(x => x.Id == reconciliationRunId)
            .Select(x => new { x.PeriodStart, x.PeriodEnd })
            .SingleOrDefaultAsync(cancellationToken);
        if (run is null) return null;

        List<SettlementSourceItem> items = await dbContext.ReconciliationItems.AsNoTracking()
            .Where(x => x.ReconciliationRunId == reconciliationRunId &&
                        x.Category == ReconciliationItemCategory.Matched &&
                        x.Currency == currency && x.InternalAmount.HasValue)
            .OrderBy(x => x.Reference)
            .Select(x => new SettlementSourceItem(x.Id, x.InternalAmount!.Value))
            .ToListAsync(cancellationToken);
        Guid[] itemIds = items.Select(x => x.ReconciliationItemId).ToArray();
        bool previouslySettled = itemIds.Length != 0 && await dbContext.SettlementItems.AsNoTracking()
            .AnyAsync(x => itemIds.Contains(x.ReconciliationItemId), cancellationToken);
        return new SettlementSource(run.PeriodStart, run.PeriodEnd, items, previouslySettled);
    }

    public async Task<SettlementCreateResult> StoreDraftAsync(
        SettlementBatch batch, CancellationToken cancellationToken)
    {
        dbContext.SettlementBatches.Add(batch);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return new SettlementCreateResult(batch, null);
        }
        catch (DbUpdateException exception) when
            (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            dbContext.ChangeTracker.Clear();
            return new SettlementCreateResult(null, "settlement_inputs_unavailable");
        }
    }

    public async Task<SettlementBatch?> FindAsync(
        Guid batchId, bool includeItems, CancellationToken cancellationToken)
    {
        IQueryable<SettlementBatch> query = dbContext.SettlementBatches.AsNoTracking();
        if (includeItems) query = query.Include(x => x.Items);
        return await query.SingleOrDefaultAsync(x => x.Id == batchId, cancellationToken);
    }

    public async Task<IReadOnlyList<SettlementBatch>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.SettlementBatches.AsNoTracking().Include(x => x.Items)
            .OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

    public async Task<SettlementFinalizeResult> FinalizeAsync(
        Guid batchId, Guid actorId, DateTimeOffset finalizedAt, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        SettlementBatch? batch = await dbContext.SettlementBatches
            .SingleOrDefaultAsync(x => x.Id == batchId, cancellationToken);
        if (batch is null) return new SettlementFinalizeResult(null, "settlement_not_found");
        if (batch.Status != SettlementStatus.Draft)
            return new SettlementFinalizeResult(null, "settlement_already_finalized");

        bool overlaps = await dbContext.SettlementBatches.AsNoTracking().AnyAsync(x =>
            x.Id != batch.Id && x.Status == SettlementStatus.Finalized && x.Currency == batch.Currency &&
            x.PeriodStart < batch.PeriodEnd && x.PeriodEnd > batch.PeriodStart, cancellationToken);
        if (overlaps) return new SettlementFinalizeResult(null, "settlement_period_overlap");

        batch.Finalize(actorId, finalizedAt);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new SettlementFinalizeResult(batch, null);
        }
        catch (Exception exception) when
            (exception is DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.SerializationFailure } } ||
             exception is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure })
        {
            return new SettlementFinalizeResult(null, "settlement_period_overlap");
        }
    }
}
