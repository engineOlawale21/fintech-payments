using System.ComponentModel.DataAnnotations;

namespace FintechPayments.Infrastructure.Configuration;

public sealed class WebhookOptions
{
    public const string SectionName = "Webhooks:MockProvider";

    [Required, MinLength(32)]
    public string Secret { get; init; } = string.Empty;

    [Range(30, 900)]
    public int ReplayToleranceSeconds { get; init; } = 300;
}
