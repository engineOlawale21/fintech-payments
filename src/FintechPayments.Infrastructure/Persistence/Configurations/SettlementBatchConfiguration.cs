using FintechPayments.Domain.Reconciliation;
using FintechPayments.Domain.Settlements;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FintechPayments.Infrastructure.Persistence.Configurations;

internal sealed class SettlementBatchConfiguration : IEntityTypeConfiguration<SettlementBatch>
{
    public void Configure(EntityTypeBuilder<SettlementBatch> builder)
    {
        builder.ToTable("settlement_batches");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.ReconciliationRunId).HasColumnName("reconciliation_run_id").IsRequired();
        builder.Property(x => x.PeriodStart).HasColumnName("period_start").IsRequired();
        builder.Property(x => x.PeriodEnd).HasColumnName("period_end").IsRequired();
        builder.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(x => x.GrossAmount).HasColumnName("gross_amount").HasPrecision(19, 4).IsRequired();
        builder.Property(x => x.FeeAmount).HasColumnName("fee_amount").HasPrecision(19, 4).IsRequired();
        builder.Property(x => x.AdjustmentAmount).HasColumnName("adjustment_amount").HasPrecision(19, 4).IsRequired();
        builder.Property(x => x.AdjustmentReason).HasColumnName("adjustment_reason").HasMaxLength(500);
        builder.Property(x => x.NetAmount).HasColumnName("net_amount").HasPrecision(19, 4).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.FinalizedAt).HasColumnName("finalized_at");
        builder.Property(x => x.FinalizedBy).HasColumnName("finalized_by");
        builder.HasOne<ReconciliationRun>().WithMany().HasForeignKey(x => x.ReconciliationRunId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.SettlementBatchId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(x => new { x.Status, x.Currency, x.PeriodStart, x.PeriodEnd })
            .HasDatabaseName("ix_settlement_batches_period");
    }
}
