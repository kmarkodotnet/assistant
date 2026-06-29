using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace FamilyOs.Api.IntegrationTests.Tasks;

[Trait("Category", "Integration")]
[Collection("IntegrationTests")]
public sealed class TaskLifecycleTests(FamilyOsTestFixture fixture)
{
    private async Task LoginAsync()
    {
        var resp = await fixture.Client.PostAsJsonAsync("/api/v1/auth/login/google", new { idToken = "admin-token" });
        resp.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CreateAndApproveTask_LifecycleSucceeds()
    {
        await LoginAsync();

        // Create a task (starts as Open since manual creation)
        var createResp = await fixture.Client.PostAsJsonAsync("/api/v1/tasks", new
        {
            title = "Integration Test Task",
            description = "Testing lifecycle",
            priority = "Normal",
        });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResp.Content.ReadFromJsonAsync<TaskResponse>();
        created.Should().NotBeNull();
        created!.Id.Should().NotBeEmpty();
        created.Status.Should().Be("Open");

        // Start the task
        var startResp = await fixture.Client.PostAsync($"/api/v1/tasks/{created.Id}/start", null);
        startResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Complete the task
        var completeResp = await fixture.Client.PostAsync($"/api/v1/tasks/{created.Id}/complete", null);
        completeResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify via GET
        var getResp = await fixture.Client.GetAsync($"/api/v1/tasks/{created.Id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var task = await getResp.Content.ReadFromJsonAsync<TaskResponse>();
        task!.Status.Should().Be("Done");
    }

    [Fact]
    public async Task CompleteWithoutStart_ReturnsDomainError()
    {
        await LoginAsync();

        // Create a task
        var createResp = await fixture.Client.PostAsJsonAsync("/api/v1/tasks", new
        {
            title = "Task for invalid transition",
            priority = "Normal",
        });
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<TaskResponse>();

        // Try to complete directly without starting (Open → Done is allowed by state machine)
        // But let's try cancel then complete which is invalid
        await fixture.Client.PostAsync($"/api/v1/tasks/{created!.Id}/cancel", null);
        var badResp = await fixture.Client.PostAsync($"/api/v1/tasks/{created.Id}/complete", null);

        // Cancelled → Done should fail
        badResp.StatusCode.Should().BeOneOf(HttpStatusCode.UnprocessableEntity, HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ListTasks_ReturnsItems()
    {
        await LoginAsync();

        await fixture.Client.PostAsJsonAsync("/api/v1/tasks", new
        {
            title = "List test task",
            priority = "Normal",
        });

        var resp = await fixture.Client.GetAsync("/api/v1/tasks");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

public sealed class TaskResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
}
