using FintechPayments.Domain.Users;

namespace FintechPayments.Application.Abstractions.Authentication;

public interface IPasswordService
{
    string Hash(User user, string password);
    bool Verify(User user, string password);
}
