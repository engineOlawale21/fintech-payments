using FintechPayments.Domain.Common;
using FintechPayments.Domain.Wallets;

namespace FintechPayments.UnitTests;

public sealed class WalletTests
{
    [Fact]
    public void CreateInitializesActiveEmptyWalletInUtc()
    {
        DateTimeOffset createdAt = new(2026, 9, 11, 22, 0, 0, TimeSpan.FromHours(1));

        Wallet wallet = Wallet.Create(Guid.NewGuid(), Currency.FromCode("USD"), createdAt);

        Assert.NotEqual(Guid.Empty, wallet.Id);
        Assert.Equal("USD", wallet.Currency);
        Assert.Equal(0m, wallet.AvailableBalance);
        Assert.Equal(WalletStatus.Active, wallet.Status);
        Assert.Equal(TimeSpan.Zero, wallet.CreatedAt.Offset);
    }

    [Fact]
    public void CreateRejectsMissingOwner()
    {
        Assert.Throws<DomainException>(() =>
            Wallet.Create(Guid.Empty, Currency.FromCode("USD"), DateTimeOffset.UtcNow));
    }
}
