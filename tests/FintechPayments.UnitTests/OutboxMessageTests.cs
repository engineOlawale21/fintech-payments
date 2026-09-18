using FintechPayments.Domain.Auditing;
using FintechPayments.Domain.Common;

namespace FintechPayments.UnitTests;

public sealed class OutboxMessageTests
{
    [Fact]
    public void CreateStoresPendingMessage()
    {
        DateTimeOffset occurredAt = DateTimeOffset.UtcNow;

        OutboxMessage message = OutboxMessage.Create(
            "transfer.completed", "{\"transferId\":\"42\"}", occurredAt);

        Assert.NotEqual(Guid.Empty, message.Id);
        Assert.Equal("transfer.completed", message.MessageType);
        Assert.Equal("{\"transferId\":\"42\"}", message.Payload);
        Assert.Equal(occurredAt, message.OccurredAt);
        Assert.Null(message.ProcessedAt);
        Assert.Equal(0, message.AttemptCount);
    }

    [Fact]
    public void CreateRejectsMissingPayload()
    {
        Assert.Throws<DomainException>(() => OutboxMessage.Create(
            "transfer.completed", "", DateTimeOffset.UtcNow));
    }
}
