using FintechPayments.Domain.Common;
using FintechPayments.Domain.Users;

namespace FintechPayments.UnitTests;

public sealed class UserTests
{
    [Fact]
    public void CreateNormalizesEmailAndDefaultsToCustomer()
    {
        User user = User.Create(" Person@Example.com ", DateTimeOffset.UtcNow);

        Assert.Equal("Person@Example.com", user.Email);
        Assert.Equal("PERSON@EXAMPLE.COM", user.NormalizedEmail);
        Assert.Equal(UserRole.Customer, user.Role);
        Assert.Equal(UserStatus.Active, user.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void CreateRejectsInvalidEmail(string email)
    {
        Assert.Throws<DomainException>(() => User.Create(email, DateTimeOffset.UtcNow));
    }
}
