using FintechPayments.Domain.Common;

namespace FintechPayments.Domain.Webhooks;

public sealed class WebhookEvent
{
    private WebhookEvent()
    {
    }

    private WebhookEvent(
        Guid id,
        string provider,
        string externalEventId,
        string eventType,
        string resourceReference,
        string payloadHash,
        string payload,
        DateTimeOffset providerTimestamp,
        DateTimeOffset receivedAt)
    {
        Id = id;
        Provider = provider;
        ExternalEventId = externalEventId;
        EventType = eventType;
        ResourceReference = resourceReference;
        PayloadHash = payloadHash;
        Payload = payload;
        ProviderTimestamp = providerTimestamp.ToUniversalTime();
        ReceivedAt = receivedAt.ToUniversalTime();
        Status = WebhookEventStatus.Received;
    }

    public Guid Id { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string ExternalEventId { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public string ResourceReference { get; private set; } = string.Empty;
    public string PayloadHash { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTimeOffset ProviderTimestamp { get; private set; }
    public DateTimeOffset ReceivedAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public WebhookEventStatus Status { get; private set; }
    public string? FailureReason { get; private set; }

    public static WebhookEvent Receive(
        string provider,
        string externalEventId,
        string eventType,
        string resourceReference,
        string payloadHash,
        string payload,
        DateTimeOffset providerTimestamp,
        DateTimeOffset receivedAt)
    {
        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(externalEventId) ||
            string.IsNullOrWhiteSpace(eventType) || string.IsNullOrWhiteSpace(resourceReference) ||
            payloadHash.Length != 64 || string.IsNullOrWhiteSpace(payload))
        {
            throw new DomainException("Webhook event is invalid.");
        }

        return new WebhookEvent(
            Guid.NewGuid(), provider, externalEventId, eventType,
            resourceReference, payloadHash, payload, providerTimestamp, receivedAt);
    }

    public void MarkProcessed(DateTimeOffset processedAt)
    {
        if (Status != WebhookEventStatus.Received)
        {
            throw new DomainException("Only a received webhook can be processed.");
        }

        Status = WebhookEventStatus.Processed;
        ProcessedAt = processedAt.ToUniversalTime();
    }
}
