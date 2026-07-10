using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using FamilyOs.Domain.Services;
using FamilyOs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pgvector;

namespace FamilyOs.Workers.Services;

public sealed class EmbedJobRunner
{
    private readonly FamilyOsDbContext _db;
    private readonly IEmbedder _embedder;
    private readonly IProcessingProgressNotifier _notifier;
    private readonly ILogger<EmbedJobRunner> _logger;

    private static readonly Action<ILogger, Guid, Exception?> LogDocTextNotFound =
        LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(1, nameof(LogDocTextNotFound)),
            "EmbedJobRunner: DocumentText for document {Id} not found — skipping.");

    private static readonly Action<ILogger, Guid, int, Exception?> LogEmbedded =
        LoggerMessage.Define<Guid, int>(LogLevel.Information, new EventId(2, nameof(LogEmbedded)),
            "EmbedJobRunner: document {Id} embedded into {Count} chunks.");

    private static readonly Action<ILogger, Guid, Exception?> LogNoteNotFound =
        LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(3, nameof(LogNoteNotFound)),
            "EmbedJobRunner: Note {Id} not found — skipping.");

    private static readonly Action<ILogger, Guid, int, Exception?> LogNoteEmbedded =
        LoggerMessage.Define<Guid, int>(LogLevel.Information, new EventId(4, nameof(LogNoteEmbedded)),
            "EmbedJobRunner: note {Id} embedded into {Count} chunks.");

    private static readonly Action<ILogger, Guid, Exception?> LogTaskNotFound =
        LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(5, nameof(LogTaskNotFound)),
            "EmbedJobRunner: FamilyTask {Id} not found — skipping.");

    private static readonly Action<ILogger, Guid, int, Exception?> LogTaskEmbedded =
        LoggerMessage.Define<Guid, int>(LogLevel.Information, new EventId(6, nameof(LogTaskEmbedded)),
            "EmbedJobRunner: task {Id} embedded into {Count} chunks.");

    private static readonly Action<ILogger, Guid, Exception?> LogDeadlineNotFound =
        LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(7, nameof(LogDeadlineNotFound)),
            "EmbedJobRunner: Deadline {Id} not found — skipping.");

    private static readonly Action<ILogger, Guid, int, Exception?> LogDeadlineEmbedded =
        LoggerMessage.Define<Guid, int>(LogLevel.Information, new EventId(8, nameof(LogDeadlineEmbedded)),
            "EmbedJobRunner: deadline {Id} embedded into {Count} chunks.");

    public EmbedJobRunner(
        FamilyOsDbContext db,
        IEmbedder embedder,
        IProcessingProgressNotifier notifier,
        ILogger<EmbedJobRunner> logger)
    {
        _db = db;
        _embedder = embedder;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task RunAsync(AiProcessingJob job, CancellationToken ct)
    {
        if (job.TargetType == JobTargetType.Note)
        {
            await EmbedNoteAsync(job, ct);
            return;
        }
        if (job.TargetType == JobTargetType.Task)
        {
            await EmbedTaskAsync(job, ct);
            return;
        }
        if (job.TargetType == JobTargetType.Deadline)
        {
            await EmbedDeadlineAsync(job, ct);
            return;
        }

        await EmbedDocumentAsync(job, ct);
    }

    private async Task EmbedDocumentAsync(AiProcessingJob job, CancellationToken ct)
    {
        var docText = await _db.DocumentTexts
            .FirstOrDefaultAsync(t => t.DocumentId == job.TargetId, ct);

        if (docText is null)
        {
            LogDocTextNotFound(_logger, job.TargetId, null);
            return;
        }

        await _notifier.NotifyProgressAsync(job.TargetId, "Embed", 0, ct);

        var chunkTexts = EmbeddingChunker.Chunk(docText.Content);

        // Batch embed all chunks
        var embeddings = await _embedder.EmbedBatchAsync(chunkTexts, ct);

        for (var i = 0; i < chunkTexts.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var chunkText = chunkTexts[i];
            var embedding = embeddings[i];

            // Find or create chunk
            var chunk = await _db.DocumentChunks
                .FirstOrDefaultAsync(c => c.DocumentId == job.TargetId && c.ChunkIndex == i, ct);

            if (chunk is null)
            {
                chunk = DocumentChunk.Create(job.TargetId, i, chunkText);
                await _db.DocumentChunks.AddAsync(chunk, ct);
            }

            chunk.SetEmbedding(new Vector(embedding), _embedder.ModelName);
        }

        await _db.SaveChangesAsync(ct);

        await _notifier.NotifyProgressAsync(job.TargetId, "Embed", 100, ct);

        LogEmbedded(_logger, job.TargetId, chunkTexts.Count, null);
    }

    private async Task EmbedNoteAsync(AiProcessingJob job, CancellationToken ct)
    {
        var note = await _db.Notes
            .FirstOrDefaultAsync(n => n.Id == job.TargetId, ct);

        if (note is null)
        {
            LogNoteNotFound(_logger, job.TargetId, null);
            return;
        }

        var chunkTexts = EmbeddingChunker.Chunk(note.Body);
        var embeddings = await _embedder.EmbedBatchAsync(chunkTexts, ct);

        for (var i = 0; i < chunkTexts.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var chunkText = chunkTexts[i];
            var embedding = embeddings[i];

            var chunk = await _db.NoteChunks
                .FirstOrDefaultAsync(c => c.NoteId == job.TargetId && c.ChunkIndex == i, ct);

            if (chunk is null)
            {
                chunk = NoteChunk.Create(job.TargetId, i, chunkText);
                await _db.NoteChunks.AddAsync(chunk, ct);
            }

            chunk.SetEmbedding(new Vector(embedding), _embedder.ModelName);
        }

        await _db.SaveChangesAsync(ct);

        LogNoteEmbedded(_logger, job.TargetId, chunkTexts.Count, null);
    }

    private async Task EmbedTaskAsync(AiProcessingJob job, CancellationToken ct)
    {
        var task = await _db.Tasks
            .FirstOrDefaultAsync(t => t.Id == job.TargetId, ct);

        if (task is null)
        {
            LogTaskNotFound(_logger, job.TargetId, null);
            return;
        }

        var content = BuildTaskContent(task);
        var chunkTexts = EmbeddingChunker.Chunk(content);
        var embeddings = await _embedder.EmbedBatchAsync(chunkTexts, ct);

        for (var i = 0; i < chunkTexts.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var chunkText = chunkTexts[i];
            var embedding = embeddings[i];

            var chunk = await _db.TaskChunks
                .FirstOrDefaultAsync(c => c.TaskId == job.TargetId && c.ChunkIndex == i, ct);

            if (chunk is null)
            {
                chunk = TaskChunk.Create(job.TargetId, i, chunkText);
                await _db.TaskChunks.AddAsync(chunk, ct);
            }

            chunk.SetEmbedding(new Vector(embedding), _embedder.ModelName);
        }

        await _db.SaveChangesAsync(ct);

        LogTaskEmbedded(_logger, job.TargetId, chunkTexts.Count, null);
    }

    private async Task EmbedDeadlineAsync(AiProcessingJob job, CancellationToken ct)
    {
        var deadline = await _db.Deadlines
            .FirstOrDefaultAsync(d => d.Id == job.TargetId, ct);

        if (deadline is null)
        {
            LogDeadlineNotFound(_logger, job.TargetId, null);
            return;
        }

        var content = BuildDeadlineContent(deadline);
        var chunkTexts = EmbeddingChunker.Chunk(content);
        var embeddings = await _embedder.EmbedBatchAsync(chunkTexts, ct);

        for (var i = 0; i < chunkTexts.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var chunkText = chunkTexts[i];
            var embedding = embeddings[i];

            var chunk = await _db.DeadlineChunks
                .FirstOrDefaultAsync(c => c.DeadlineId == job.TargetId && c.ChunkIndex == i, ct);

            if (chunk is null)
            {
                chunk = DeadlineChunk.Create(job.TargetId, i, chunkText);
                await _db.DeadlineChunks.AddAsync(chunk, ct);
            }

            chunk.SetEmbedding(new Vector(embedding), _embedder.ModelName);
        }

        await _db.SaveChangesAsync(ct);

        LogDeadlineEmbedded(_logger, job.TargetId, chunkTexts.Count, null);
    }

    private static string BuildTaskContent(FamilyTask task)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"Feladat: {task.Title}");
        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"Prioritás: {task.Priority}");
        if (!string.IsNullOrWhiteSpace(task.Description))
            sb.AppendLine(task.Description);
        if (task.DueDateUtc.HasValue)
            sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"Esedékes: {task.DueDateUtc.Value:yyyy-MM-dd}");
        return sb.ToString().Trim();
    }

    private static string BuildDeadlineContent(Deadline deadline)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"Határidő: {deadline.Title}");
        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"Kategória: {deadline.Category}");
        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"Esedékes: {deadline.DueDateUtc:yyyy-MM-dd}");
        if (!string.IsNullOrWhiteSpace(deadline.Description))
            sb.AppendLine(deadline.Description);
        return sb.ToString().Trim();
    }
}
