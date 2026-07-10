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
        // Run FTS and semantic in parallel.
        // Restrict FTS to entity types that have semantic embeddings (documents, notes).
        var ftsReq = new SearchRequest
        {
            Query = req.Query,
            Mode = req.Mode,
            EntityTypes = ["documents", "notes"],
            TopicSlugs = req.TopicSlugs,
            TagNames = req.TagNames,
            From = req.From,
            To = req.To,
            RelatedFamilyMemberId = req.RelatedFamilyMemberId,
            Page = req.Page,
            PageSize = req.PageSize,
        };
        var embeddingTask = _embeddingCache.GetOrComputeAsync(req.Query, ct);
        var ftsTask = _ftsHandler.SearchAsync(ftsReq, userId, ct);

        await System.Threading.Tasks.Task.WhenAll(embeddingTask, ftsTask);

        var embedding = await embeddingTask;
        var ftsResponse = await ftsTask;

        // Lower threshold for Q&A context gathering; hits will be filtered further by RRF score.
        var semanticHits = await _semanticSearch.SearchAsync(embedding, req.PageSize * 2, userId, minSimilarity: 0.20, ct);

        // Build entity type map (semantic hits take precedence for type resolution)
        var entityTypeMap = new Dictionary<Guid, string>();
        foreach (var hit in ftsResponse.Hits)
            entityTypeMap[hit.EntityId] = hit.EntityType;
        foreach (var hit in semanticHits)
            entityTypeMap[hit.EntityId] = hit.EntityType;

        // Build ranked lists for RRF
        var ftsRanked = ftsResponse.Hits.Select(h => h.EntityId).ToList();
        var vectorRanked = semanticHits
            .GroupBy(h => h.EntityId)
            .Select(g => g.OrderByDescending(x => x.Score).First().EntityId)
            .ToList();

        var fused = ReciprocalRankFusion.Fuse(ftsRanked, vectorRanked);

        var pagedIds = fused
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(f => f.id)
            .ToList();

        // Fetch titles per entity type
        var docIds = pagedIds.Where(id => entityTypeMap.GetValueOrDefault(id) != "note").ToList();
        var noteIds = pagedIds.Where(id => entityTypeMap.GetValueOrDefault(id) == "note").ToList();

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

        var scoreMap = fused.ToDictionary(f => f.id, f => f.score);

        var hits = pagedIds.Select(id =>
        {
            var entityType = entityTypeMap.GetValueOrDefault(id, "document");
            var title = entityType == "note"
                ? noteTitles.GetValueOrDefault(id, string.Empty)
                : docTitles.GetValueOrDefault(id, string.Empty);
            var snippet = semanticHits.FirstOrDefault(h => h.EntityId == id)?.Snippet;

            return new SearchHit
            {
                EntityType = entityType,
                EntityId = id,
                Title = title,
                Score = scoreMap.GetValueOrDefault(id),
                Snippet = snippet,
            };
        }).ToList();

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
