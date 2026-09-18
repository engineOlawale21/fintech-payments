using FintechPayments.Domain.Common;

namespace FintechPayments.UnitTests;

public sealed class MoneyTests
{
    [Theory]
    [InlineData("usd", "USD")]
    [InlineData(" NGN ", "NGN")]
    [InlineData("GBP", "GBP")]
    public void CurrencyNormalizesSupportedCodes(string input, string expected)
    {
        Assert.Equal(expected, Currency.FromCode(input).Code);
    }

    [Fact]
    public void CurrencyRejectsUnsupportedCode()
    {
        Assert.Throws<DomainException>(() => Currency.FromCode("EUR"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MoneyRejectsNonPositiveAmounts(decimal amount)
    {
        Assert.Throws<DomainException>(() => Money.Create(amount, Currency.FromCode("USD")));
    }

    [Fact]
    public void MoneyRejectsExcessCurrencyPrecision()
    {
        Assert.Throws<DomainException>(() => Money.Create(10.001m, Currency.FromCode("USD")));
    }

    [Fact]
    public void MoneyCreatesValidAmount()
    {
        Money money = Money.Create(1250.75m, Currency.FromCode("NGN"));

        Assert.Equal(1250.75m, money.Amount);
        Assert.Equal("NGN", money.Currency.Code);
    }
}
