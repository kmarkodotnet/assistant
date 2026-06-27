using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Application.Common.Ai;
using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using FamilyOs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FamilyOs.Workers.Services;

public sealed class DetectLanguageJobRunner
{
    private readonly FamilyOsDbContext _db;
    private readonly ILanguageDetector _languageDetector;
    private readonly IAiProcessingJobRepository _jobRepository;
    private readonly ILogger<DetectLanguageJobRunner> _logger;

    // LoggerMessage delegates (CA1848 compliance)
    private static readonly Action<ILogger, Guid, Exception?> LogDocTextNotFound =
        LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(1, nameof(LogDocTextNotFound)),
            "DetectLanguageJobRunner: DocumentText for document {Id} not found — skipping.");

    private static readonly Action<ILogger, string, Guid, int, Exception?> LogLanguageDetected =
        LoggerMessage.Define<string, Guid, int>(LogLevel.Information, new EventId(2, nameof(LogLanguageDetected)),
            "DetectLanguageJobRunner: detected language '{Lang}' for document {Id}; enqueued {Count} downstream jobs.");

    public DetectLanguageJobRunner(
        FamilyOsDbContext db,
        ILanguageDetector languageDetector,
        IAiProcessingJobRepository jobRepository,
        ILogger<DetectLanguageJobRunner> logger)
    {
        _db = db;
        _languageDetector = languageDetector;
        _jobRepository = jobRepository;
        _logger = logger;
    }

    public async Task RunAsync(AiProcessingJob job, CancellationToken ct)
    {
        // 1. Load DocumentText (and its parent Document) for the target document ID
        var docText = await _db.DocumentTexts
            .Include(t => t.Document)
            .FirstOrDefaultAsync(t => t.DocumentId == job.TargetId, ct);

        if (docText is null)
        {
            LogDocTextNotFound(_logger, job.TargetId, null);
            return;
        }

        // 2. Detect language from first 1000 chars of the extracted text
        var snippet = docText.Content.Length > 1000
            ? docText.Content[..1000]
            : docText.Content;

        var detected = _languageDetector.Detect(snippet);
        var langCode = detected == "unknown" ? null : detected;

        // 3. Persist language on DocumentText and Document
        docText.SetLanguageDetected(langCode);

        if (docText.Document is not null)
        {
            docText.Document.SetLanguage(langCode);
        }

        await _db.SaveChangesAsync(ct);

        // 4. Enqueue downstream analysis jobs in parallel (Summarize, Classify, ExtractDeadlines, ExtractTasks, Embed)
        var nextJobs = new AiJobType[]
        {
            AiJobType.Summarize,
            AiJobType.Classify,
            AiJobType.ExtractDeadlines,
            AiJobType.ExtractTasks,
            AiJobType.Embed,
        }
        .Select(jobType => AiProcessingJob.Create(jobType, job.TargetId))
        .ToList();

        await _jobRepository.AddRangeAsync(nextJobs, ct);

        // 5. Set document processing status to Analyzing
        if (docText.Document is not null)
        {
            docText.Document.SetProcessingStatus(ProcessingStatus.Analyzing);
        }

        await _jobRepository.SaveChangesAsync(ct);

        LogLanguageDetected(_logger, langCode ?? "unknown", job.TargetId, nextJobs.Count, null);
    }
}
