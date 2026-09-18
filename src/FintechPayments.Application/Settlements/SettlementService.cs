using FintechPayments.Application.Abstractions.Persistence;
using FintechPayments.Application.Abstractions.Time;
using FintechPayments.Domain.Common;
using FintechPayments.Domain.Settlements;

namespace FintechPayments.Application.Settlements;

public sealed class SettlementService(ISettlementRepository repository, IClock clock)
{
    private const decimal PercentageFee = 0.015m;
    private const decimal PerTransactionFee = 0.10m;

    public async Task<SettlementCreateResult> CreateDraftAsync(
        Guid reconciliationRunId, string currencyCode, decimal adjustmentAmount,
        string? adjustmentReason, CancellationToken cancellationToken)
    {
        Currency currency = Currency.FromCode(currencyCode);
        SettlementSource? source = await repository.GetSourceAsync(
            reconciliationRunId, currency.Code, cancellationToken);
        if (source is null)
        {
            return new SettlementCreateResult(null, "reconciliation_not_found");
        }
        if (source.HasPreviouslySettledItems || source.Items.Count == 0)
        {
            return new SettlementCreateResult(null, "settlement_inputs_unavailable");
        }

        decimal gross = source.Items.Sum(x => x.Amount);
        decimal fee = decimal.Round(
            gross * PercentageFee + source.Items.Count * PerTransactionFee,
            currency.MinorUnitDigits, MidpointRounding.AwayFromZero);
        SettlementBatch batch = SettlementBatch.CreateDraft(
            reconciliationRunId, source.PeriodStart, source.PeriodEnd, currency.Code,
            gross, fee, adjustmentAmount, adjustmentReason, clock.UtcNow,
            source.Items.Select(x => x.ReconciliationItemId));
        return await repository.StoreDraftAsync(batch, cancellationToken);
    }

    public Task<SettlementBatch?> GetAsync(Guid id, bool includeItems, CancellationToken cancellationToken) =>
        repository.FindAsync(id, includeItems, cancellationToken);

    public Task<IReadOnlyList<SettlementBatch>> ListAsync(CancellationToken cancellationToken) =>
        repository.ListAsync(cancellationToken);

    public Task<SettlementFinalizeResult> FinalizeAsync(
        Guid id, Guid actorId, CancellationToken cancellationToken) =>
        repository.FinalizeAsync(id, actorId, clock.UtcNow, cancellationToken);
}
