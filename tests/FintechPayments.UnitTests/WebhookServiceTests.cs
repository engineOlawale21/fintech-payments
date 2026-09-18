using FintechPayments.Application.Abstractions.Persistence;
using FintechPayments.Application.Abstractions.Time;
using FintechPayments.Application.Abstractions.Webhooks;
using FintechPayments.Application.Webhooks;
using FintechPayments.Domain.Webhooks;

namespace FintechPayments.UnitTests;

public sealed class WebhookServiceTests
{
    private const string Payload =
        "{\"eventId\":\"26e2f5c3-2239-4bdb-92e8-bb942b7f45d3\",\"eventType\":\"payment.settled\",\"resourceReference\":\"payment-42\",\"amount\":25.50,\"currency\":\"USD\"}";

    [Fact]
    public async Task InvalidSignatureIsRejectedBeforePersistence()
    {
        FakeRepository repository = new(WebhookStoreResult.Stored);
        WebhookService service = new(new FakeVerifier(false), repository, new FakeClock());

        WebhookProcessingResult result = await service.ProcessAsync(
            Payload, "timestamp", "signature", "v1", CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("webhook_signature_invalid", result.ErrorCode);
        Assert.Null(repository.StoredEvent);
    }

    [Theory]
    [InlineData(WebhookStoreResult.Stored, false)]
    [InlineData(WebhookStoreResult.Duplicate, true)]
    public async Task ValidDeliveryReportsStorageOutcome(WebhookStoreResult storeResult, bool duplicate)
    {
        FakeRepository repository = new(storeResult);
        WebhookService service = new(new FakeVerifier(true), repository, new FakeClock());

        WebhookProcessingResult result = await service.ProcessAsync(
            Payload, "timestamp", "signature", "v1", CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(duplicate, result.IsDuplicate);
        Assert.Equal(WebhookEventStatus.Processed, repository.StoredEvent!.Status);
    }

    [Fact]
    public async Task ReusedEventIdWithChangedPayloadIsRejected()
    {
        WebhookService service = new(
            new FakeVerifier(true),
            new FakeRepository(WebhookStoreResult.PayloadConflict),
            new FakeClock());

        WebhookProcessingResult result = await service.ProcessAsync(
            Payload, "timestamp", "signature", "v1", CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("webhook_event_conflict", result.ErrorCode);
    }

    private sealed class FakeVerifier(bool valid) : IWebhookSignatureVerifier
    {
        public WebhookSignatureResult Verify(string rawBody, string? timestamp, string? signature, string? version) =>
            valid
                ? new(true, null, DateTimeOffset.UnixEpoch)
                : new(false, "webhook_signature_invalid", null);
    }

    private sealed class FakeRepository(WebhookStoreResult result) : IWebhookEventRepository
    {
        public WebhookEvent? StoredEvent { get; private set; }

        public Task<WebhookStoreOutcome> StoreOnceAsync(
            WebhookEvent webhookEvent, CancellationToken cancellationToken)
        {
            StoredEvent = webhookEvent;
            return Task.FromResult(new WebhookStoreOutcome(result, webhookEvent.Id));
        }
    }

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UnixEpoch.AddMinutes(1);
    }
}
