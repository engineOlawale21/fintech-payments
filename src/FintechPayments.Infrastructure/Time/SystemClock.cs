using FintechPayments.Application.Abstractions.Time;

namespace FintechPayments.Infrastructure.Time;

internal sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
