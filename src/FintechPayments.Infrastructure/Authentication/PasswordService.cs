using FintechPayments.Application.Abstractions.Authentication;
using FintechPayments.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace FintechPayments.Infrastructure.Authentication;

internal sealed class PasswordService(IPasswordHasher<User> hasher) : IPasswordService
{
    public string Hash(User user, string password) => hasher.HashPassword(user, password);

    public bool Verify(User user, string password) =>
        hasher.VerifyHashedPassword(user, user.PasswordHash, password) != PasswordVerificationResult.Failed;
}
