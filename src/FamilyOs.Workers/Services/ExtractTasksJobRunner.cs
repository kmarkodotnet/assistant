using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Application.Documents.Suggestions;
using FamilyOs.Domain.Entities;
using FamilyOs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FamilyOs.Workers.Services;

public sealed class ExtractTasksJobRunner
{
    private readonly FamilyOsDbContext _db;
    private readonly ITaskExtractor _extractor;
    private readonly IProcessingProgressNotifier _notifier;
    private readonly ILogger<ExtractTasksJobRunner> _logger;

    private static readonly Action<ILogger, Guid, Exception?> LogDocumentNotFound =
        LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(1, nameof(LogDocumentNotFound)),
            "ExtractTasksJobRunner: Document {Id} not found — skipping.");

    private static readonly Action<ILogger, Guid, int, int, Exception?> LogTasksCreated =
        LoggerMessage.Define<Guid, int, int>(LogLevel.Information, new EventId(2, nameof(LogTasksCreated)),
            "ExtractTasksJobRunner: document {Id} — {Created} tasks created, {Skipped} skipped (dedup).");

    public ExtractTasksJobRunner(
        FamilyOsDbContext db,
        ITaskExtractor extractor,
        IProcessingProgressNotifier notifier,
        ILogger<ExtractTasksJobRunner> logger)
    {
        _db = db;
        _extractor = extractor;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task RunAsync(AiProcessingJob job, CancellationToken ct)
    {
        var document = await _db.Documents
            .Include(d => d.DocumentText)
            .FirstOrDefaultAsync(d => d.Id == job.TargetId, ct);

        if (document is null || document.DocumentText is null)
        {
            LogDocumentNotFound(_logger, job.TargetId, null);
            return;
        }

        await _notifier.NotifyProgressAsync(job.TargetId, "ExtractTasks", 0, ct);

        // Load family members for name resolution
        var familyMembers = await _db.FamilyMembers
            .Where(fm => fm.DeletedUtc == null)
            .ToListAsync(ct);

        var memberNames = familyMembers
            .Select(fm => fm.DisplayName)
            .ToList();

        var suggestions = await _extractor.ExtractAsync(document.DocumentText.Content, memberNames, ct);

        // Load existing tasks for this document (for dedup)
        var existing = await _db.Tasks
            .Where(t => t.SourceDocumentId == job.TargetId)
            .ToListAsync(ct);

        int created = 0, skipped = 0;

        foreach (var suggestion in suggestions)
        {
            if (SuggestionDedupService.IsTaskDuplicate(existing, suggestion.Title))
            {
                skipped++;
                continue;
            }

            // Resolve assignedToHint to FamilyMember by DisplayName (case-insensitive exact match for MVP)
            Guid? assignedMemberId = null;
            if (!string.IsNullOrWhiteSpace(suggestion.AssignedToHint))
            {
                var matched = familyMembers.FirstOrDefault(fm =>
                    string.Equals(fm.DisplayName, suggestion.AssignedToHint, StringComparison.OrdinalIgnoreCase));
                assignedMemberId = matched?.Id;
            }

            DateTime? dueDateUtc = suggestion.DueDate.HasValue
                ? suggestion.DueDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                : null;

            var task = FamilyTask.CreateSuggestion(
                suggestion.Title,
                job.TargetId,
                document.CreatedByUserAccountId,
                dueDateUtc,
                suggestion.Description,
                assignedMemberId);

            await _db.Tasks.AddAsync(task, ct);
            existing.Add(task);
            created++;
        }

        await _db.SaveChangesAsync(ct);

        await _notifier.NotifyProgressAsync(job.TargetId, "ExtractTasks", 100, ct);

        LogTasksCreated(_logger, job.TargetId, created, skipped, null);
    }
}
