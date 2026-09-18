using FintechPayments.Domain.Settlements;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FintechPayments.Infrastructure.Persistence.Configurations;

internal sealed class SettlementItemConfiguration : IEntityTypeConfiguration<SettlementItem>
{
    public void Configure(EntityTypeBuilder<SettlementItem> builder)
    {
        builder.ToTable("settlement_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.SettlementBatchId).HasColumnName("settlement_batch_id").IsRequired();
        builder.Property(x => x.ReconciliationItemId).HasColumnName("reconciliation_item_id").IsRequired();
        builder.HasIndex(x => x.ReconciliationItemId).IsUnique()
            .HasDatabaseName("ux_settlement_items_reconciliation_item");
    }
}
