using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Application.Documents.Suggestions;
using FamilyOs.Domain.Entities;
using FamilyOs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FamilyOs.Workers.Services;

public sealed class ExtractDeadlinesJobRunner
{
    private readonly FamilyOsDbContext _db;
    private readonly IDeadlineExtractor _extractor;
    private readonly IProcessingProgressNotifier _notifier;
    private readonly ILogger<ExtractDeadlinesJobRunner> _logger;

    private static readonly Action<ILogger, Guid, Exception?> LogDocumentNotFound =
        LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(1, nameof(LogDocumentNotFound)),
            "ExtractDeadlinesJobRunner: Document {Id} not found — skipping.");

    private static readonly Action<ILogger, Guid, int, int, Exception?> LogDeadlinesCreated =
        LoggerMessage.Define<Guid, int, int>(LogLevel.Information, new EventId(2, nameof(LogDeadlinesCreated)),
            "ExtractDeadlinesJobRunner: document {Id} — {Created} deadlines created, {Skipped} skipped (dedup).");

    public ExtractDeadlinesJobRunner(
        FamilyOsDbContext db,
        IDeadlineExtractor extractor,
        IProcessingProgressNotifier notifier,
        ILogger<ExtractDeadlinesJobRunner> logger)
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

        await _notifier.NotifyProgressAsync(job.TargetId, "ExtractDeadlines", 0, ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var suggestions = await _extractor.ExtractAsync(document.DocumentText.Content, today, ct);

        // Load existing deadlines for this document (for dedup)
        var existing = await _db.Deadlines
            .Where(d => d.SourceDocumentId == job.TargetId)
            .ToListAsync(ct);

        int created = 0, skipped = 0;

        foreach (var suggestion in suggestions)
        {
            var dueDateTime = suggestion.DueDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

            if (SuggestionDedupService.IsDeadlineDuplicate(existing, suggestion.Title, dueDateTime))
            {
                skipped++;
                continue;
            }

            var deadline = Deadline.CreateSuggestion(
                suggestion.Title,
                dueDateTime,
                job.TargetId,
                document.CreatedByUserAccountId,
                suggestion.Description);

            await _db.Deadlines.AddAsync(deadline, ct);
            existing.Add(deadline);
            created++;
        }

        await _db.SaveChangesAsync(ct);

        await _notifier.NotifyProgressAsync(job.TargetId, "ExtractDeadlines", 100, ct);

        LogDeadlinesCreated(_logger, job.TargetId, created, skipped, null);
    }
}
