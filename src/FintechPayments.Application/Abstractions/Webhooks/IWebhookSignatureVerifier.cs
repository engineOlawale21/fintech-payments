namespace FintechPayments.Application.Abstractions.Webhooks;

public interface IWebhookSignatureVerifier
{
    WebhookSignatureResult Verify(string rawBody, string? timestamp, string? signature, string? version);
}

public sealed record WebhookSignatureResult(bool IsValid, string? ErrorCode, DateTimeOffset? ProviderTimestamp)
{
    public static WebhookSignatureResult Valid(DateTimeOffset timestamp) => new(true, null, timestamp);
    public static WebhookSignatureResult Invalid(string errorCode) => new(false, errorCode, null);
}
