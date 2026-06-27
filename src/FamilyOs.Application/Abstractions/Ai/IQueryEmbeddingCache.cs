namespace FamilyOs.Application.Abstractions.Ai;

public interface IQueryEmbeddingCache
{
    Task<float[]> GetOrComputeAsync(string query, CancellationToken ct = default);
}
