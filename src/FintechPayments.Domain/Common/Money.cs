namespace FintechPayments.Domain.Common;

public readonly record struct Money
{
    private Money(decimal amount, Currency currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public Currency Currency { get; }

    public static Money Create(decimal amount, Currency currency)
    {
        if (amount <= 0)
        {
            throw new DomainException("Money amount must be greater than zero.");
        }

        if (decimal.Round(amount, currency.MinorUnitDigits) != amount)
        {
            throw new DomainException(
                $"Amount has more than {currency.MinorUnitDigits} decimal places for {currency.Code}.");
        }

        return new Money(amount, currency);
    }
}
