using FamilyOs.Domain.Entities;
using FluentAssertions;

namespace FamilyOs.Domain.Tests.Ai;

public sealed class DocumentSummaryTests
{
    [Fact]
    public void Create_SetsIsCurrentTrue()
    {
        var docId = Guid.NewGuid();

        var summary = DocumentSummary.Create(docId, "Test content", "llama3.2:3b", "v1");

        summary.Id.Should().NotBe(Guid.Empty);
        summary.DocumentId.Should().Be(docId);
        summary.Content.Should().Be("Test content");
        summary.ModelName.Should().Be("llama3.2:3b");
        summary.PromptVersion.Should().Be("v1");
        summary.IsCurrent.Should().BeTrue();
        summary.CreatedUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Supersede_SetsIsCurrentFalse()
    {
        var summary = DocumentSummary.Create(Guid.NewGuid(), "Content", "model", "v1");

        summary.Supersede();

        summary.IsCurrent.Should().BeFalse();
    }

    [Fact]
    public void Create_MultipleForSameDocument_OnlyLatestShouldBeCurrent()
    {
        var docId = Guid.NewGuid();

        var first = DocumentSummary.Create(docId, "First summary", "model", "v1");
        var second = DocumentSummary.Create(docId, "Second summary", "model", "v1");

        // Simulate the upsert: supersede the first
        first.Supersede();

        first.IsCurrent.Should().BeFalse();
        second.IsCurrent.Should().BeTrue();
    }
}
