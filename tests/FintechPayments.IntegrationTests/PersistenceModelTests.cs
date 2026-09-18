using FintechPayments.Domain.Auditing;
using FintechPayments.Domain.Wallets;
using FintechPayments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FintechPayments.IntegrationTests;

public sealed class PersistenceModelTests
{
    [Fact]
    public void WalletMappingDefinesFinancialConstraints()
    {
        DbContextOptions<PaymentsDbContext> options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseNpgsql("Host=unused;Database=unused")
            .Options;

        using PaymentsDbContext context = new(options);
        IModel designTimeModel = context.GetService<IDesignTimeModel>().Model;
        IEntityType wallet = designTimeModel.FindEntityType(typeof(Wallet))
            ?? throw new InvalidOperationException("Wallet mapping was not registered.");
        IProperty balance = wallet.FindProperty(nameof(Wallet.AvailableBalance))
            ?? throw new InvalidOperationException("Balance mapping was not registered.");

        Assert.Equal(19, balance.GetPrecision());
        Assert.Equal(4, balance.GetScale());
        Assert.True(wallet.FindProperty(nameof(Wallet.Version))?.IsConcurrencyToken);
        Assert.Contains(wallet.GetIndexes(), index => index.IsUnique);
        Assert.Contains(wallet.GetCheckConstraints(), constraint =>
            constraint.Name == "ck_wallets_balance_non_negative");
    }

    [Fact]
    public void OutboxMappingDefinesPendingMessageIndex()
    {
        DbContextOptions<PaymentsDbContext> options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseNpgsql("Host=unused;Database=unused")
            .Options;

        using PaymentsDbContext context = new(options);
        IModel designTimeModel = context.GetService<IDesignTimeModel>().Model;
        IEntityType outbox = designTimeModel.FindEntityType(typeof(OutboxMessage))
            ?? throw new InvalidOperationException("Outbox mapping was not registered.");

        Assert.Contains(outbox.GetIndexes(), index =>
            index.GetDatabaseName() == "ix_outbox_messages_pending" && !index.IsUnique);
    }
}
