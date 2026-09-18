using FintechPayments.Domain.Common;
using FintechPayments.Domain.Webhooks;

namespace FintechPayments.UnitTests;

public sealed class WebhookEventTests
{
    [Fact]
    public void ReceivedEventCanBeMarkedProcessedOnce()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        WebhookEvent webhook = WebhookEvent.Receive(
            "mock-provider", "event-1", "payment.settled", "payment-1",
            new string('a', 64), "{}", now, now);

        webhook.MarkProcessed(now.AddSeconds(1));

        Assert.Equal(WebhookEventStatus.Processed, webhook.Status);
        Assert.Equal(now.AddSeconds(1), webhook.ProcessedAt);
        Assert.Throws<DomainException>(() => webhook.MarkProcessed(now.AddSeconds(2)));
    }
}
