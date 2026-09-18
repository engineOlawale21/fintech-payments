using FintechPayments.Domain.Common;
using FintechPayments.Domain.Payments;

namespace FintechPayments.UnitTests;

public sealed class IdempotencyRecordTests
{
    [Fact]
    public void ReserveAndCompleteCaptureDurableOutcome()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        IdempotencyRecord record = IdempotencyRecord.Reserve(
            Guid.NewGuid(), "create_transfer", "request-1", new string('a', 64), now, TimeSpan.FromHours(24));
        Guid transferId = Guid.NewGuid();

        record.Complete(transferId, "{\"status\":\"completed\"}", now);

        Assert.Equal(IdempotencyState.Completed, record.State);
        Assert.Equal(transferId, record.TransferId);
        Assert.Equal(201, record.ResponseStatusCode);
        Assert.NotNull(record.ResponseBody);
    }

    [Fact]
    public void CompletedRecordCannotBeCompletedAgain()
    {
        IdempotencyRecord record = IdempotencyRecord.Reserve(
            Guid.NewGuid(), "create_transfer", "request-2", new string('b', 64),
            DateTimeOffset.UtcNow, TimeSpan.FromHours(24));
        record.Complete(Guid.NewGuid(), "{}", DateTimeOffset.UtcNow);

        Assert.Throws<DomainException>(() =>
            record.Complete(Guid.NewGuid(), "{}", DateTimeOffset.UtcNow));
    }
}
