using FamilyOs.Domain.Entities;
using FluentAssertions;

namespace FamilyOs.Domain.Tests.Ai;

public sealed class TagTopicEntityTests
{
    [Fact]
    public void Tag_Create_NormalizesNameToLower()
    {
        var tag = Tag.Create("  AXA Insurance  ");

        tag.Name.Should().Be("axa insurance");
        tag.UsageCount.Should().Be(1);
        tag.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Tag_IncrementUsage_IncreasesCount()
    {
        var tag = Tag.Create("test");

        tag.IncrementUsage();

        tag.UsageCount.Should().Be(2);
    }

    [Fact]
    public void Topic_Create_NormalizesSlugToLower()
    {
        var topic = Topic.Create("Insurance", "INSURANCE");

        topic.Slug.Should().Be("insurance");
        topic.Name.Should().Be("Insurance");
        topic.ParentId.Should().BeNull();
    }

    [Fact]
    public void Topic_Create_WithParent_SetsParentId()
    {
        var parentId = Guid.NewGuid();
        var topic = Topic.Create("Car Insurance", "car-insurance", parentId);

        topic.ParentId.Should().Be(parentId);
    }

    [Fact]
    public void DocumentChunk_Create_SetsCorrectFields()
    {
        var docId = Guid.NewGuid();
        var chunk = Domain.Entities.DocumentChunk.Create(docId, 0, "Chunk content");

        chunk.Id.Should().NotBe(Guid.Empty);
        chunk.DocumentId.Should().Be(docId);
        chunk.ChunkIndex.Should().Be(0);
        chunk.Content.Should().Be("Chunk content");
        chunk.Embedding.Should().BeNull();
        chunk.EmbeddingModel.Should().BeEmpty();
    }

    [Fact]
    public void DocumentChunk_SetEmbedding_SetsEmbeddingAndModel()
    {
        var chunk = Domain.Entities.DocumentChunk.Create(Guid.NewGuid(), 0, "Test");
        var vector = new Pgvector.Vector(new float[768]);

        chunk.SetEmbedding(vector, "nomic-embed-text:v1.5");

        chunk.EmbeddingModel.Should().Be("nomic-embed-text:v1.5");
        chunk.Embedding.Should().NotBeNull();
    }
}
