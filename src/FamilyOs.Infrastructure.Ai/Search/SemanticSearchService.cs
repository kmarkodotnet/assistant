using System.Data;
using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using Pgvector;

namespace FamilyOs.Infrastructure.Ai.Search;

public sealed class SemanticSearchService : ISemanticSearchService
{
    private readonly IFamilyOsDbContext _db;
    private readonly IEmbedder _embedder;

    public SemanticSearchService(IFamilyOsDbContext db, IEmbedder embedder)
    {
        _db = db;
        _embedder = embedder;
    }

    public async Task<IReadOnlyList<SemanticHit>> SearchAsync(
        float[] queryEmbedding,
        int limit,
        Guid? userId,
        CancellationToken ct = default)
    {
        var vector = new Vector(queryEmbedding);
        var modelName = _embedder.ModelName;

        var sql = @"
            SELECT dc.document_id, dc.id as chunk_id, dc.content as snippet,
                   1 - (dc.embedding <=> @vector) as score
            FROM app.document_chunk dc
            JOIN app.document d ON d.id = dc.document_id
            WHERE dc.embedding IS NOT NULL
              AND dc.embedding_model = @model
              AND d.deleted_utc IS NULL
              AND (@userId IS NULL OR d.is_private = false OR d.created_by_user_account_id = @userId::uuid)
            ORDER BY dc.embedding <=> @vector
            LIMIT @limit";

        var results = await _db.Database
            .SqlQueryRaw<SemanticQueryResult>(
                sql,
                new Npgsql.NpgsqlParameter("@vector", vector),
                new Npgsql.NpgsqlParameter("@model", modelName),
                new Npgsql.NpgsqlParameter("@userId", (object?)userId?.ToString() ?? DBNull.Value),
                new Npgsql.NpgsqlParameter("@limit", limit))
            .ToListAsync(ct);

        return results
            .Select(r => new SemanticHit(r.DocumentId, r.ChunkId, r.Snippet, r.Score))
            .ToList();
    }

    private sealed class SemanticQueryResult
    {
        public Guid DocumentId { get; set; }
        public Guid ChunkId { get; set; }
        public string Snippet { get; set; } = string.Empty;
        public double Score { get; set; }
    }
}
