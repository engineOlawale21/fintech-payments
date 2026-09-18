namespace FintechPayments.Api.Diagnostics;

public static class CorrelationId
{
    public const string HeaderName = "X-Correlation-ID";
    public const string ItemKey = "CorrelationId";

    public static string Normalize(string? candidate) =>
        string.IsNullOrWhiteSpace(candidate) || candidate.Length > 128
            ? Guid.NewGuid().ToString("N")
            : candidate.Trim();
}
