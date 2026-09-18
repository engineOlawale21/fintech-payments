using FintechPayments.Api.Authentication;
using FintechPayments.Api.Contracts.Reconciliation;
using FintechPayments.Application.Abstractions.Persistence;
using FintechPayments.Application.Reconciliation;
using FintechPayments.Domain.Common;
using FintechPayments.Domain.Reconciliation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using FintechPayments.Application.Auditing;

namespace FintechPayments.Api.Controllers;

[ApiController]
[Authorize(Policy = "OperationsOnly")]
[EnableRateLimiting("operations")]
[Route("api/v1/reconciliations")]
public sealed class ReconciliationsController(
    ReconciliationService reconciliations, AuditService audit) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ReconciliationResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ReconciliationResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(
        CreateReconciliationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            ReconciliationStoreResult result = await reconciliations.CreateAsync(
                request.Provider, request.PeriodStart, request.PeriodEnd,
                request.Records.Select(x => new ProviderStatementRecord(
                    x.Reference, x.Amount, x.Currency)).ToArray(), cancellationToken);
            ReconciliationResponse response = ReconciliationResponse.FromRun(result.Run);
            User.TryGetUserId(out Guid actorId);
            await audit.RecordAsync(actorId, "reconciliation.create", "ReconciliationRun", response.Id,
                result.IsReplay ? "replayed" : "succeeded",
                HttpContext.TraceIdentifier, null, CancellationToken.None);
            return result.IsReplay
                ? Ok(response)
                : CreatedAtAction(nameof(Get), new { reconciliationId = response.Id }, response);
        }
        catch (DomainException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Reconciliation request is invalid.",
                detail: exception.Message,
                extensions: new Dictionary<string, object?> { ["code"] = "validation_failed" });
        }
    }

    [HttpGet("{reconciliationId:guid}")]
    public async Task<IActionResult> Get(Guid reconciliationId, CancellationToken cancellationToken)
    {
        ReconciliationRun? run = await reconciliations.GetAsync(
            reconciliationId, false, cancellationToken);
        return run is null ? NotFound() : Ok(ReconciliationResponse.FromRun(run));
    }

    [HttpGet("{reconciliationId:guid}/items")]
    public async Task<IActionResult> GetItems(Guid reconciliationId, CancellationToken cancellationToken)
    {
        ReconciliationRun? run = await reconciliations.GetAsync(
            reconciliationId, true, cancellationToken);
        return run is null
            ? NotFound()
            : Ok(run.Items.OrderBy(x => x.Reference).Select(ReconciliationItemResponse.FromItem));
    }

    [HttpPost("{reconciliationId:guid}/items/{itemId:guid}/resolve")]
    public async Task<IActionResult> Resolve(
        Guid reconciliationId, Guid itemId, ResolveReconciliationItemRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out Guid actorId)) return Unauthorized();
        try
        {
            ReconciliationItem? item = await reconciliations.ResolveAsync(
                reconciliationId, itemId, actorId, request.Note, cancellationToken);
            if (item is null) return NotFound();
            await audit.RecordAsync(actorId, "reconciliation.resolve", "ReconciliationItem", item.Id,
                "succeeded", HttpContext.TraceIdentifier, null, CancellationToken.None);
            return Ok(ReconciliationItemResponse.FromItem(item));
        }
        catch (DomainException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Reconciliation item cannot be resolved.",
                detail: exception.Message,
                extensions: new Dictionary<string, object?> { ["code"] = "reconciliation_resolution_conflict" });
        }
    }
}
