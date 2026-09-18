using FintechPayments.Domain.Ledger;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FintechPayments.Domain.Transfers;

namespace FintechPayments.Infrastructure.Persistence.Configurations;

internal sealed class LedgerTransactionConfiguration : IEntityTypeConfiguration<LedgerTransaction>
{
    public void Configure(EntityTypeBuilder<LedgerTransaction> builder)
    {
        builder.ToTable("ledger_transactions");
        builder.HasKey(transaction => transaction.Id);
        builder.Property(transaction => transaction.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(transaction => transaction.TransferId).HasColumnName("transfer_id");
        builder.Property(transaction => transaction.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(24).IsRequired();
        builder.Property(transaction => transaction.Reference).HasColumnName("reference").HasMaxLength(100).IsRequired();
        builder.Property(transaction => transaction.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.HasIndex(transaction => transaction.Reference).IsUnique().HasDatabaseName("ux_ledger_transactions_reference");
        builder.HasIndex(transaction => transaction.TransferId)
            .IsUnique()
            .HasFilter("transfer_id IS NOT NULL")
            .HasDatabaseName("ux_ledger_transactions_transfer_id");
        builder.HasOne<Transfer>()
            .WithOne()
            .HasForeignKey<LedgerTransaction>(transaction => transaction.TransferId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(transaction => transaction.Entries)
            .WithOne()
            .HasForeignKey(entry => entry.LedgerTransactionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(transaction => transaction.Entries).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
