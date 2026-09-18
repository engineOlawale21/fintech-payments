using FintechPayments.Domain.Auditing;
using FintechPayments.Domain.Common;

namespace FintechPayments.UnitTests;

public sealed class AuditEventTests
{
    [Fact]
    public void CreateCapturesControlledAuditFields()
    {
        Guid actorId = Guid.NewGuid();
        Guid entityId = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        AuditEvent auditEvent = AuditEvent.Create(
            actorId, "transfer.create", "Transfer", entityId,
            "succeeded", "correlation-42", "{\"errorCode\":null}", now);

        Assert.Equal(actorId, auditEvent.ActorId);
        Assert.Equal(entityId, auditEvent.EntityId);
        Assert.Equal("correlation-42", auditEvent.CorrelationId);
        Assert.Equal(now.ToUniversalTime(), auditEvent.CreatedAt);
    }

    [Fact]
    public void CreateRejectsMissingCorrelationId()
    {
        Assert.Throws<DomainException>(() => AuditEvent.Create(
            null, "user.login", "User", null, "rejected", "", "{}", DateTimeOffset.UtcNow));
    }
}
