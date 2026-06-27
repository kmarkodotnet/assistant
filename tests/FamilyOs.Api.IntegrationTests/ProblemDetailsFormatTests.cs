using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace FamilyOs.Api.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class ProblemDetailsFormatTests(FamilyOsTestFixture fixture) : IClassFixture<FamilyOsTestFixture>
{
    [Fact]
    public async Task NotFound_ReturnsProblemDetailsWithTraceId()
    {
        // First login to get auth cookie
        var loginResp = await fixture.Client.PostAsJsonAsync(
            "/api/v1/auth/login/google",
            new { idToken = "admin-token" });
        loginResp.EnsureSuccessStatusCode();

        // Use the auth cookie from login
        var cookieHeader = loginResp.Headers.GetValues("Set-Cookie").FirstOrDefault();

        var request = new HttpRequestMessage(HttpMethod.Get,
            "/api/v1/family-members/00000000-0000-0000-0000-000000000000");
        if (cookieHeader is not null)
            request.Headers.Add("Cookie", cookieHeader.Split(';')[0]);

        var resp = await fixture.Client.SendAsync(request);

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        resp.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("detail").GetString().Should().NotBeNullOrEmpty();
    }
}
