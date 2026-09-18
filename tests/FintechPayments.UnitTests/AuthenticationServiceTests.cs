using FintechPayments.Application.Abstractions.Authentication;
using FintechPayments.Application.Abstractions.Persistence;
using FintechPayments.Application.Abstractions.Time;
using FintechPayments.Application.Authentication;
using FintechPayments.Domain.Common;
using FintechPayments.Domain.Users;

namespace FintechPayments.UnitTests;

public sealed class AuthenticationServiceTests
{
    [Fact]
    public async Task RegisterCreatesCustomerAndReturnsToken()
    {
        FakeUserRepository repository = new();
        AuthenticationService service = CreateService(repository);

        AuthenticationResult result = await service.RegisterAsync(
            "person@example.com", "a-secure-password", CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(UserRole.Customer.ToString(), result.Role);
        Assert.Equal("test-token", result.Token?.Value);
        Assert.NotNull(repository.User);
        Assert.NotEqual("a-secure-password", repository.User.PasswordHash);
    }

    [Fact]
    public async Task RegisterRejectsDuplicateEmail()
    {
        FakeUserRepository repository = new() { AllowCreate = false };
        AuthenticationService service = CreateService(repository);

        AuthenticationResult result = await service.RegisterAsync(
            "person@example.com", "a-secure-password", CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("email_already_registered", result.ErrorCode);
    }

    [Fact]
    public async Task RegisterRejectsShortPassword()
    {
        AuthenticationService service = CreateService(new FakeUserRepository());

        await Assert.ThrowsAsync<DomainException>(() => service.RegisterAsync(
            "person@example.com", "too-short", CancellationToken.None));
    }

    private static AuthenticationService CreateService(FakeUserRepository repository) =>
        new(repository, new FakePasswordService(), new FakeTokenService(), new FakeClock());

    private sealed class FakeUserRepository : IUserRepository
    {
        public bool AllowCreate { get; init; } = true;
        public User? User { get; private set; }

        public Task<User?> FindByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
            Task.FromResult(User);

        public Task<bool> CreateIfEmailAvailableAsync(User user, CancellationToken cancellationToken)
        {
            User = user;
            return Task.FromResult(AllowCreate);
        }
    }

    private sealed class FakePasswordService : IPasswordService
    {
        public string Hash(User user, string password) => $"hashed::{password}";
        public bool Verify(User user, string password) => user.PasswordHash == $"hashed::{password}";
    }

    private sealed class FakeTokenService : ITokenService
    {
        public AuthenticationToken Create(User user) =>
            new("test-token", new DateTimeOffset(2026, 9, 12, 0, 15, 0, TimeSpan.Zero));
    }

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 9, 12, 0, 0, 0, TimeSpan.Zero);
    }
}
