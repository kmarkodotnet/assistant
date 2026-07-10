namespace FamilyOs.Application.Abstractions.Ai;

public record SemanticHit(string EntityType, Guid EntityId, Guid ChunkId, string Snippet, double Score);

public interface ISemanticSearchService
{
    Task<IReadOnlyList<SemanticHit>> SearchAsync(
        float[] queryEmbedding,
        int limit,
        Guid? userId,
        CancellationToken ct = default,
        double minSimilarity = 0.0);
}
