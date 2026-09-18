using FintechPayments.Domain.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FintechPayments.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(message => message.MessageType).HasColumnName("message_type")
            .HasMaxLength(100).IsRequired();
        builder.Property(message => message.Payload).HasColumnName("payload")
            .HasColumnType("jsonb").IsRequired();
        builder.Property(message => message.OccurredAt).HasColumnName("occurred_at").IsRequired();
        builder.Property(message => message.ProcessedAt).HasColumnName("processed_at");
        builder.Property(message => message.AttemptCount).HasColumnName("attempt_count")
            .IsRequired();
        builder.HasIndex(message => new { message.ProcessedAt, message.OccurredAt })
            .HasDatabaseName("ix_outbox_messages_pending");
    }
}
