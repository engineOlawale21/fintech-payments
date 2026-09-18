using FintechPayments.Domain;

namespace FintechPayments.UnitTests;

public sealed class AssemblyBoundaryTests
{
    [Fact]
    public void DomainAssemblyHasExpectedName()
    {
        Assert.Equal("FintechPayments.Domain", typeof(AssemblyReference).Assembly.GetName().Name);
    }
}
