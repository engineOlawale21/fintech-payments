using FintechPayments.Domain.Common;

namespace FintechPayments.Domain.Settlements;

public sealed class SettlementBatch
{
    private readonly List<SettlementItem> items = [];
    private SettlementBatch() { }

    private SettlementBatch(
        Guid id, Guid reconciliationRunId, DateTimeOffset periodStart, DateTimeOffset periodEnd,
        string currency, decimal grossAmount, decimal feeAmount, decimal adjustmentAmount,
        string? adjustmentReason, DateTimeOffset createdAt, IEnumerable<Guid> inputItemIds)
    {
        Id = id;
        ReconciliationRunId = reconciliationRunId;
        PeriodStart = periodStart.ToUniversalTime();
        PeriodEnd = periodEnd.ToUniversalTime();
        Currency = currency;
        GrossAmount = grossAmount;
        FeeAmount = feeAmount;
        AdjustmentAmount = adjustmentAmount;
        AdjustmentReason = adjustmentReason;
        NetAmount = grossAmount - feeAmount + adjustmentAmount;
        Status = SettlementStatus.Draft;
        CreatedAt = createdAt.ToUniversalTime();
        items.AddRange(inputItemIds.Select(x => SettlementItem.Create(id, x)));
    }

    public Guid Id { get; private set; }
    public Guid ReconciliationRunId { get; private set; }
    public DateTimeOffset PeriodStart { get; private set; }
    public DateTimeOffset PeriodEnd { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public decimal GrossAmount { get; private set; }
    public decimal FeeAmount { get; private set; }
    public decimal AdjustmentAmount { get; private set; }
    public string? AdjustmentReason { get; private set; }
    public decimal NetAmount { get; private set; }
    public SettlementStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? FinalizedAt { get; private set; }
    public Guid? FinalizedBy { get; private set; }
    public IReadOnlyCollection<SettlementItem> Items => items.AsReadOnly();

    public static SettlementBatch CreateDraft(
        Guid reconciliationRunId, DateTimeOffset periodStart, DateTimeOffset periodEnd,
        string currency, decimal grossAmount, decimal feeAmount, decimal adjustmentAmount,
        string? adjustmentReason, DateTimeOffset createdAt, IEnumerable<Guid> inputItemIds)
    {
        Guid[] ids = inputItemIds.Distinct().ToArray();
        if (reconciliationRunId == Guid.Empty || periodStart >= periodEnd ||
            string.IsNullOrWhiteSpace(currency) || currency.Length != 3 || grossAmount <= 0 ||
            feeAmount < 0 || grossAmount - feeAmount + adjustmentAmount < 0 || ids.Length == 0 ||
            (adjustmentAmount != 0 && string.IsNullOrWhiteSpace(adjustmentReason)) ||
            adjustmentReason?.Length > 500)
        {
            throw new DomainException("Settlement batch is invalid.");
        }

        return new SettlementBatch(
            Guid.NewGuid(), reconciliationRunId, periodStart, periodEnd,
            currency.Trim().ToUpperInvariant(), grossAmount, feeAmount, adjustmentAmount,
            adjustmentReason?.Trim(), createdAt, ids);
    }

    public void Finalize(Guid actorId, DateTimeOffset finalizedAt)
    {
        if (Status != SettlementStatus.Draft || actorId == Guid.Empty)
        {
            throw new DomainException("Only a draft settlement can be finalized by an operator.");
        }
        Status = SettlementStatus.Finalized;
        FinalizedBy = actorId;
        FinalizedAt = finalizedAt.ToUniversalTime();
    }
}
