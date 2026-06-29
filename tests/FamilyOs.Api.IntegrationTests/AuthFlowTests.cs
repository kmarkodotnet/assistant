using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace FamilyOs.Api.IntegrationTests;

[Trait("Category", "Integration")]
[Collection("IntegrationTests")]
public sealed class AuthFlowTests(FamilyOsTestFixture fixture)
{
    [Fact]
    public async Task Login_WithValidAdminToken_Returns200AndCookie()
    {
        var resp = await fixture.Client.PostAsJsonAsync(
            "/api/v1/auth/login/google",
            new { idToken = "admin-token" });

        if (resp.StatusCode != HttpStatusCode.OK)
        {
            var body = await resp.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Login failed with {resp.StatusCode}: {body}");
        }

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Headers.Should().ContainKey("Set-Cookie");
    }

    [Fact]
    public async Task GetMe_WithoutCookie_Returns401()
    {
        var resp = await fixture.Client.GetAsync("/api/v1/auth/me");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
