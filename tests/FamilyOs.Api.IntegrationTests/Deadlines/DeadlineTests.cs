using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace FamilyOs.Api.IntegrationTests.Deadlines;

[Trait("Category", "Integration")]
[Collection("IntegrationTests")]
public sealed class DeadlineTests(FamilyOsTestFixture fixture)
{
    private async Task LoginAsync()
    {
        var resp = await fixture.Client.PostAsJsonAsync("/api/v1/auth/login/google", new { idToken = "admin-token" });
        resp.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CreateDeadline_AndApprove_SetsOriginAiApproved()
    {
        await LoginAsync();

        var dueDate = DateTime.UtcNow.AddDays(30);
        var createResp = await fixture.Client.PostAsJsonAsync("/api/v1/deadlines", new
        {
            title = "Insurance renewal",
            dueDateUtc = dueDate,
            category = "Insurance",
        });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResp.Content.ReadFromJsonAsync<DeadlineResponse>();
        created.Should().NotBeNull();
        created!.Status.Should().Be("Upcoming");

        // Approve
        var approveResp = await fixture.Client.PostAsync($"/api/v1/deadlines/{created.Id}/approve", null);
        approveResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify
        var getResp = await fixture.Client.GetAsync($"/api/v1/deadlines/{created.Id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var deadline = await getResp.Content.ReadFromJsonAsync<DeadlineResponse>();
        deadline!.Origin.Should().Be("AiApproved");
    }

    [Fact]
    public async Task DismissDeadline_SetsStatusDismissed()
    {
        await LoginAsync();

        var createResp = await fixture.Client.PostAsJsonAsync("/api/v1/deadlines", new
        {
            title = "Dismiss test deadline",
            dueDateUtc = DateTime.UtcNow.AddDays(14),
            category = "Other",
        });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResp.Content.ReadFromJsonAsync<DeadlineResponse>();

        // Dismiss
        var dismissResp = await fixture.Client.PostAsync($"/api/v1/deadlines/{created!.Id}/dismiss", null);
        dismissResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify
        var getResp = await fixture.Client.GetAsync($"/api/v1/deadlines/{created.Id}");
        var deadline = await getResp.Content.ReadFromJsonAsync<DeadlineResponse>();
        deadline!.Status.Should().Be("Dismissed");
    }
}

public sealed class DeadlineResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}
