namespace FamilyOs.Application.Abstractions.Ai;

public record SemanticHit(Guid DocumentId, Guid ChunkId, string Snippet, double Score);

public interface ISemanticSearchService
{
    Task<IReadOnlyList<SemanticHit>> SearchAsync(
        float[] queryEmbedding,
        int limit,
        Guid? userId,
        CancellationToken ct = default);
}
