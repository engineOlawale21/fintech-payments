using FintechPayments.Domain.Common;
using FintechPayments.Domain.Transfers;

namespace FintechPayments.Application.Abstractions.Persistence;

public interface ITransferExecutor
{
    Task<TransferExecutionResult> ExecuteAsync(
        Guid actorId,
        Guid sourceWalletId,
        Guid destinationWalletId,
        Money money,
        string reference,
        string idempotencyKey,
        string requestHash,
        CancellationToken cancellationToken);
}

public sealed record TransferExecutionResult(
    bool Succeeded,
    Transfer? Transfer,
    string? ErrorCode,
    bool IsReplay)
{
    public static TransferExecutionResult Success(Transfer transfer, bool isReplay = false) =>
        new(true, transfer, null, isReplay);

    public static TransferExecutionResult Failure(string errorCode) =>
        new(false, null, errorCode, false);
}
