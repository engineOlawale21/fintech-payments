using System.Text;
using FintechPayments.Application.Webhooks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FintechPayments.Api.Controllers;

[ApiController]
[AllowAnonymous]
[EnableRateLimiting("webhooks")]
[Route("api/v1/webhooks/payments")]
public sealed class PaymentWebhooksController(WebhookService webhooks) : ControllerBase
{
    [HttpPost("mock-provider")]
    [RequestSizeLimit(262_144)]
    [Consumes("application/json")]
    [ProducesResponseType<WebhookResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        using StreamReader reader = new(Request.Body, Encoding.UTF8, leaveOpen: true);
        string rawBody = await reader.ReadToEndAsync(cancellationToken);
        WebhookProcessingResult result = await webhooks.ProcessAsync(
            rawBody,
            Request.Headers["X-Webhook-Timestamp"].FirstOrDefault(),
            Request.Headers["X-Webhook-Signature"].FirstOrDefault(),
            Request.Headers["X-Webhook-Signature-Version"].FirstOrDefault(),
            cancellationToken);

        if (result.Succeeded)
        {
            return Ok(new WebhookResponse(true, result.IsDuplicate, result.WebhookEventId!.Value));
        }

        int status = result.ErrorCode switch
        {
            "webhook_payload_invalid" => StatusCodes.Status400BadRequest,
            "webhook_event_conflict" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status401Unauthorized,
        };
        return Problem(
            statusCode: status,
            title: status == StatusCodes.Status401Unauthorized
                ? "Webhook authentication failed."
                : "Webhook could not be processed.",
            extensions: new Dictionary<string, object?> { ["code"] = result.ErrorCode });
    }
}

public sealed record WebhookResponse(bool Received, bool Duplicate, Guid EventId);
