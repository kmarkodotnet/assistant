using FamilyOs.Domain.Services;
using FluentAssertions;

namespace FamilyOs.Infrastructure.Tests.Pipeline;

public sealed class EmbeddingChunkerTests
{
    [Fact]
    public void Chunk_ShortText_ReturnsSingleChunk()
    {
        // Arrange
        var text = "Ez egy rövid szöveg.";

        // Act
        var chunks = EmbeddingChunker.Chunk(text);

        // Assert
        chunks.Should().HaveCount(1);
        chunks[0].Should().Contain("rövid szöveg");
    }

    [Fact]
    public void Chunk_EmptyText_ReturnsSingleEmptyChunk()
    {
        // Arrange
        var text = "   ";

        // Act
        var chunks = EmbeddingChunker.Chunk(text);

        // Assert
        chunks.Should().HaveCount(1);
    }

    [Fact]
    public void Chunk_LongText_SplitsIntoMultipleChunks()
    {
        // Arrange: create a text > 3200 chars using paragraphs
        var paragraphs = Enumerable.Range(1, 30)
            .Select(i => new string('A', 150) + $" Paragraph {i}")
            .ToList();
        var text = string.Join("\n\n", paragraphs);

        // Act
        var chunks = EmbeddingChunker.Chunk(text);

        // Assert
        chunks.Should().HaveCountGreaterThan(1);
        foreach (var chunk in chunks)
        {
            chunk.Length.Should().BeLessOrEqualTo(4000); // with overlap tolerance
        }
    }

    [Fact]
    public void Chunk_MultipleSmallParagraphs_MergesUntilMaxSize()
    {
        // Arrange: paragraphs that individually are small
        var paragraphs = Enumerable.Range(1, 5).Select(i => $"Bekezdés {i}").ToList();
        var text = string.Join("\n\n", paragraphs);

        // Act
        var chunks = EmbeddingChunker.Chunk(text);

        // Assert: all small paragraphs merge into one chunk
        chunks.Should().HaveCount(1);
    }

    [Fact]
    public void Chunk_LargeText_AddsOverlapBetweenChunks()
    {
        // Arrange: two large paragraphs
        var para1 = new string('X', 2000) + " END_PARA1";
        var para2 = new string('Y', 2000) + " END_PARA2";
        var text = para1 + "\n\n" + para2;

        // Act
        var chunks = EmbeddingChunker.Chunk(text);

        // Assert: second chunk contains overlap from first
        chunks.Should().HaveCountGreaterThanOrEqualTo(2);
        // The second chunk should contain some of the first paragraph's tail (overlap)
        chunks[1].Should().Contain("END_PARA1");
    }
}
