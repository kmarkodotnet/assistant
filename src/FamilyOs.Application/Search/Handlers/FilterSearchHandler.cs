using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Search.Dtos;
using Microsoft.EntityFrameworkCore;
using DomainTaskStatus = FamilyOs.Domain.Enums.TaskStatus;

namespace FamilyOs.Application.Search.Handlers;

public sealed class FilterSearchHandler
{
    private readonly IFamilyOsDbContext _db;

    public FilterSearchHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task<SearchResponse> SearchAsync(
        SearchRequest req,
        Guid? userId,
        CancellationToken ct)
    {
        var entityTypes = req.EntityTypes ?? ["documents", "tasks", "deadlines", "notes", "reminders", "suggestions"];
        var hits = new List<SearchHit>();

        if (entityTypes.Contains("documents", StringComparer.OrdinalIgnoreCase))
        {
            var docQuery = _db.Documents
                .AsNoTracking()
                .Where(d => !d.IsPrivate || d.CreatedByUserAccountId == userId);

            if (req.From.HasValue)
                docQuery = docQuery.Where(d => d.DocumentDate >= req.From.Value);
            if (req.To.HasValue)
                docQuery = docQuery.Where(d => d.DocumentDate <= req.To.Value);
            if (req.RelatedFamilyMemberId.HasValue)
                docQuery = docQuery.Where(d => d.RelatedFamilyMemberId == req.RelatedFamilyMemberId.Value);
            if (req.TagNames is { Length: > 0 })
                docQuery = docQuery.Where(d =>
                    d.DocumentText != null &&
                    _db.DocumentTags.Any(dt =>
                        dt.DocumentId == d.Id &&
                        _db.Tags.Any(t => t.Id == dt.TagId && req.TagNames.Contains(t.Name))));
            if (req.TopicSlugs is { Length: > 0 })
                docQuery = docQuery.Where(d =>
                    _db.DocumentTopics.Any(dt =>
                        dt.DocumentId == d.Id &&
                        _db.Topics.Any(t => t.Id == dt.TopicId && req.TopicSlugs.Contains(t.Slug))));

            if (!string.IsNullOrWhiteSpace(req.Query))
                docQuery = docQuery.Where(d => d.Title.Contains(req.Query));

            var docs = await docQuery
                .OrderByDescending(d => d.CreatedUtc)
                .Skip((req.Page - 1) * req.PageSize)
                .Take(req.PageSize)
                .Select(d => new { d.Id, d.Title, d.CreatedUtc, d.DocumentDate })
                .ToListAsync(ct);

            hits.AddRange(docs.Select(d => new SearchHit
            {
                EntityType = "document",
                EntityId = d.Id,
                Title = d.Title,
                Score = 1.0,
                Metadata = new Dictionary<string, object?>
                {
                    ["createdUtc"] = d.CreatedUtc,
                    ["documentDate"] = d.DocumentDate,
                },
            }));
        }

        if (entityTypes.Contains("tasks", StringComparer.OrdinalIgnoreCase))
        {
            var taskQuery = _db.Tasks
                .AsNoTracking()
                .Where(t => !t.IsPrivate || t.CreatedByUserAccountId == userId)
                .Where(t => t.Status != DomainTaskStatus.Suggested);

            if (req.RelatedFamilyMemberId.HasValue)
                taskQuery = taskQuery.Where(t => t.AssignedToFamilyMemberId == req.RelatedFamilyMemberId.Value);

            if (!string.IsNullOrWhiteSpace(req.Query))
                taskQuery = taskQuery.Where(t => t.Title.Contains(req.Query) || (t.Description != null && t.Description.Contains(req.Query)));

            var tasks = await taskQuery
                .OrderByDescending(t => t.CreatedUtc)
                .Skip((req.Page - 1) * req.PageSize)
                .Take(req.PageSize)
                .Select(t => new { t.Id, t.Title, t.Status, t.DueDateUtc })
                .ToListAsync(ct);

            hits.AddRange(tasks.Select(t => new SearchHit
            {
                EntityType = "task",
                EntityId = t.Id,
                Title = t.Title,
                Score = 1.0,
                Metadata = new Dictionary<string, object?>
                {
                    ["status"] = t.Status.ToString(),
                    ["dueDateUtc"] = t.DueDateUtc,
                },
            }));
        }

        if (entityTypes.Contains("deadlines", StringComparer.OrdinalIgnoreCase))
        {
            var dlQuery = _db.Deadlines
                .AsNoTracking()
                .Where(d => !d.IsPrivate || d.CreatedByUserAccountId == userId);

            if (req.From.HasValue)
                dlQuery = dlQuery.Where(d => d.DueDateUtc >= req.From.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
            if (req.To.HasValue)
                dlQuery = dlQuery.Where(d => d.DueDateUtc <= req.To.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc));
            if (req.RelatedFamilyMemberId.HasValue)
                dlQuery = dlQuery.Where(d => d.RelatedFamilyMemberId == req.RelatedFamilyMemberId.Value);

            if (!string.IsNullOrWhiteSpace(req.Query))
                dlQuery = dlQuery.Where(d => d.Title.Contains(req.Query) || (d.Description != null && d.Description.Contains(req.Query)));

            var deadlines = await dlQuery
                .OrderBy(d => d.DueDateUtc)
                .Skip((req.Page - 1) * req.PageSize)
                .Take(req.PageSize)
                .Select(d => new { d.Id, d.Title, d.Status, d.DueDateUtc, d.Category })
                .ToListAsync(ct);

            hits.AddRange(deadlines.Select(d => new SearchHit
            {
                EntityType = "deadline",
                EntityId = d.Id,
                Title = d.Title,
                Score = 1.0,
                Metadata = new Dictionary<string, object?>
                {
                    ["status"] = d.Status.ToString(),
                    ["dueDateUtc"] = d.DueDateUtc,
                    ["category"] = d.Category.ToString(),
                },
            }));
        }

        if (entityTypes.Contains("notes", StringComparer.OrdinalIgnoreCase))
        {
            var noteQuery = _db.Notes
                .AsNoTracking()
                .Where(n => !n.IsPrivate || n.CreatedByUserAccountId == userId);

            if (req.RelatedFamilyMemberId.HasValue)
                noteQuery = noteQuery.Where(n => n.RelatedFamilyMemberId == req.RelatedFamilyMemberId.Value);

            if (!string.IsNullOrWhiteSpace(req.Query))
                noteQuery = noteQuery.Where(n => n.Title.Contains(req.Query) || n.Body.Contains(req.Query));

            var notes = await noteQuery
                .OrderByDescending(n => n.UpdatedUtc)
                .Skip((req.Page - 1) * req.PageSize)
                .Take(req.PageSize)
                .Select(n => new { n.Id, n.Title, n.UpdatedUtc })
                .ToListAsync(ct);

            hits.AddRange(notes.Select(n => new SearchHit
            {
                EntityType = "note",
                EntityId = n.Id,
                Title = n.Title,
                Score = 1.0,
                Metadata = new Dictionary<string, object?> { ["updatedUtc"] = n.UpdatedUtc },
            }));
        }

        if (entityTypes.Contains("reminders", StringComparer.OrdinalIgnoreCase))
        {
            var reminderQuery = _db.Reminders
                .AsNoTracking()
                .Where(r => r.TargetUserAccountId == userId || r.CreatedByUserAccountId == userId);

            if (req.From.HasValue)
                reminderQuery = reminderQuery.Where(r => r.TriggerUtc >= req.From.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
            if (req.To.HasValue)
                reminderQuery = reminderQuery.Where(r => r.TriggerUtc <= req.To.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc));

            if (!string.IsNullOrWhiteSpace(req.Query))
                reminderQuery = reminderQuery.Where(r =>
                    (r.Task != null && r.Task.Title.Contains(req.Query)) ||
                    (r.Deadline != null && r.Deadline.Title.Contains(req.Query)) ||
                    (r.SnoozeNote != null && r.SnoozeNote.Contains(req.Query)));

            var reminders = await reminderQuery
                .OrderBy(r => r.TriggerUtc)
                .Skip((req.Page - 1) * req.PageSize)
                .Take(req.PageSize)
                .Select(r => new
                {
                    r.Id,
                    Title = r.Task != null ? r.Task.Title : r.Deadline!.Title,
                    r.TriggerUtc,
                    r.Status,
                    r.Channel,
                })
                .ToListAsync(ct);

            hits.AddRange(reminders.Select(r => new SearchHit
            {
                EntityType = "reminder",
                EntityId = r.Id,
                Title = r.Title,
                Score = 1.0,
                Metadata = new Dictionary<string, object?>
                {
                    ["triggerUtc"] = r.TriggerUtc,
                    ["status"] = r.Status.ToString(),
                    ["channel"] = r.Channel.ToString(),
                },
            }));
        }

        if (entityTypes.Contains("suggestions", StringComparer.OrdinalIgnoreCase))
        {
            var suggestionQuery = _db.Tasks
                .AsNoTracking()
                .Where(t => t.Status == DomainTaskStatus.Suggested)
                .Where(t => !t.IsPrivate || t.CreatedByUserAccountId == userId);

            if (!string.IsNullOrWhiteSpace(req.Query))
                suggestionQuery = suggestionQuery.Where(t =>
                    t.Title.Contains(req.Query) || (t.Description != null && t.Description.Contains(req.Query)));

            var suggestions = await suggestionQuery
                .OrderByDescending(t => t.CreatedUtc)
                .Skip((req.Page - 1) * req.PageSize)
                .Take(req.PageSize)
                .Select(t => new { t.Id, t.Title, t.Description, t.DueDateUtc })
                .ToListAsync(ct);

            hits.AddRange(suggestions.Select(s => new SearchHit
            {
                EntityType = "suggestion",
                EntityId = s.Id,
                Title = s.Title,
                Snippet = s.Description,
                Score = 1.0,
                Metadata = new Dictionary<string, object?> { ["dueDateUtc"] = s.DueDateUtc },
            }));
        }

        return new SearchResponse
        {
            Hits = hits,
            TotalCount = hits.Count,
            ModeUsed = SearchMode.Filter,
        };
    }
}
