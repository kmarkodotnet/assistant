using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using FamilyOs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FamilyOs.Workers.Services;

public sealed class ClassifyJobRunner
{
    private readonly FamilyOsDbContext _db;
    private readonly IDocumentClassifier _classifier;
    private readonly IProcessingProgressNotifier _notifier;
    private readonly ILogger<ClassifyJobRunner> _logger;

    private static readonly Action<ILogger, Guid, Exception?> LogDocTextNotFound =
        LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(1, nameof(LogDocTextNotFound)),
            "ClassifyJobRunner: DocumentText for document {Id} not found — skipping.");

    private static readonly Action<ILogger, Guid, int, int, Exception?> LogClassified =
        LoggerMessage.Define<Guid, int, int>(LogLevel.Information, new EventId(2, nameof(LogClassified)),
            "ClassifyJobRunner: document {Id} classified — {TagCount} tags, {TopicCount} topics.");

    private static readonly Action<ILogger, Guid, Exception?> LogEmailDiscardedNoTopic =
        LoggerMessage.Define<Guid>(LogLevel.Information, new EventId(3, nameof(LogEmailDiscardedNoTopic)),
            "ClassifyJobRunner: email-sourced document {Id} matched no known topic — soft-deleted.");

    public ClassifyJobRunner(
        FamilyOsDbContext db,
        IDocumentClassifier classifier,
        IProcessingProgressNotifier notifier,
        ILogger<ClassifyJobRunner> logger)
    {
        _db = db;
        _classifier = classifier;
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

        await _notifier.NotifyProgressAsync(job.TargetId, "Classify", 0, ct);

        var result = await _classifier.ClassifyAsync(docText.Content, ct);

        // Process tags
        var existingDocTags = await _db.DocumentTags
            .Where(dt => dt.DocumentId == job.TargetId)
            .Select(dt => dt.TagId)
            .ToListAsync(ct);

        foreach (var tagName in result.Tags)
        {
            if (string.IsNullOrWhiteSpace(tagName)) continue;

            var normalizedName = tagName.ToLowerInvariant().Trim();

            // Find or create tag
            var tag = await _db.Tags
                .FirstOrDefaultAsync(t => t.Name == normalizedName, ct);

            if (tag is null)
            {
                tag = Tag.Create(normalizedName);
                await _db.Tags.AddAsync(tag, ct);
                await _db.SaveChangesAsync(ct); // flush to get the ID
            }
            else
            {
                tag.IncrementUsage();
            }

            // Skip if DocumentTag already exists
            if (existingDocTags.Contains(tag.Id)) continue;

            var docTag = new DocumentTag
            {
                DocumentId = job.TargetId,
                TagId = tag.Id,
                Origin = Origin.AiSuggested,
                IsApproved = false,
            };
            await _db.DocumentTags.AddAsync(docTag, ct);
            existingDocTags.Add(tag.Id);
        }

        // Process topics — only find existing, do NOT create new ones
        var existingDocTopics = await _db.DocumentTopics
            .Where(dt => dt.DocumentId == job.TargetId)
            .Select(dt => dt.TopicId)
            .ToListAsync(ct);

        foreach (var topicSlug in result.Topics)
        {
            if (string.IsNullOrWhiteSpace(topicSlug)) continue;

            var normalizedSlug = topicSlug.ToLowerInvariant().Trim();

            var topic = await _db.Topics
                .FirstOrDefaultAsync(t => t.Slug == normalizedSlug, ct);

            if (topic is null) continue; // skip unknown topics

            if (existingDocTopics.Contains(topic.Id)) continue;

            var docTopic = new DocumentTopic
            {
                DocumentId = job.TargetId,
                TopicId = topic.Id,
                Origin = Origin.AiSuggested,
                IsApproved = false,
            };
            await _db.DocumentTopics.AddAsync(docTopic, ct);
            existingDocTopics.Add(topic.Id);
        }

        // Facet chaining: if classification identified a facet type, enqueue ExtractFacet
        if (result.FacetType is "Warranty" or "Medical" or "Financial")
        {
            var hasPendingFacetJob = await _db.AiProcessingJobs.AnyAsync(
                j => j.TargetId == job.TargetId
                     && j.JobType == AiJobType.ExtractFacet
                     && (j.Status == JobStatus.Queued || j.Status == JobStatus.Running), ct);

            if (!hasPendingFacetJob)
            {
                await _db.AiProcessingJobs.AddAsync(
                    AiProcessingJob.Create(AiJobType.ExtractFacet, job.TargetId), ct);
            }
        }

        // Gmail-imported content that the classifier couldn't fit into any known topic (not even
        // the catch-all "egyeb") is treated as noise (newsletters, notifications, spam) rather than
        // family-relevant material — soft-delete so it never surfaces in the UI. "egyeb" itself is a
        // real match (existingDocTopics.Count > 0 in that case), so genuinely ambiguous-but-relevant
        // mail still survives; only a fully empty topic match gets discarded. Reversible (soft
        // delete only), never a hard delete.
        if (existingDocTopics.Count == 0)
        {
            var document = await _db.Documents.FirstOrDefaultAsync(d => d.Id == job.TargetId, ct);
            if (document is not null && document.SourceType == SourceType.Email)
            {
                document.SoftDelete();

                var stillQueued = await _db.AiProcessingJobs
                    .Where(j => j.TargetId == job.TargetId
                                && j.TargetType == JobTargetType.Document
                                && j.Status == JobStatus.Queued)
                    .ToListAsync(ct);
                foreach (var queuedJob in stillQueued)
                    queuedJob.Cancel();

                LogEmailDiscardedNoTopic(_logger, job.TargetId, null);
            }
        }

        await _db.SaveChangesAsync(ct);

        await _notifier.NotifyProgressAsync(job.TargetId, "Classify", 100, ct);

        LogClassified(_logger, job.TargetId, result.Tags.Length, result.Topics.Length, null);
    }
}
