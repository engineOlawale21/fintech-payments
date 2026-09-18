namespace FintechPayments.Domain.Common;

public readonly record struct Currency
{
    private static readonly HashSet<string> SupportedCodes =
        new(StringComparer.Ordinal) { "GBP", "NGN", "USD" };

    private Currency(string code)
    {
        Code = code;
    }

    public string Code { get; }

    public int MinorUnitDigits => Code switch
    {
        "GBP" or "NGN" or "USD" => 2,
        _ => throw new DomainException($"Currency '{Code}' is not supported."),
    };

    public static Currency FromCode(string code)
    {
        string normalized = code?.Trim().ToUpperInvariant()
            ?? throw new DomainException("Currency is required.");

        if (!SupportedCodes.Contains(normalized))
        {
            throw new DomainException($"Currency '{normalized}' is not supported.");
        }

        return new Currency(normalized);
    }

    public override string ToString() => Code;
}
