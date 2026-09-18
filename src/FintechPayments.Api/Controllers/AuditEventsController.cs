using System.Text.Json;
using FintechPayments.Application.Auditing;
using FintechPayments.Domain.Auditing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FintechPayments.Api.Controllers;

[ApiController]
[Authorize(Policy = "OperationsOnly")]
[EnableRateLimiting("operations")]
[Route("api/v1/audit-events")]
public sealed class AuditEventsController(AuditService audit) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? correlationId,
        [FromQuery] DateTimeOffset? before,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AuditEvent> events = await audit.ListAsync(
            correlationId, before, pageSize, cancellationToken);
        return Ok(events.Select(x => new
        {
            x.Id, x.ActorId, x.Action, x.EntityType, x.EntityId,
            x.Outcome, x.CorrelationId,
            Metadata = JsonSerializer.Deserialize<JsonElement>(x.Metadata),
            x.CreatedAt,
        }));
    }
}
