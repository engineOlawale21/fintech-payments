using FintechPayments.Domain.Reconciliation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FintechPayments.Infrastructure.Persistence.Configurations;

internal sealed class ReconciliationItemConfiguration : IEntityTypeConfiguration<ReconciliationItem>
{
    public void Configure(EntityTypeBuilder<ReconciliationItem> builder)
    {
        builder.ToTable("reconciliation_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.ReconciliationRunId).HasColumnName("reconciliation_run_id").IsRequired();
        builder.Property(x => x.Reference).HasColumnName("reference").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Category).HasColumnName("category").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.InternalAmount).HasColumnName("internal_amount").HasPrecision(19, 4);
        builder.Property(x => x.ExternalAmount).HasColumnName("external_amount").HasPrecision(19, 4);
        builder.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(x => x.ResolutionNote).HasColumnName("resolution_note").HasMaxLength(500);
        builder.Property(x => x.ResolvedBy).HasColumnName("resolved_by");
        builder.Property(x => x.ResolvedAt).HasColumnName("resolved_at");
        builder.HasIndex(x => new { x.ReconciliationRunId, x.Category })
            .HasDatabaseName("ix_reconciliation_items_run_category");
    }
}
