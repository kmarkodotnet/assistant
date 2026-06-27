using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Search.Dtos;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Search.Handlers;

public sealed class FtsSearchHandler
{
    private readonly IFamilyOsDbContext _db;

    public FtsSearchHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task<SearchResponse> SearchAsync(
        SearchRequest req,
        Guid? userId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Query))
            return new SearchResponse { ModeUsed = SearchMode.Text };

        var q = req.Query;

        var results = await _db.DocumentTexts
            .AsNoTracking()
            .Where(dt =>
                dt.Content.Contains(q) ||
                dt.Document!.Title.Contains(q))
            .Where(dt =>
                !dt.Document!.IsPrivate || dt.Document.CreatedByUserAccountId == userId)
            .OrderByDescending(dt => dt.Document!.CreatedUtc)
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(dt => new
            {
                dt.DocumentId,
                dt.Document!.Title,
                dt.Content,
            })
            .ToListAsync(ct);

        var hits = results.Select(r => new SearchHit
        {
            EntityType = "document",
            EntityId = r.DocumentId,
            Title = r.Title,
            Snippet = r.Content.Length > 200 ? r.Content[..200] + "…" : r.Content,
            Score = 0.7,
        }).ToList();

        return new SearchResponse
        {
            Hits = hits,
            TotalCount = hits.Count,
            ModeUsed = SearchMode.Text,
        };
    }
}
