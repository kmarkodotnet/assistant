using Pgvector;

namespace FamilyOs.Domain.Entities;

public sealed class DocumentChunk
{
    private DocumentChunk() { }

    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public int ChunkIndex { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public Vector? Embedding { get; private set; }
    public string EmbeddingModel { get; private set; } = string.Empty;
    public DateTime CreatedUtc { get; private set; }
    public Document? Document { get; private set; }

    public static DocumentChunk Create(Guid documentId, int chunkIndex, string content)
        => new()
        {
            Id = Guid.CreateVersion7(),
            DocumentId = documentId,
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
