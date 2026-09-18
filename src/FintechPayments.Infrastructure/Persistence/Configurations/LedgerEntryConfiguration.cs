using FintechPayments.Domain.Ledger;
using FintechPayments.Domain.Wallets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FintechPayments.Infrastructure.Persistence.Configurations;

internal sealed class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable("ledger_entries", table =>
        {
            table.HasCheckConstraint("ck_ledger_entries_amount_positive", "amount > 0");
            table.HasCheckConstraint("ck_ledger_entries_currency_length", "char_length(currency) = 3");
        });
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(entry => entry.LedgerTransactionId).HasColumnName("ledger_transaction_id").IsRequired();
        builder.Property(entry => entry.WalletId).HasColumnName("wallet_id").IsRequired();
        builder.Property(entry => entry.Direction).HasColumnName("direction").HasConversion<string>().HasMaxLength(8).IsRequired();
        builder.Property(entry => entry.Amount).HasColumnName("amount").HasPrecision(19, 4).IsRequired();
        builder.Property(entry => entry.Currency).HasColumnName("currency").HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(entry => entry.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasOne<Wallet>()
            .WithMany()
            .HasForeignKey(entry => entry.WalletId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entry => new { entry.WalletId, entry.CreatedAt, entry.Id })
            .HasDatabaseName("ix_ledger_entries_wallet_created_id");
    }
}
