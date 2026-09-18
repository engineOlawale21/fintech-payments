using FintechPayments.Domain.Ledger;

namespace FintechPayments.Api.Contracts.Ledger;

public sealed record LedgerPageResponse(
    IReadOnlyList<LedgerEntryResponse> Items,
    string? NextCursor);

public sealed record LedgerEntryResponse(
    Guid Id,
    Guid TransactionId,
    string Direction,
    decimal Amount,
    string Currency,
    DateTimeOffset CreatedAt)
{
    public static LedgerEntryResponse FromEntry(LedgerEntry entry) => new(
        entry.Id,
        entry.LedgerTransactionId,
        entry.Direction.ToString(),
        entry.Amount,
        entry.Currency,
        entry.CreatedAt);
}
