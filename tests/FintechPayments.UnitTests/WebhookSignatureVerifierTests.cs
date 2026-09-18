using System.Security.Cryptography;
using System.Text;
using FintechPayments.Application.Abstractions.Time;
using FintechPayments.Infrastructure.Configuration;
using FintechPayments.Infrastructure.Webhooks;
using Microsoft.Extensions.Options;

namespace FintechPayments.UnitTests;

public sealed class WebhookSignatureVerifierTests
{
    private const string Secret = "unit-test-webhook-secret-at-least-32-characters";
    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);

    [Fact]
    public void CorrectTimestampedHmacIsAccepted()
    {
        const string body = "{\"eventId\":\"event-1\"}";
        string timestamp = Now.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);

        var result = CreateVerifier().Verify(body, timestamp, Sign(timestamp, body), "v1");

        Assert.True(result.IsValid);
        Assert.Equal(Now, result.ProviderTimestamp);
    }

    [Fact]
    public void ChangedBodyAndStaleTimestampAreRejected()
    {
        string timestamp = Now.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
        string signature = Sign(timestamp, "original");

        Assert.False(CreateVerifier().Verify("changed", timestamp, signature, "v1").IsValid);
        Assert.Equal(
            "webhook_timestamp_expired",
            CreateVerifier().Verify(
                "original",
                (Now.ToUnixTimeSeconds() - 301).ToString(System.Globalization.CultureInfo.InvariantCulture),
                signature,
                "v1").ErrorCode);
    }

    private static MockProviderWebhookSignatureVerifier CreateVerifier() => new(
        Options.Create(new WebhookOptions { Secret = Secret, ReplayToleranceSeconds = 300 }),
        new FixedClock());

    private static string Sign(string timestamp, string body) => Convert.ToHexString(
        HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(Secret),
            Encoding.UTF8.GetBytes($"{timestamp}.{body}"))).ToLowerInvariant();

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }
}
