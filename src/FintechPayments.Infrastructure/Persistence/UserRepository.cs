using FintechPayments.Application.Abstractions.Persistence;
using FintechPayments.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FintechPayments.Infrastructure.Persistence;

internal sealed class UserRepository(PaymentsDbContext context) : IUserRepository
{
    public Task<User?> FindByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken) =>
        context.Users.SingleOrDefaultAsync(
            user => user.NormalizedEmail == normalizedEmail,
            cancellationToken);

    public async Task<bool> CreateIfEmailAvailableAsync(User user, CancellationToken cancellationToken)
    {
        context.Users.Add(user);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            context.Entry(user).State = EntityState.Detached;
            return false;
        }
    }
}
