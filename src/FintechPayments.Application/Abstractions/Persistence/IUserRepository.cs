using FintechPayments.Domain.Users;

namespace FintechPayments.Application.Abstractions.Persistence;

public interface IUserRepository
{
    Task<User?> FindByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
    Task<bool> CreateIfEmailAvailableAsync(User user, CancellationToken cancellationToken);
}
