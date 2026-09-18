using FintechPayments.Domain.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FintechPayments.Infrastructure.Persistence.Configurations;

internal sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("audit_events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.ActorId).HasColumnName("actor_id");
        builder.Property(x => x.Action).HasColumnName("action").HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(64).IsRequired();
        builder.Property(x => x.EntityId).HasColumnName("entity_id");
        builder.Property(x => x.Outcome).HasColumnName("outcome").HasMaxLength(32).IsRequired();
        builder.Property(x => x.CorrelationId).HasColumnName("correlation_id").HasMaxLength(128).IsRequired();
        builder.Property(x => x.Metadata).HasColumnName("metadata").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.HasIndex(x => new { x.EntityType, x.EntityId, x.CreatedAt })
            .HasDatabaseName("ix_audit_events_entity_created");
        builder.HasIndex(x => new { x.ActorId, x.CreatedAt })
            .HasDatabaseName("ix_audit_events_actor_created");
        builder.HasIndex(x => x.CorrelationId).HasDatabaseName("ix_audit_events_correlation");
    }
}
