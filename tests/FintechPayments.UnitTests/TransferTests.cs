using FintechPayments.Domain.Common;
using FintechPayments.Domain.Transfers;
using FintechPayments.Domain.Wallets;

namespace FintechPayments.UnitTests;

public sealed class TransferTests
{
    [Fact]
    public void WalletDebitAndCreditPreserveExpectedBalances()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Currency usd = Currency.FromCode("USD");
        Wallet source = Wallet.Create(Guid.NewGuid(), usd, now);
        Wallet destination = Wallet.Create(Guid.NewGuid(), usd, now);
        source.Credit(Money.Create(100m, usd), now);

        source.Debit(Money.Create(35.50m, usd), now);
        destination.Credit(Money.Create(35.50m, usd), now);

        Assert.Equal(64.50m, source.AvailableBalance);
        Assert.Equal(35.50m, destination.AvailableBalance);
        Assert.Equal(2, source.Version);
        Assert.Equal(1, destination.Version);
    }

    [Fact]
    public void WalletCannotBeOverdrawn()
    {
        Wallet wallet = Wallet.Create(
            Guid.NewGuid(), Currency.FromCode("NGN"), DateTimeOffset.UtcNow);

        Assert.Throws<DomainException>(() => wallet.Debit(
            Money.Create(1m, Currency.FromCode("NGN")), DateTimeOffset.UtcNow));
        Assert.Equal(0m, wallet.AvailableBalance);
    }

    [Fact]
    public void WalletRejectsAnotherCurrency()
    {
        Wallet wallet = Wallet.Create(
            Guid.NewGuid(), Currency.FromCode("USD"), DateTimeOffset.UtcNow);

        Assert.Throws<DomainException>(() => wallet.Credit(
            Money.Create(1m, Currency.FromCode("GBP")), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void TransferMovesFromPendingToCompleted()
    {
        Transfer transfer = Transfer.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Money.Create(10m, Currency.FromCode("USD")),
            "transfer-state-test",
            DateTimeOffset.UtcNow);

        transfer.Complete(DateTimeOffset.UtcNow);

        Assert.Equal(TransferStatus.Completed, transfer.Status);
        Assert.Throws<DomainException>(() => transfer.Complete(DateTimeOffset.UtcNow));
    }
}
