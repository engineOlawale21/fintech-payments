using FintechPayments.Domain.Common;

namespace FintechPayments.Domain.Wallets;

public sealed class Wallet
{
    private Wallet()
    {
    }

    private Wallet(Guid id, Guid ownerId, Currency currency, DateTimeOffset createdAt)
    {
        Id = id;
        OwnerId = ownerId;
        Currency = currency.Code;
        Status = WalletStatus.Active;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid OwnerId { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public decimal AvailableBalance { get; private set; }

    public WalletStatus Status { get; private set; }

    public long Version { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Wallet Create(Guid ownerId, Currency currency, DateTimeOffset createdAt)
    {
        if (ownerId == Guid.Empty)
        {
            throw new DomainException("Wallet owner is required.");
        }

        return new Wallet(Guid.NewGuid(), ownerId, currency, createdAt.ToUniversalTime());
    }

    public void Debit(Money money, DateTimeOffset updatedAt)
    {
        EnsureCanPost(money);
        if (AvailableBalance < money.Amount)
        {
            throw new DomainException("Wallet has insufficient funds.");
        }

        AvailableBalance -= money.Amount;
        MarkUpdated(updatedAt);
    }

    public void Credit(Money money, DateTimeOffset updatedAt)
    {
        EnsureCanPost(money);
        AvailableBalance += money.Amount;
        MarkUpdated(updatedAt);
    }

    private void EnsureCanPost(Money money)
    {
        if (Status != WalletStatus.Active)
        {
            throw new DomainException("Wallet is not active.");
        }

        if (!string.Equals(Currency, money.Currency.Code, StringComparison.Ordinal))
        {
            throw new DomainException("Wallet currency does not match the transfer currency.");
        }
    }

    private void MarkUpdated(DateTimeOffset updatedAt)
    {
        UpdatedAt = updatedAt.ToUniversalTime();
        Version++;
    }
}
