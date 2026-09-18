using System.ComponentModel.DataAnnotations;

namespace FintechPayments.Infrastructure.Configuration;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    [Required]
    public string ConnectionString { get; init; } = string.Empty;
}
