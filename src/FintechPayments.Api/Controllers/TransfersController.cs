using FintechPayments.Api.Authentication;
using FintechPayments.Api.Contracts.Transfers;
using FintechPayments.Application.Abstractions.Persistence;
using FintechPayments.Application.Transfers;
using FintechPayments.Domain.Common;
using FintechPayments.Domain.Transfers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using FintechPayments.Application.Auditing;

namespace FintechPayments.Api.Controllers;

[ApiController]
[Authorize(Roles = "Customer")]
[EnableRateLimiting("transfers")]
[Route("api/v1/transfers")]
public sealed class TransfersController(TransferService transfers, AuditService audit) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<TransferResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        CreateTransferRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out Guid actorId))
        {
            return Unauthorized();
        }

        try
        {
            TransferExecutionResult result = await transfers.CreateAsync(
                actorId,
                request.SourceWalletId,
                request.DestinationWalletId,
                request.Amount,
                request.Currency,
                request.Reference,
                idempotencyKey ?? string.Empty,
                cancellationToken);

            if (!result.Succeeded)
            {
                await audit.RecordAsync(actorId, "transfer.create", "Transfer", null,
                    "rejected", HttpContext.TraceIdentifier, result.ErrorCode, CancellationToken.None);
                return ToProblem(result.ErrorCode!);
            }

            TransferResponse response = TransferResponse.FromTransfer(result.Transfer!);
            if (result.IsReplay)
            {
                Response.Headers["Idempotency-Replayed"] = "true";
            }
            await audit.RecordAsync(actorId, "transfer.create", "Transfer", response.Id,
                result.IsReplay ? "replayed" : "succeeded",
                HttpContext.TraceIdentifier, null, CancellationToken.None);
            return CreatedAtAction(nameof(GetById), new { transferId = response.Id }, response);
        }
        catch (DomainException exception)
        {
            await audit.RecordAsync(actorId, "transfer.create", "Transfer", null,
                "rejected", HttpContext.TraceIdentifier, "validation_failed", CancellationToken.None);
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Transfer request is invalid.",
                detail: exception.Message,
                extensions: new Dictionary<string, object?> { ["code"] = "validation_failed" });
        }
    }

    [HttpGet("{transferId:guid}")]
    [ProducesResponseType<TransferResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid transferId, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out Guid actorId))
        {
            return Unauthorized();
        }

        Transfer? transfer = await transfers.GetAccessibleAsync(transferId, actorId, cancellationToken);
        return transfer is null ? NotFound() : Ok(TransferResponse.FromTransfer(transfer));
    }

    private ObjectResult ToProblem(string errorCode)
    {
        (int status, string title) = errorCode switch
        {
            "wallet_not_found" => (StatusCodes.Status404NotFound, "A wallet was not found."),
            "source_wallet_not_owned" => (StatusCodes.Status403Forbidden, "The source wallet is not available to this user."),
            "insufficient_funds" => (StatusCodes.Status422UnprocessableEntity, "The source wallet has insufficient funds."),
            "currency_mismatch" => (StatusCodes.Status422UnprocessableEntity, "Wallet currencies do not match the transfer."),
            "wallet_unavailable" => (StatusCodes.Status422UnprocessableEntity, "A wallet is unavailable."),
            "transfer_reference_conflict" => (StatusCodes.Status409Conflict, "Transfer reference already exists."),
            "idempotency_key_conflict" => (StatusCodes.Status409Conflict, "Idempotency key was used for another request."),
            "request_in_progress" => (StatusCodes.Status409Conflict, "A request with this idempotency key is in progress."),
            _ => (StatusCodes.Status422UnprocessableEntity, "Transfer could not be completed."),
        };

        return Problem(
            statusCode: status,
            title: title,
            extensions: new Dictionary<string, object?> { ["code"] = errorCode });
    }
}
