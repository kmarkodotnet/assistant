using FamilyOs.Application.Abstractions.Ai;
using Microsoft.Extensions.Caching.Memory;

namespace FamilyOs.Infrastructure.Ai.Caching;

public sealed class QueryEmbeddingCache : IQueryEmbeddingCache
{
    private readonly IMemoryCache _cache;
    private readonly IEmbedder _embedder;

    public QueryEmbeddingCache(IMemoryCache cache, IEmbedder embedder)
    {
        _cache = cache;
        _embedder = embedder;
    }

    public async Task<float[]> GetOrComputeAsync(string query, CancellationToken ct = default)
    {
        var key = $"qemb:{query.ToLowerInvariant().Trim()}";
        if (_cache.TryGetValue(key, out float[]? cached))
            return cached!;

        var embedding = await _embedder.EmbedAsync(query, ct);
        _cache.Set(key, embedding, TimeSpan.FromHours(1));
        return embedding;
    }
}
