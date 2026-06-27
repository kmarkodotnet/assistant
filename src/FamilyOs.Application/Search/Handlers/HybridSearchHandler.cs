using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Search.Dtos;
using FamilyOs.Application.Search.Rrf;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Search.Handlers;

public sealed class HybridSearchHandler
{
    private readonly ISemanticSearchService _semanticSearch;
    private readonly IQueryEmbeddingCache _embeddingCache;
    private readonly FtsSearchHandler _ftsHandler;
    private readonly IFamilyOsDbContext _db;

    public HybridSearchHandler(
        ISemanticSearchService semanticSearch,
        IQueryEmbeddingCache embeddingCache,
        FtsSearchHandler ftsHandler,
        IFamilyOsDbContext db)
    {
        _semanticSearch = semanticSearch;
        _embeddingCache = embeddingCache;
        _ftsHandler = ftsHandler;
        _db = db;
    }

    public async Task<(SearchResponse response, IReadOnlyList<(string chunkId, string content)> topChunks)>
        SearchWithChunksAsync(SearchRequest req, Guid? userId, CancellationToken ct)
    {
        // Run FTS and semantic in parallel
        var embeddingTask = _embeddingCache.GetOrComputeAsync(req.Query, ct);
        var ftsTask = _ftsHandler.SearchAsync(req, userId, ct);

        await System.Threading.Tasks.Task.WhenAll(embeddingTask, ftsTask);

        var embedding = await embeddingTask;
        var ftsResponse = await ftsTask;

        var semanticHits = await _semanticSearch.SearchAsync(embedding, req.PageSize * 2, userId, ct);

        // Build ranked lists for RRF
        var ftsRanked = ftsResponse.Hits.Select(h => h.EntityId).ToList();
        var vectorRanked = semanticHits
            .GroupBy(h => h.DocumentId)
            .Select(g => g.OrderByDescending(x => x.Score).First().DocumentId)
            .ToList();

        var fused = ReciprocalRankFusion.Fuse(ftsRanked, vectorRanked);

        var pagedIds = fused
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(f => f.id)
            .ToList();

        var titles = await _db.Documents
            .AsNoTracking()
            .Where(d => pagedIds.Contains(d.Id))
            .Select(d => new { d.Id, d.Title })
            .ToDictionaryAsync(d => d.Id, d => d.Title, ct);

        var scoreMap = fused.ToDictionary(f => f.id, f => f.score);

        var hits = pagedIds
            .Select(id => new SearchHit
            {
                EntityType = "document",
                EntityId = id,
                Title = titles.GetValueOrDefault(id, string.Empty),
                Score = scoreMap.GetValueOrDefault(id),
                Snippet = semanticHits.FirstOrDefault(h => h.DocumentId == id)?.Snippet,
            })
            .ToList();

        var topChunks = semanticHits
            .Take(20)
            .Select(h => (chunkId: h.ChunkId.ToString(), content: h.Snippet))
            .ToList();

        var response = new SearchResponse
        {
            Hits = hits,
            TotalCount = fused.Count,
            ModeUsed = SearchMode.Semantic,
        };

        return (response, topChunks);
    }

    public async Task<SearchResponse> SearchAsync(SearchRequest req, Guid? userId, CancellationToken ct)
    {
        var (response, _) = await SearchWithChunksAsync(req, userId, ct);
        return response;
    }
}
