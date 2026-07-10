using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Errors;
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

        float[] embedding;
        try
        {
            embedding = await _embeddingCache.GetOrComputeAsync(req.Query, ct);
        }
        catch (InfrastructureException)
        {
            return new SearchResponse { ModeUsed = SearchMode.Semantic };
        }
        var semanticHits = await _semanticSearch.SearchAsync(embedding, req.PageSize * 2, userId, minSimilarity: 0.50, ct);

        // Group by (EntityType, EntityId), take best score per entity
        var byEntity = semanticHits
            .GroupBy(h => (h.EntityType, h.EntityId))
            .Select(g => g.OrderByDescending(h => h.Score).First())
            .OrderByDescending(h => h.Score)
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .ToList();

        // Fetch titles from each entity type
        var docIds = byEntity.Where(h => h.EntityType == "document").Select(h => h.EntityId).ToList();
        var noteIds = byEntity.Where(h => h.EntityType == "note").Select(h => h.EntityId).ToList();
        var taskIds = byEntity.Where(h => h.EntityType == "task").Select(h => h.EntityId).ToList();
        var deadlineIds = byEntity.Where(h => h.EntityType == "deadline").Select(h => h.EntityId).ToList();

        var docTitles = docIds.Count > 0
            ? await _db.Documents.AsNoTracking()
                .Where(d => docIds.Contains(d.Id))
                .Select(d => new { d.Id, d.Title })
                .ToDictionaryAsync(d => d.Id, d => d.Title, ct)
            : new Dictionary<Guid, string>();

        var noteTitles = noteIds.Count > 0
            ? await _db.Notes.AsNoTracking()
                .Where(n => noteIds.Contains(n.Id))
                .Select(n => new { n.Id, n.Title })
                .ToDictionaryAsync(n => n.Id, n => n.Title, ct)
            : new Dictionary<Guid, string>();

        var taskTitles = taskIds.Count > 0
            ? await _db.Tasks.AsNoTracking()
                .Where(t => taskIds.Contains(t.Id))
                .Select(t => new { t.Id, t.Title })
                .ToDictionaryAsync(t => t.Id, t => t.Title, ct)
            : new Dictionary<Guid, string>();

        var deadlineTitles = deadlineIds.Count > 0
            ? await _db.Deadlines.AsNoTracking()
                .Where(d => deadlineIds.Contains(d.Id))
                .Select(d => new { d.Id, d.Title })
                .ToDictionaryAsync(d => d.Id, d => d.Title, ct)
            : new Dictionary<Guid, string>();

        var hits = byEntity.Select(h =>
        {
            var title = h.EntityType switch
            {
                "note" => noteTitles.GetValueOrDefault(h.EntityId, string.Empty),
                "task" => taskTitles.GetValueOrDefault(h.EntityId, string.Empty),
                "deadline" => deadlineTitles.GetValueOrDefault(h.EntityId, string.Empty),
                _ => docTitles.GetValueOrDefault(h.EntityId, string.Empty),
            };

            return new SearchHit
            {
                EntityType = h.EntityType,
                EntityId = h.EntityId,
                Title = title,
                Snippet = h.Snippet,
                Score = h.Score,
                Metadata = new Dictionary<string, object?> { ["chunkId"] = h.ChunkId },
            };
        }).ToList();

        return new SearchResponse
        {
            Hits = hits,
            TotalCount = hits.Count,
            ModeUsed = SearchMode.Semantic,
        };
    }
}
