using FintechPayments.Domain.Transfers;

namespace FintechPayments.Api.Contracts.Transfers;

public sealed record TransferResponse(
    Guid Id,
    Guid SourceWalletId,
    Guid DestinationWalletId,
    decimal Amount,
    string Currency,
    string Reference,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static TransferResponse FromTransfer(Transfer transfer) => new(
        transfer.Id,
        transfer.SourceWalletId,
        transfer.DestinationWalletId,
        transfer.Amount,
        transfer.Currency,
        transfer.Reference,
        transfer.Status.ToString(),
        transfer.CreatedAt,
        transfer.UpdatedAt);
}
