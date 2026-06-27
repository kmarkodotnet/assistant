using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Domain.Enums;
using FamilyOs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FamilyOs.Workers.Services;

public sealed class PipelineOrchestrator
{
    private readonly FamilyOsDbContext _db;
    private readonly IProcessingProgressNotifier _notifier;
    private readonly ILogger<PipelineOrchestrator> _logger;

    private static readonly AiJobType[] ParallelJobTypes =
    [
        AiJobType.Summarize,
        AiJobType.Classify,
        AiJobType.ExtractDeadlines,
        AiJobType.ExtractTasks,
        AiJobType.Embed,
    ];

    private static readonly Action<ILogger, Guid, ProcessingStatus, Exception?> LogDocumentFinalized =
        LoggerMessage.Define<Guid, ProcessingStatus>(LogLevel.Information, new EventId(1, nameof(LogDocumentFinalized)),
            "PipelineOrchestrator: document {Id} finalized with status {Status}.");

    private static readonly Action<ILogger, Guid, Exception?> LogDocumentNotReady =
        LoggerMessage.Define<Guid>(LogLevel.Debug, new EventId(2, nameof(LogDocumentNotReady)),
            "PipelineOrchestrator: document {Id} not yet ready for finalization.");

    public PipelineOrchestrator(
        FamilyOsDbContext db,
        IProcessingProgressNotifier notifier,
        ILogger<PipelineOrchestrator> logger)
    {
        _db = db;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task CheckAndFinalizeAsync(Guid documentId, CancellationToken ct)
    {
        var jobs = await _db.AiProcessingJobs
            .Where(j => j.TargetId == documentId && ParallelJobTypes.Contains(j.JobType))
            .OrderByDescending(j => j.CreatedUtc)
            .ToListAsync(ct);

        // Get latest job per type
        var latestPerType = ParallelJobTypes
            .Select(t => jobs.FirstOrDefault(j => j.JobType == t))
            .Where(j => j != null)
            .ToList();

        if (latestPerType.Count < ParallelJobTypes.Length)
        {
            LogDocumentNotReady(_logger, documentId, null);
            return; // not all enqueued yet
        }

        if (latestPerType.Any(j => j!.Status == JobStatus.Queued || j!.Status == JobStatus.Running))
        {
            LogDocumentNotReady(_logger, documentId, null);
            return; // still running
        }

        var doc = await _db.Documents.FindAsync([documentId], ct);
        if (doc is null) return;

        var allFailed = latestPerType.All(j => j!.Status == JobStatus.Failed);
        var finalStatus = allFailed ? ProcessingStatus.Failed : ProcessingStatus.Done;

        doc.SetProcessingStatus(finalStatus);

        await _db.SaveChangesAsync(ct);

        // Notify (no-op in workers; SignalR in API via post-MVP backplane)
        await _notifier.NotifyProcessedAsync(documentId, finalStatus.ToString(), ct);

        LogDocumentFinalized(_logger, documentId, finalStatus, null);
    }
}
