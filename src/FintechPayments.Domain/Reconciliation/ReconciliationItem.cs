using FintechPayments.Domain.Common;

namespace FintechPayments.Domain.Reconciliation;

public sealed class ReconciliationItem
{
    private ReconciliationItem() { }

    private ReconciliationItem(
        Guid id, string reference, ReconciliationItemCategory category,
        decimal? internalAmount, decimal? externalAmount, string currency)
    {
        Id = id;
        Reference = reference;
        Category = category;
        InternalAmount = internalAmount;
        ExternalAmount = externalAmount;
        Currency = currency;
    }

    public Guid Id { get; private set; }
    public Guid ReconciliationRunId { get; private set; }
    public string Reference { get; private set; } = string.Empty;
    public ReconciliationItemCategory Category { get; private set; }
    public decimal? InternalAmount { get; private set; }
    public decimal? ExternalAmount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public string? ResolutionNote { get; private set; }
    public Guid? ResolvedBy { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }

    public static ReconciliationItem Create(
        string reference, ReconciliationItemCategory category,
        decimal? internalAmount, decimal? externalAmount, string currency)
    {
        if (string.IsNullOrWhiteSpace(reference) || reference.Length > 100 ||
            string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
        {
            throw new DomainException("Reconciliation item is invalid.");
        }

        return new ReconciliationItem(
            Guid.NewGuid(), reference.Trim(), category, internalAmount, externalAmount,
            currency.Trim().ToUpperInvariant());
    }

    public void Resolve(Guid actorId, string note, DateTimeOffset resolvedAt)
    {
        if (Category == ReconciliationItemCategory.Matched)
        {
            throw new DomainException("Matched items do not require resolution.");
        }
        if (actorId == Guid.Empty || string.IsNullOrWhiteSpace(note) || note.Length > 500)
        {
            throw new DomainException("A resolver and a note of at most 500 characters are required.");
        }
        if (ResolvedAt.HasValue)
        {
            throw new DomainException("The reconciliation item is already resolved.");
        }

        ResolvedBy = actorId;
        ResolutionNote = note.Trim();
        ResolvedAt = resolvedAt.ToUniversalTime();
    }
}
