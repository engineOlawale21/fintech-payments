using FintechPayments.Domain.Common;
using FintechPayments.Domain.Ledger;

namespace FintechPayments.UnitTests;

public sealed class LedgerTransactionTests
{
    [Fact]
    public void TransferCreatesBalancedImmutableEntries()
    {
        Guid source = Guid.NewGuid();
        Guid destination = Guid.NewGuid();
        Money money = Money.Create(75.25m, Currency.FromCode("USD"));

        LedgerTransaction transaction = LedgerTransaction.CreateTransfer(
            Guid.NewGuid(),
            source, destination, money, "transfer-001", DateTimeOffset.UtcNow);

        Assert.Equal(2, transaction.Entries.Count);
        LedgerEntry debit = Assert.Single(transaction.Entries, entry => entry.Direction == EntryDirection.Debit);
        LedgerEntry credit = Assert.Single(transaction.Entries, entry => entry.Direction == EntryDirection.Credit);
        Assert.Equal(source, debit.WalletId);
        Assert.Equal(destination, credit.WalletId);
        Assert.Equal(debit.Amount, credit.Amount);
        Assert.Equal(debit.Currency, credit.Currency);
    }

    [Fact]
    public void TransferRejectsSameWallet()
    {
        Guid walletId = Guid.NewGuid();

        Assert.Throws<DomainException>(() => LedgerTransaction.CreateTransfer(
            Guid.NewGuid(),
            walletId,
            walletId,
            Money.Create(10m, Currency.FromCode("NGN")),
            "transfer-002",
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void TransferRejectsMissingReference()
    {
        Assert.Throws<DomainException>(() => LedgerTransaction.CreateTransfer(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Money.Create(10m, Currency.FromCode("GBP")),
            "",
            DateTimeOffset.UtcNow));
    }
}
