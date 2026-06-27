using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace FamilyOs.Api.IntegrationTests.Notes;

[Trait("Category", "Integration")]
public sealed class NoteCrudTests(FamilyOsTestFixture fixture) : IClassFixture<FamilyOsTestFixture>
{
    private async Task LoginAsync()
    {
        var loginResp = await fixture.Client.PostAsJsonAsync(
            "/api/v1/auth/login/google",
            new { idToken = "admin-token" });
        loginResp.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CreateNote_ReturnsCreated_AndEnqueuesEmbedJob()
    {
        // Arrange
        await LoginAsync();

        var body = new
        {
            title = "Test Note",
            body = "## Hello\n\nThis is a **test** note.",
            isPrivate = false,
        };

        // Act
        var resp = await fixture.Client.PostAsJsonAsync("/api/v1/notes", body);

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await resp.Content.ReadFromJsonAsync<NoteResponse>();
        result.Should().NotBeNull();
        result!.Title.Should().Be("Test Note");
        result.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetNote_AfterCreate_ReturnsNote()
    {
        // Arrange
        await LoginAsync();

        var createResp = await fixture.Client.PostAsJsonAsync("/api/v1/notes", new
        {
            title = "Get Test Note",
            body = "Body content.",
            isPrivate = false,
        });
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<NoteResponse>();

        // Act
        var getResp = await fixture.Client.GetAsync($"/api/v1/notes/{created!.Id}");

        // Assert
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var note = await getResp.Content.ReadFromJsonAsync<NoteResponse>();
        note!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task ListNotes_FiltersPrivate_ForOtherUser()
    {
        // Arrange — create a private note as admin
        await LoginAsync();

        await fixture.Client.PostAsJsonAsync("/api/v1/notes", new
        {
            title = "Private Note",
            body = "Secret content.",
            isPrivate = true,
        });

        // Act — list notes (same admin user, should see their own private notes)
        var listResp = await fixture.Client.GetAsync("/api/v1/notes");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await listResp.Content.ReadFromJsonAsync<List<NoteListItem>>();
        list.Should().NotBeNull();
        // The admin user can see their own private note
        list!.Should().Contain(n => n.Title == "Private Note");
    }

    [Fact]
    public async Task DeleteNote_Returns204()
    {
        // Arrange
        await LoginAsync();

        var createResp = await fixture.Client.PostAsJsonAsync("/api/v1/notes", new
        {
            title = "Delete Me",
            body = "Will be deleted.",
        });
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<NoteResponse>();

        // Act
        var deleteResp = await fixture.Client.DeleteAsync($"/api/v1/notes/{created!.Id}");

        // Assert
        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's gone
        var getResp = await fixture.Client.GetAsync($"/api/v1/notes/{created.Id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetRendered_ReturnsHtml()
    {
        // Arrange
        await LoginAsync();

        var createResp = await fixture.Client.PostAsJsonAsync("/api/v1/notes", new
        {
            title = "Markdown Note",
            body = "# Hello\n\nThis is **markdown**.",
        });
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<NoteResponse>();

        // Act
        var renderedResp = await fixture.Client.GetAsync($"/api/v1/notes/{created!.Id}/rendered");

        // Assert
        renderedResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await renderedResp.Content.ReadAsStringAsync();
        html.Should().Contain("<h1>");
        html.Should().Contain("<strong>");
    }

    private sealed class NoteResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsPrivate { get; set; }
    }

    private sealed class NoteListItem
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsPrivate { get; set; }
    }
}
