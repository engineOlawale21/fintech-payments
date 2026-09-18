using FintechPayments.Domain.Transfers;
using FintechPayments.Domain.Wallets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FintechPayments.Infrastructure.Persistence.Configurations;

internal sealed class TransferConfiguration : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> builder)
    {
        builder.ToTable("transfers", table =>
        {
            table.HasCheckConstraint("ck_transfers_amount_positive", "amount > 0");
            table.HasCheckConstraint("ck_transfers_distinct_wallets", "source_wallet_id <> destination_wallet_id");
        });
        builder.HasKey(transfer => transfer.Id);
        builder.Property(transfer => transfer.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(transfer => transfer.SourceWalletId).HasColumnName("source_wallet_id").IsRequired();
        builder.Property(transfer => transfer.DestinationWalletId).HasColumnName("destination_wallet_id").IsRequired();
        builder.Property(transfer => transfer.Amount).HasColumnName("amount").HasPrecision(19, 4).IsRequired();
        builder.Property(transfer => transfer.Currency).HasColumnName("currency").HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(transfer => transfer.Reference).HasColumnName("reference").HasMaxLength(100).IsRequired();
        builder.Property(transfer => transfer.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(transfer => transfer.FailureReason).HasColumnName("failure_reason").HasMaxLength(200);
        builder.Property(transfer => transfer.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(transfer => transfer.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasIndex(transfer => transfer.Reference).IsUnique().HasDatabaseName("ux_transfers_reference");
        builder.HasIndex(transfer => new { transfer.SourceWalletId, transfer.CreatedAt })
            .HasDatabaseName("ix_transfers_source_created");
        builder.HasIndex(transfer => new { transfer.DestinationWalletId, transfer.CreatedAt })
            .HasDatabaseName("ix_transfers_destination_created");
        builder.HasOne<Wallet>().WithMany().HasForeignKey(transfer => transfer.SourceWalletId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Wallet>().WithMany().HasForeignKey(transfer => transfer.DestinationWalletId).OnDelete(DeleteBehavior.Restrict);
    }
}
