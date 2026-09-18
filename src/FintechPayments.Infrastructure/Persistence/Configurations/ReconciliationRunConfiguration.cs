using FintechPayments.Domain.Reconciliation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FintechPayments.Infrastructure.Persistence.Configurations;

internal sealed class ReconciliationRunConfiguration : IEntityTypeConfiguration<ReconciliationRun>
{
    public void Configure(EntityTypeBuilder<ReconciliationRun> builder)
    {
        builder.ToTable("reconciliation_runs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.Provider).HasColumnName("provider").HasMaxLength(64).IsRequired();
        builder.Property(x => x.PeriodStart).HasColumnName("period_start").IsRequired();
        builder.Property(x => x.PeriodEnd).HasColumnName("period_end").IsRequired();
        builder.Property(x => x.InputChecksum).HasColumnName("input_checksum").HasColumnType("char(64)").IsRequired();
        builder.Property(x => x.MatchedCount).HasColumnName("matched_count").IsRequired();
        builder.Property(x => x.DiscrepancyCount).HasColumnName("discrepancy_count").IsRequired();
        builder.Property(x => x.InternalTotal).HasColumnName("internal_total").HasPrecision(19, 4).IsRequired();
        builder.Property(x => x.ExternalTotal).HasColumnName("external_total").HasPrecision(19, 4).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.ReconciliationRunId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(x => new { x.Provider, x.PeriodStart, x.PeriodEnd, x.InputChecksum })
            .IsUnique().HasDatabaseName("ux_reconciliation_runs_identity");
    }
}
