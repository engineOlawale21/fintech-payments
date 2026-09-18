using System.ComponentModel.DataAnnotations;

namespace FintechPayments.Infrastructure.Configuration;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    [Required]
    public string ConnectionString { get; init; } = string.Empty;
}
