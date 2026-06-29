namespace FamilyOs.Api.IntegrationTests;

[Trait("Category", "Integration")]
[Collection("IntegrationTests")]
public sealed class HealthEndpointTests(FamilyOsTestFixture fixture)
{
    [Fact]
    public async Task GetHealthzLive_Returns200()
    {
        var response = await fixture.Client.GetAsync("/healthz/live");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}
