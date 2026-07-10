using Pgvector;

namespace FamilyOs.Domain.Entities;

public sealed class DeadlineChunk
{
    private DeadlineChunk() { }

    public Guid Id { get; private set; }
    public Guid DeadlineId { get; private set; }
    public int ChunkIndex { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public Vector? Embedding { get; private set; }
    public string EmbeddingModel { get; private set; } = string.Empty;
    public DateTime CreatedUtc { get; private set; }
    public Deadline? Deadline { get; private set; }

    public static DeadlineChunk Create(Guid deadlineId, int chunkIndex, string content)
        => new()
        {
            Id = Guid.CreateVersion7(),
            DeadlineId = deadlineId,
            ChunkIndex = chunkIndex,
            Content = content,
            EmbeddingModel = string.Empty,
            CreatedUtc = DateTime.UtcNow,
        };

    public void SetEmbedding(Vector embedding, string modelName)
    {
        Embedding = embedding;
        EmbeddingModel = modelName;
    }
}
