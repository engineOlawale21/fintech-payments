namespace FintechPayments.Api.Contracts.Reconciliation;

public sealed record CreateReconciliationRequest(
    string Provider,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    IReadOnlyCollection<ProviderRecordRequest> Records);

public sealed record ProviderRecordRequest(string Reference, decimal Amount, string Currency);

public sealed record ResolveReconciliationItemRequest(string Note);
