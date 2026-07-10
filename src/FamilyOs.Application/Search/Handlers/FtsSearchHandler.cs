using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Search.Dtos;
using Microsoft.EntityFrameworkCore;
using DomainTaskStatus = FamilyOs.Domain.Enums.TaskStatus;

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
        // For multi-word queries, also search by the longest significant word as a fallback.
        // E.g. "áramszámla határideje" → primaryWord = "áramszámla", which matches "áramszámla befizetése".
        var tokens = q.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var primaryWord = tokens.Length > 1
            ? (tokens.OrderByDescending(w => w.Length).FirstOrDefault(w => w.Length >= 4) ?? q)
            : q;
        var entityTypes = req.EntityTypes ?? ["documents", "tasks", "deadlines", "notes", "reminders", "suggestions"];
        var hits = new List<SearchHit>();

        if (entityTypes.Contains("documents", StringComparer.OrdinalIgnoreCase))
        {
            var docResults = await _db.DocumentTexts
                .AsNoTracking()
                .Where(dt =>
                    dt.Content.Contains(q) || dt.Content.Contains(primaryWord) ||
                    dt.Document!.Title.Contains(q) || dt.Document!.Title.Contains(primaryWord))
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

            hits.AddRange(docResults.Select(r => new SearchHit
            {
                EntityType = "document",
                EntityId = r.DocumentId,
                Title = r.Title,
                Snippet = r.Content.Length > 200 ? r.Content[..200] + "…" : r.Content,
                Score = 0.7,
            }));
        }

        if (entityTypes.Contains("notes", StringComparer.OrdinalIgnoreCase))
        {
            var noteResults = await _db.Notes
                .AsNoTracking()
                .Where(n => n.Title.Contains(q) || n.Title.Contains(primaryWord) ||
                            n.Body.Contains(q) || n.Body.Contains(primaryWord))
                .Where(n => !n.IsPrivate || n.CreatedByUserAccountId == userId)
                .OrderByDescending(n => n.UpdatedUtc)
                .Take(req.PageSize)
                .Select(n => new
                {
                    n.Id,
                    n.Title,
                    Snippet = n.Body.Length > 200 ? n.Body.Substring(0, 200) : n.Body,
                })
                .ToListAsync(ct);

            hits.AddRange(noteResults.Select(n => new SearchHit
            {
                EntityType = "note",
                EntityId = n.Id,
                Title = n.Title,
                Snippet = n.Snippet,
                Score = 0.65,
            }));
        }

        if (entityTypes.Contains("tasks", StringComparer.OrdinalIgnoreCase))
        {
            var taskResults = await _db.Tasks
                .AsNoTracking()
                .Where(t => !t.IsPrivate || t.CreatedByUserAccountId == userId)
                .Where(t => t.Status != DomainTaskStatus.Suggested)
                .Where(t => t.Title.Contains(q) || t.Title.Contains(primaryWord) ||
                            (t.Description != null && (t.Description.Contains(q) || t.Description.Contains(primaryWord))))
                .OrderByDescending(t => t.CreatedUtc)
                .Take(req.PageSize)
                .Select(t => new { t.Id, t.Title, Snippet = t.Description })
                .ToListAsync(ct);

            hits.AddRange(taskResults.Select(t => new SearchHit
            {
                EntityType = "task",
                EntityId = t.Id,
                Title = t.Title,
                Snippet = t.Snippet != null && t.Snippet.Length > 200 ? t.Snippet[..200] : t.Snippet,
                Score = 0.6,
            }));
        }

        if (entityTypes.Contains("deadlines", StringComparer.OrdinalIgnoreCase))
        {
            var deadlineResults = await _db.Deadlines
                .AsNoTracking()
                .Where(d => !d.IsPrivate || d.CreatedByUserAccountId == userId)
                .Where(d => d.Title.Contains(q) || d.Title.Contains(primaryWord) ||
                            (d.Description != null && (d.Description.Contains(q) || d.Description.Contains(primaryWord))))
                .OrderBy(d => d.DueDateUtc)
                .Take(req.PageSize)
                .Select(d => new { d.Id, d.Title, d.DueDateUtc })
                .ToListAsync(ct);

            hits.AddRange(deadlineResults.Select(d => new SearchHit
            {
                EntityType = "deadline",
                EntityId = d.Id,
                Title = d.Title,
                Score = 0.6,
            }));
        }

        if (entityTypes.Contains("reminders", StringComparer.OrdinalIgnoreCase))
        {
            var reminderResults = await _db.Reminders
                .AsNoTracking()
                .Where(r => r.TargetUserAccountId == userId || r.CreatedByUserAccountId == userId)
                .Where(r =>
                    (r.Task != null && (r.Task.Title.Contains(q) || r.Task.Title.Contains(primaryWord))) ||
                    (r.Deadline != null && (r.Deadline.Title.Contains(q) || r.Deadline.Title.Contains(primaryWord))) ||
                    (r.SnoozeNote != null && (r.SnoozeNote.Contains(q) || r.SnoozeNote.Contains(primaryWord))))
                .OrderBy(r => r.TriggerUtc)
                .Take(req.PageSize)
                .Select(r => new
                {
                    r.Id,
                    Title = r.Task != null ? r.Task.Title : r.Deadline!.Title,
                    r.TriggerUtc,
                })
                .ToListAsync(ct);

            hits.AddRange(reminderResults.Select(r => new SearchHit
            {
                EntityType = "reminder",
                EntityId = r.Id,
                Title = r.Title,
                Score = 0.55,
            }));
        }

        if (entityTypes.Contains("suggestions", StringComparer.OrdinalIgnoreCase))
        {
            var suggestionResults = await _db.Tasks
                .AsNoTracking()
                .Where(t => !t.IsPrivate || t.CreatedByUserAccountId == userId)
                .Where(t => t.Status == DomainTaskStatus.Suggested)
                .Where(t => t.Title.Contains(q) || t.Title.Contains(primaryWord) ||
                            (t.Description != null && (t.Description.Contains(q) || t.Description.Contains(primaryWord))))
                .OrderByDescending(t => t.CreatedUtc)
                .Take(req.PageSize)
                .Select(t => new { t.Id, t.Title, Snippet = t.Description })
                .ToListAsync(ct);

            hits.AddRange(suggestionResults.Select(s => new SearchHit
            {
                EntityType = "suggestion",
                EntityId = s.Id,
                Title = s.Title,
                Snippet = s.Snippet != null && s.Snippet.Length > 200 ? s.Snippet[..200] : s.Snippet,
                Score = 0.55,
            }));
        }

        return new SearchResponse
        {
            Hits = hits,
            TotalCount = hits.Count,
            ModeUsed = SearchMode.Text,
        };
    }
}
