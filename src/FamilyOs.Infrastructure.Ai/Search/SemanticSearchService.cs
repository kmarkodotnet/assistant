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
            SELECT entity_type, entity_id, chunk_id, snippet, score FROM (
                SELECT 'document' as entity_type,
                       dc.document_id as entity_id,
                       dc.id as chunk_id,
                       dc.content as snippet,
                       1 - (dc.embedding <=> @vector) as score
                FROM app.document_chunk dc
                JOIN app.document d ON d.id = dc.document_id
                WHERE dc.embedding IS NOT NULL
                  AND dc.embedding_model = @model
                  AND d.deleted_utc IS NULL
                  AND (@userId IS NULL OR d.is_private = false OR d.created_by_user_account_id = @userId::uuid)
                UNION ALL
                SELECT 'note' as entity_type,
                       nc.note_id as entity_id,
                       nc.id as chunk_id,
                       nc.content as snippet,
                       1 - (nc.embedding <=> @vector) as score
                FROM app.note_chunk nc
                JOIN app.note n ON n.id = nc.note_id
                WHERE nc.embedding IS NOT NULL
                  AND nc.embedding_model = @model
                  AND n.deleted_utc IS NULL
                  AND (@userId IS NULL OR n.is_private = false OR n.created_by_user_account_id = @userId::uuid)
            ) combined
            ORDER BY score DESC
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
            .Select(r => new SemanticHit(r.EntityType, r.EntityId, r.ChunkId, r.Snippet, r.Score))
            .ToList();
    }

    private sealed class SemanticQueryResult
    {
        public string EntityType { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public Guid ChunkId { get; set; }
        public string Snippet { get; set; } = string.Empty;
        public double Score { get; set; }
    }
}
