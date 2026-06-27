using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace FamilyOs.Api.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class AuthFlowTests(FamilyOsTestFixture fixture) : IClassFixture<FamilyOsTestFixture>
{
    [Fact]
    public async Task Login_WithValidAdminToken_Returns200AndCookie()
    {
        var resp = await fixture.Client.PostAsJsonAsync(
            "/api/v1/auth/login/google",
            new { idToken = "admin-token" });

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
