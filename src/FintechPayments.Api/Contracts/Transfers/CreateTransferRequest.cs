using System.ComponentModel.DataAnnotations;

namespace FintechPayments.Api.Contracts.Transfers;

public sealed record CreateTransferRequest(
    Guid SourceWalletId,
    Guid DestinationWalletId,
    [Range(typeof(decimal), "0.01", "999999999999999.9999")] decimal Amount,
    [Required, StringLength(3, MinimumLength = 3)] string Currency,
    [Required, MaxLength(100)] string Reference);
