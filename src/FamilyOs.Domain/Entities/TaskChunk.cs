using Pgvector;

namespace FamilyOs.Domain.Entities;

public sealed class TaskChunk
{
    private TaskChunk() { }

    public Guid Id { get; private set; }
    public Guid TaskId { get; private set; }
    public int ChunkIndex { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public Vector? Embedding { get; private set; }
    public string EmbeddingModel { get; private set; } = string.Empty;
    public DateTime CreatedUtc { get; private set; }
    public FamilyTask? Task { get; private set; }

    public static TaskChunk Create(Guid taskId, int chunkIndex, string content)
        => new()
        {
            Id = Guid.CreateVersion7(),
            TaskId = taskId,
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
