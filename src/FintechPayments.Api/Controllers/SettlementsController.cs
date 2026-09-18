using FintechPayments.Api.Authentication;
using FintechPayments.Api.Contracts.Settlements;
using FintechPayments.Application.Abstractions.Persistence;
using FintechPayments.Application.Settlements;
using FintechPayments.Domain.Common;
using FintechPayments.Domain.Settlements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using FintechPayments.Application.Auditing;

namespace FintechPayments.Api.Controllers;

[ApiController]
[Authorize(Policy = "OperationsOnly")]
[EnableRateLimiting("operations")]
[Route("api/v1/settlements")]
public sealed class SettlementsController(SettlementService settlements, AuditService audit) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        CreateSettlementRequest request, CancellationToken cancellationToken)
    {
        try
        {
            SettlementCreateResult result = await settlements.CreateDraftAsync(
                request.ReconciliationRunId, request.Currency, request.AdjustmentAmount,
                request.AdjustmentReason, cancellationToken);
            if (!result.Succeeded) return ToProblem(result.ErrorCode!);
            SettlementResponse response = SettlementResponse.FromBatch(result.Batch!);
            User.TryGetUserId(out Guid actorId);
            await audit.RecordAsync(actorId, "settlement.create", "SettlementBatch", response.Id,
                "succeeded", HttpContext.TraceIdentifier, null, CancellationToken.None);
            return CreatedAtAction(nameof(Get), new { settlementId = response.Id }, response);
        }
        catch (DomainException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Settlement request is invalid.", detail: exception.Message,
                extensions: new Dictionary<string, object?> { ["code"] = "validation_failed" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok((await settlements.ListAsync(cancellationToken)).Select(SettlementResponse.FromBatch));

    [HttpGet("{settlementId:guid}")]
    public async Task<IActionResult> Get(Guid settlementId, CancellationToken cancellationToken)
    {
        SettlementBatch? batch = await settlements.GetAsync(settlementId, true, cancellationToken);
        return batch is null ? NotFound() : Ok(SettlementResponse.FromBatch(batch));
    }

    [HttpPost("{settlementId:guid}/finalize")]
    public async Task<IActionResult> Finalize(Guid settlementId, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out Guid actorId)) return Unauthorized();
        try
        {
            SettlementFinalizeResult result = await settlements.FinalizeAsync(
                settlementId, actorId, cancellationToken);
            if (!result.Succeeded) return ToProblem(result.ErrorCode!);
            await audit.RecordAsync(actorId, "settlement.finalize", "SettlementBatch", result.Batch!.Id,
                "succeeded", HttpContext.TraceIdentifier, null, CancellationToken.None);
            return Ok(SettlementResponse.FromBatch(result.Batch));
        }
        catch (DomainException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Settlement cannot be finalized.", detail: exception.Message,
                extensions: new Dictionary<string, object?> { ["code"] = "settlement_state_conflict" });
        }
    }

    private ObjectResult ToProblem(string code)
    {
        int status = code == "reconciliation_not_found" || code == "settlement_not_found"
            ? StatusCodes.Status404NotFound
            : StatusCodes.Status409Conflict;
        return Problem(statusCode: status, title: "Settlement operation failed.",
            extensions: new Dictionary<string, object?> { ["code"] = code });
    }
}
