using FintechPayments.Domain.Wallets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FintechPayments.Domain.Users;

namespace FintechPayments.Infrastructure.Persistence.Configurations;

internal sealed class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable("wallets", table =>
        {
            table.HasCheckConstraint("ck_wallets_balance_non_negative", "available_balance >= 0");
            table.HasCheckConstraint("ck_wallets_currency_length", "char_length(currency) = 3");
        });

        builder.HasKey(wallet => wallet.Id);
        builder.Property(wallet => wallet.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(wallet => wallet.OwnerId).HasColumnName("owner_id").IsRequired();
        builder.Property(wallet => wallet.Currency).HasColumnName("currency").HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(wallet => wallet.AvailableBalance).HasColumnName("available_balance").HasPrecision(19, 4).IsRequired();
        builder.Property(wallet => wallet.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(wallet => wallet.Version).HasColumnName("version").IsConcurrencyToken().IsRequired();
        builder.Property(wallet => wallet.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(wallet => wallet.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasIndex(wallet => new { wallet.OwnerId, wallet.Currency })
            .IsUnique()
            .HasDatabaseName("ux_wallets_owner_currency");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(wallet => wallet.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
