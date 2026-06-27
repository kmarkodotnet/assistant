using Microsoft.AspNetCore.Mvc.Testing;

namespace FamilyOs.Api.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Default", "Host=localhost;Port=5432;Database=test;Username=test;Password=test");
            builder.UseSetting("Auth:GoogleClientId", "test");
            builder.UseSetting("Auth:BootstrapAdmin", "admin@test.com");
        }).CreateClient();
    }

    [Fact]
    public async Task GetHealthzLive_Returns200()
    {
        var response = await _client.GetAsync("/healthz/live");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}
