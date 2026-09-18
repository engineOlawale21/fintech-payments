using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using FintechPayments.Application.Abstractions.Time;
using FintechPayments.Application.Abstractions.Webhooks;
using FintechPayments.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace FintechPayments.Infrastructure.Webhooks;

public sealed class MockProviderWebhookSignatureVerifier(
    IOptions<WebhookOptions> options,
    IClock clock) : IWebhookSignatureVerifier
{
    public WebhookSignatureResult Verify(
        string rawBody, string? timestamp, string? signature, string? version)
    {
        if (!string.Equals(version, "v1", StringComparison.Ordinal))
        {
            return Invalid("webhook_signature_version_invalid");
        }

        if (!long.TryParse(timestamp, NumberStyles.None, CultureInfo.InvariantCulture, out long unixSeconds))
        {
            return Invalid("webhook_timestamp_invalid");
        }

        DateTimeOffset providerTimestamp;
        try
        {
            providerTimestamp = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        }
        catch (ArgumentOutOfRangeException)
        {
            return Invalid("webhook_timestamp_invalid");
        }

        if ((clock.UtcNow - providerTimestamp).Duration() >
            TimeSpan.FromSeconds(options.Value.ReplayToleranceSeconds))
        {
            return Invalid("webhook_timestamp_expired");
        }

        if (string.IsNullOrWhiteSpace(signature))
        {
            return Invalid("webhook_signature_invalid");
        }

        byte[] supplied;
        try
        {
            supplied = Convert.FromHexString(signature);
        }
        catch (FormatException)
        {
            return Invalid("webhook_signature_invalid");
        }

        byte[] signingBytes = Encoding.UTF8.GetBytes($"{timestamp}.{rawBody}");
        byte[] expected = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(options.Value.Secret), signingBytes);
        return supplied.Length == expected.Length && CryptographicOperations.FixedTimeEquals(supplied, expected)
            ? new WebhookSignatureResult(true, null, providerTimestamp)
            : Invalid("webhook_signature_invalid");
    }

    private static WebhookSignatureResult Invalid(string code) => new(false, code, null);
}
