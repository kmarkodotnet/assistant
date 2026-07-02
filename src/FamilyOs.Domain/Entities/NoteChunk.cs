using Pgvector;

namespace FamilyOs.Domain.Entities;

public sealed class NoteChunk
{
    private NoteChunk() { }

    public Guid Id { get; private set; }
    public Guid NoteId { get; private set; }
    public int ChunkIndex { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public Vector? Embedding { get; private set; }
    public string EmbeddingModel { get; private set; } = string.Empty;
    public DateTime CreatedUtc { get; private set; }

    public Note? Note { get; private set; }

    public static NoteChunk Create(Guid noteId, int chunkIndex, string content)
        => new()
        {
            Id = Guid.CreateVersion7(),
            NoteId = noteId,
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
