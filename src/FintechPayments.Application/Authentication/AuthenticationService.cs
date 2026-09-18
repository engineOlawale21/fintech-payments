using FintechPayments.Application.Abstractions.Authentication;
using FintechPayments.Application.Abstractions.Persistence;
using FintechPayments.Application.Abstractions.Time;
using FintechPayments.Domain.Common;
using FintechPayments.Domain.Users;

namespace FintechPayments.Application.Authentication;

public sealed class AuthenticationService(
    IUserRepository users,
    IPasswordService passwords,
    ITokenService tokens,
    IClock clock)
{
    public async Task<AuthenticationResult> RegisterAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        ValidatePassword(password);
        User user = User.Create(email, clock.UtcNow);
        user.SetPasswordHash(passwords.Hash(user, password));

        bool created = await users.CreateIfEmailAvailableAsync(user, cancellationToken);
        if (!created)
        {
            return AuthenticationResult.Failure("email_already_registered");
        }

        return AuthenticationResult.Success(user, tokens.Create(user));
    }

    public async Task<AuthenticationResult> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        string normalizedEmail = email.Trim().ToUpperInvariant();
        User? user = await users.FindByNormalizedEmailAsync(normalizedEmail, cancellationToken);

        if (user is null || user.Status != UserStatus.Active || !passwords.Verify(user, password))
        {
            return AuthenticationResult.Failure("invalid_credentials");
        }

        return AuthenticationResult.Success(user, tokens.Create(user));
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 12)
        {
            throw new DomainException("Password must contain at least 12 characters.");
        }
    }
}

public sealed record AuthenticationResult(
    bool Succeeded,
    string? ErrorCode,
    Guid? UserId,
    string? Email,
    string? Role,
    AuthenticationToken? Token)
{
    public static AuthenticationResult Success(User user, AuthenticationToken token) =>
        new(true, null, user.Id, user.Email, user.Role.ToString(), token);

    public static AuthenticationResult Failure(string errorCode) =>
        new(false, errorCode, null, null, null, null);
}
