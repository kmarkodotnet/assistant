using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Domain.Entities;
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
}
