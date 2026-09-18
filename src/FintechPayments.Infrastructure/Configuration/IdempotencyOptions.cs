using System.ComponentModel.DataAnnotations;

namespace FintechPayments.Infrastructure.Configuration;

public sealed class IdempotencyOptions
{
    public const string SectionName = "Idempotency";

    [Range(1, 168)]
    public int RetentionHours { get; init; } = 24;
}
