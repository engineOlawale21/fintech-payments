using FintechPayments.Domain.Webhooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FintechPayments.Infrastructure.Persistence.Configurations;

public sealed class WebhookEventConfiguration : IEntityTypeConfiguration<WebhookEvent>
{
    public void Configure(EntityTypeBuilder<WebhookEvent> builder)
    {
        builder.ToTable("webhook_events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Provider).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ExternalEventId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.EventType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ResourceReference).HasMaxLength(128).IsRequired();
        builder.Property(x => x.PayloadHash).HasColumnType("char(64)").IsRequired();
        builder.Property(x => x.Payload).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.FailureReason).HasMaxLength(512);
        builder.HasIndex(x => new { x.Provider, x.ExternalEventId }).IsUnique();
        builder.HasIndex(x => new { x.Status, x.ReceivedAt });
    }
}
