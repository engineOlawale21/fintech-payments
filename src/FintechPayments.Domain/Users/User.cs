using FintechPayments.Domain.Common;

namespace FintechPayments.Domain.Users;

public sealed class User
{
    private User()
    {
    }

    private User(Guid id, string email, string normalizedEmail, DateTimeOffset createdAt)
    {
        Id = id;
        Email = email;
        NormalizedEmail = normalizedEmail;
        Role = UserRole.Customer;
        Status = UserStatus.Active;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string NormalizedEmail { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public UserStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static User Create(string email, DateTimeOffset createdAt)
    {
        string trimmed = email?.Trim() ?? throw new DomainException("Email is required.");
        if (trimmed.Length is < 3 or > 320 || !trimmed.Contains('@', StringComparison.Ordinal))
        {
            throw new DomainException("Email is invalid.");
        }

        return new User(Guid.NewGuid(), trimmed, trimmed.ToUpperInvariant(), createdAt.ToUniversalTime());
    }

    public void SetPasswordHash(string passwordHash)
    {
        PasswordHash = string.IsNullOrWhiteSpace(passwordHash)
            ? throw new DomainException("Password hash is required.")
            : passwordHash;
    }
}
