using FintechPayments.Domain.Reconciliation;

namespace FintechPayments.Api.Contracts.Reconciliation;

public sealed record ReconciliationResponse(
    Guid Id, string Provider, DateTimeOffset PeriodStart, DateTimeOffset PeriodEnd,
    string InputChecksum, int MatchedCount, int DiscrepancyCount,
    decimal InternalTotal, decimal ExternalTotal, DateTimeOffset CreatedAt)
{
    public static ReconciliationResponse FromRun(ReconciliationRun run) => new(
        run.Id, run.Provider, run.PeriodStart, run.PeriodEnd, run.InputChecksum,
        run.MatchedCount, run.DiscrepancyCount, run.InternalTotal, run.ExternalTotal, run.CreatedAt);
}

public sealed record ReconciliationItemResponse(
    Guid Id, string Reference, string Category, decimal? InternalAmount,
    decimal? ExternalAmount, string Currency, string? ResolutionNote,
    Guid? ResolvedBy, DateTimeOffset? ResolvedAt)
{
    public static ReconciliationItemResponse FromItem(ReconciliationItem item) => new(
        item.Id, item.Reference, item.Category.ToString(), item.InternalAmount,
        item.ExternalAmount, item.Currency, item.ResolutionNote, item.ResolvedBy, item.ResolvedAt);
}
