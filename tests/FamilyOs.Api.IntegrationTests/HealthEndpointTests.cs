using Microsoft.AspNetCore.Mvc.Testing;

namespace FamilyOs.Api.IntegrationTests;

public sealed class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealthzLiveReturns200()
    {
        var response = await _client.GetAsync("/healthz/live");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}
