namespace FamilyOs.Domain.Tests;

public sealed class SmokeTests
{
    [Fact]
    public void DomainHasNoExternalDependenciesPasses()
    {
        // Trivial smoke: domain project compiles and this test runs.
        Assert.True(true);
    }
}
