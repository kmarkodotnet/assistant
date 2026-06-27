using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Domain.Entities;
using FamilyOs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FamilyOs.Workers.Services;

public sealed class SummarizeJobRunner
{
    private readonly FamilyOsDbContext _db;
    private readonly IDocumentSummarizer _summarizer;
    private readonly IProcessingProgressNotifier _notifier;
    private readonly ILogger<SummarizeJobRunner> _logger;

    private static readonly Action<ILogger, Guid, Exception?> LogDocTextNotFound =
        LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(1, nameof(LogDocTextNotFound)),
            "SummarizeJobRunner: DocumentText for document {Id} not found — skipping.");

    private static readonly Action<ILogger, Guid, Exception?> LogSummarized =
        LoggerMessage.Define<Guid>(LogLevel.Information, new EventId(2, nameof(LogSummarized)),
            "SummarizeJobRunner: summary created for document {Id}.");

    public SummarizeJobRunner(
        FamilyOsDbContext db,
        IDocumentSummarizer summarizer,
        IProcessingProgressNotifier notifier,
        ILogger<SummarizeJobRunner> logger)
    {
        _db = db;
        _summarizer = summarizer;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task RunAsync(AiProcessingJob job, CancellationToken ct)
    {
        var docText = await _db.DocumentTexts
            .FirstOrDefaultAsync(t => t.DocumentId == job.TargetId, ct);

        if (docText is null)
        {
            LogDocTextNotFound(_logger, job.TargetId, null);
            return;
        }

        await _notifier.NotifyProgressAsync(job.TargetId, "Summarize", 0, ct);

        var doc = await _db.Documents.FindAsync([job.TargetId], ct);
        var language = doc?.Language ?? "hu";

        var result = await _summarizer.SummarizeAsync(docText.Content, language, ct);

        // Upsert: mark existing current summary as superseded, insert new one
        var existing = await _db.DocumentSummaries
            .Where(s => s.DocumentId == job.TargetId && s.IsCurrent)
            .ToListAsync(ct);

        foreach (var old in existing)
        {
            old.Supersede();
        }

        var summary = DocumentSummary.Create(
            job.TargetId,
            result.Summary,
            result.ModelName,
            result.PromptVersion);

        await _db.DocumentSummaries.AddAsync(summary, ct);
        await _db.SaveChangesAsync(ct);

        await _notifier.NotifyProgressAsync(job.TargetId, "Summarize", 100, ct);

        LogSummarized(_logger, job.TargetId, null);
    }
}
