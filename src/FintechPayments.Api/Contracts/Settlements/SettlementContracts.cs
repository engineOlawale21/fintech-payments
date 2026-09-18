using FintechPayments.Domain.Settlements;

namespace FintechPayments.Api.Contracts.Settlements;

public sealed record CreateSettlementRequest(
    Guid ReconciliationRunId, string Currency,
    decimal AdjustmentAmount = 0, string? AdjustmentReason = null);

public sealed record SettlementResponse(
    Guid Id, Guid ReconciliationRunId, DateTimeOffset PeriodStart, DateTimeOffset PeriodEnd,
    string Currency, decimal GrossAmount, decimal FeeAmount, decimal AdjustmentAmount,
    string? AdjustmentReason, decimal NetAmount, string Status, int InputCount,
    DateTimeOffset CreatedAt, DateTimeOffset? FinalizedAt, Guid? FinalizedBy)
{
    public static SettlementResponse FromBatch(SettlementBatch batch) => new(
        batch.Id, batch.ReconciliationRunId, batch.PeriodStart, batch.PeriodEnd,
        batch.Currency, batch.GrossAmount, batch.FeeAmount, batch.AdjustmentAmount,
        batch.AdjustmentReason, batch.NetAmount, batch.Status.ToString(), batch.Items.Count,
        batch.CreatedAt, batch.FinalizedAt, batch.FinalizedBy);
}
