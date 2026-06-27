using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Search.Dtos;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Search.Handlers;

public sealed class SemanticSearchHandler
{
    private readonly ISemanticSearchService _semanticSearch;
    private readonly IQueryEmbeddingCache _embeddingCache;
    private readonly IFamilyOsDbContext _db;

    public SemanticSearchHandler(
        ISemanticSearchService semanticSearch,
        IQueryEmbeddingCache embeddingCache,
        IFamilyOsDbContext db)
    {
        _semanticSearch = semanticSearch;
        _embeddingCache = embeddingCache;
        _db = db;
    }

    public async Task<SearchResponse> SearchAsync(
        SearchRequest req,
        Guid? userId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Query))
            return new SearchResponse { ModeUsed = SearchMode.Semantic };

        var embedding = await _embeddingCache.GetOrComputeAsync(req.Query, ct);
        var semanticHits = await _semanticSearch.SearchAsync(embedding, req.PageSize * 2, userId, ct);

        // Group by document, take best score per document
        var byDoc = semanticHits
            .GroupBy(h => h.DocumentId)
            .Select(g => g.OrderByDescending(h => h.Score).First())
            .OrderByDescending(h => h.Score)
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .ToList();

        // Fetch titles
        var docIds = byDoc.Select(h => h.DocumentId).ToList();
        var titles = await _db.Documents
            .AsNoTracking()
            .Where(d => docIds.Contains(d.Id))
            .Select(d => new { d.Id, d.Title })
            .ToDictionaryAsync(d => d.Id, d => d.Title, ct);

        var hits = byDoc.Select(h => new SearchHit
        {
            EntityType = "document",
            EntityId = h.DocumentId,
            Title = titles.GetValueOrDefault(h.DocumentId, string.Empty),
            Snippet = h.Snippet,
            Score = h.Score,
            Metadata = new Dictionary<string, object?> { ["chunkId"] = h.ChunkId },
        }).ToList();

        return new SearchResponse
        {
            Hits = hits,
            TotalCount = hits.Count,
            ModeUsed = SearchMode.Semantic,
        };
    }
}
