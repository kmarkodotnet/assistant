using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace FamilyOs.Api.IntegrationTests.Search;

[Trait("Category", "Integration")]
[Collection("IntegrationTests")]
public sealed class SearchExampleTests(FamilyOsTestFixture fixture)
{
    private async Task LoginAsync()
    {
        var resp = await fixture.Client.PostAsJsonAsync("/api/v1/auth/login/google", new { idToken = "admin-token" });
        resp.EnsureSuccessStatusCode();
    }

    private static readonly string[] DocumentsEntityType = ["documents"];

    [Fact]
    public async Task FilterSearch_MutasdOsszesDocumentumot_Returns200()
    {
        await LoginAsync();

        var resp = await fixture.Client.PostAsJsonAsync("/api/v1/search", new
        {
            query = "Mutasd az összes dokumentumot",
            mode = "Filter",
            entityTypes = DocumentsEntityType,
        });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await resp.Content.ReadFromJsonAsync<SearchResponseDto>();
        result.Should().NotBeNull();
        result!.ModeUsed.Should().Be("Filter");
    }

    [Fact]
    public async Task AutoSearch_MikorJarLeABiztositas_ClassifiesAndReturns200()
    {
        await LoginAsync();

        var resp = await fixture.Client.PostAsJsonAsync("/api/v1/search", new
        {
            query = "Mikor jár le a biztosítás?",
            mode = "Auto",
        });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await resp.Content.ReadFromJsonAsync<SearchResponseDto>();
        result.Should().NotBeNull();
        // Should have classified as Text/Lookup or Semantic
        result!.ModeUsed.Should().BeOneOf("Text", "Semantic", "Filter");
    }

    [Fact]
    public async Task SemanticSearch_WithQuery_Returns200()
    {
        await LoginAsync();

        var resp = await fixture.Client.PostAsJsonAsync("/api/v1/search", new
        {
            query = "biztosítási szerződés",
            mode = "Semantic",
        });

        // Even with no embeddings, semantic search should return empty results (not error)
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await resp.Content.ReadFromJsonAsync<SearchResponseDto>();
        result.Should().NotBeNull();
        result!.ModeUsed.Should().Be("Semantic");
    }
}

public sealed class SearchResponseDto
{
    public List<object> Hits { get; set; } = [];
    public int TotalCount { get; set; }
    public string ModeUsed { get; set; } = string.Empty;
    public string? Answer { get; set; }
}
