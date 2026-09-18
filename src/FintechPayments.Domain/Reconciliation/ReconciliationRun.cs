using FintechPayments.Domain.Common;

namespace FintechPayments.Domain.Reconciliation;

public sealed class ReconciliationRun
{
    private readonly List<ReconciliationItem> items = [];

    private ReconciliationRun() { }

    private ReconciliationRun(
        Guid id, string provider, DateTimeOffset periodStart, DateTimeOffset periodEnd,
        string inputChecksum, DateTimeOffset createdAt, IEnumerable<ReconciliationItem> runItems)
    {
        Id = id;
        Provider = provider;
        PeriodStart = periodStart.ToUniversalTime();
        PeriodEnd = periodEnd.ToUniversalTime();
        InputChecksum = inputChecksum;
        CreatedAt = createdAt.ToUniversalTime();
        items.AddRange(runItems);
        MatchedCount = items.Count(x => x.Category == ReconciliationItemCategory.Matched);
        DiscrepancyCount = items.Count - MatchedCount;
        ExternalTotal = items.Where(x => x.ExternalAmount.HasValue).Sum(x => x.ExternalAmount!.Value);
        InternalTotal = items.Where(x => x.InternalAmount.HasValue).Sum(x => x.InternalAmount!.Value);
    }

    public Guid Id { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public DateTimeOffset PeriodStart { get; private set; }
    public DateTimeOffset PeriodEnd { get; private set; }
    public string InputChecksum { get; private set; } = string.Empty;
    public int MatchedCount { get; private set; }
    public int DiscrepancyCount { get; private set; }
    public decimal InternalTotal { get; private set; }
    public decimal ExternalTotal { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyCollection<ReconciliationItem> Items => items.AsReadOnly();

    public static ReconciliationRun Create(
        string provider, DateTimeOffset periodStart, DateTimeOffset periodEnd,
        string inputChecksum, DateTimeOffset createdAt, IEnumerable<ReconciliationItem> items)
    {
        if (string.IsNullOrWhiteSpace(provider) || provider.Length > 64 ||
            periodStart >= periodEnd || inputChecksum.Length != 64)
        {
            throw new DomainException("Reconciliation run is invalid.");
        }

        return new ReconciliationRun(
            Guid.NewGuid(), provider.Trim().ToLowerInvariant(), periodStart, periodEnd,
            inputChecksum, createdAt, items);
    }
}
