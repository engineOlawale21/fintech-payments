using FintechPayments.Domain.Users;

namespace FintechPayments.Application.Abstractions.Authentication;

public interface ITokenService
{
    AuthenticationToken Create(User user);
}

public sealed record AuthenticationToken(string Value, DateTimeOffset ExpiresAt);
