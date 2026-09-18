using FintechPayments.Domain.Payments;
using FintechPayments.Domain.Transfers;
using FintechPayments.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FintechPayments.Infrastructure.Persistence.Configurations;

internal sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("idempotency_records");
        builder.HasKey(record => record.Id);
        builder.Property(record => record.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(record => record.ActorId).HasColumnName("actor_id").IsRequired();
        builder.Property(record => record.Operation).HasColumnName("operation").HasMaxLength(100).IsRequired();
        builder.Property(record => record.Key).HasColumnName("key").HasMaxLength(100).IsRequired();
        builder.Property(record => record.RequestHash).HasColumnName("request_hash").HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(record => record.State).HasColumnName("state").HasConversion<string>().HasMaxLength(24).IsRequired();
        builder.Property(record => record.TransferId).HasColumnName("transfer_id");
        builder.Property(record => record.ResponseStatusCode).HasColumnName("response_status_code");
        builder.Property(record => record.ResponseBody).HasColumnName("response_body").HasColumnType("jsonb");
        builder.Property(record => record.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(record => record.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(record => record.ExpiresAt).HasColumnName("expires_at").IsRequired();

        builder.HasIndex(record => new { record.ActorId, record.Operation, record.Key })
            .IsUnique()
            .HasDatabaseName("ux_idempotency_actor_operation_key");
        builder.HasIndex(record => record.TransferId)
            .IsUnique()
            .HasFilter("transfer_id IS NOT NULL")
            .HasDatabaseName("ux_idempotency_transfer_id");
        builder.HasIndex(record => record.ExpiresAt)
            .HasDatabaseName("ix_idempotency_expires_at");

        builder.HasOne<User>().WithMany().HasForeignKey(record => record.ActorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Transfer>().WithOne().HasForeignKey<IdempotencyRecord>(record => record.TransferId).OnDelete(DeleteBehavior.Restrict);
    }
}
