using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FintechPayments.Application.Abstractions.Persistence;
using FintechPayments.Application.Abstractions.Time;
using FintechPayments.Application.Abstractions.Webhooks;
using FintechPayments.Domain.Common;
using FintechPayments.Domain.Webhooks;

namespace FintechPayments.Application.Webhooks;

public sealed class WebhookService(
    IWebhookSignatureVerifier signatures,
    IWebhookEventRepository events,
    IClock clock)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<WebhookProcessingResult> ProcessAsync(
        string rawBody,
        string? timestamp,
        string? signature,
        string? signatureVersion,
        CancellationToken cancellationToken)
    {
        WebhookSignatureResult verification = signatures.Verify(
            rawBody, timestamp, signature, signatureVersion);
        if (!verification.IsValid)
        {
            return WebhookProcessingResult.Failure(verification.ErrorCode!);
        }

        MockProviderEvent? payload;
        try
        {
            payload = JsonSerializer.Deserialize<MockProviderEvent>(rawBody, JsonOptions);
        }
        catch (JsonException)
        {
            return WebhookProcessingResult.Failure("webhook_payload_invalid");
        }

        if (payload is null || payload.EventId == Guid.Empty ||
            string.IsNullOrWhiteSpace(payload.EventType) ||
            string.IsNullOrWhiteSpace(payload.ResourceReference))
        {
            return WebhookProcessingResult.Failure("webhook_payload_invalid");
        }

        string payloadHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(rawBody))).ToLowerInvariant();
        WebhookEvent webhookEvent = WebhookEvent.Receive(
            "mock-provider",
            payload.EventId.ToString("N"),
            payload.EventType,
            payload.ResourceReference,
            payloadHash,
            rawBody,
            verification.ProviderTimestamp!.Value,
            clock.UtcNow);
        webhookEvent.MarkProcessed(clock.UtcNow);

        WebhookStoreOutcome outcome = await events.StoreOnceAsync(webhookEvent, cancellationToken);
        return outcome.Result switch
        {
            WebhookStoreResult.Stored => WebhookProcessingResult.Success(false, outcome.EventId),
            WebhookStoreResult.Duplicate => WebhookProcessingResult.Success(true, outcome.EventId),
            WebhookStoreResult.PayloadConflict => WebhookProcessingResult.Failure("webhook_event_conflict"),
            _ => throw new InvalidOperationException("Unknown webhook store result."),
        };
    }
}

public sealed record MockProviderEvent(
    Guid EventId,
    string EventType,
    string ResourceReference,
    decimal Amount,
    string Currency);

public sealed record WebhookProcessingResult(
    bool Succeeded,
    string? ErrorCode,
    bool IsDuplicate,
    Guid? WebhookEventId)
{
    public static WebhookProcessingResult Success(bool isDuplicate, Guid id) =>
        new(true, null, isDuplicate, id);

    public static WebhookProcessingResult Failure(string errorCode) =>
        new(false, errorCode, false, null);
}
