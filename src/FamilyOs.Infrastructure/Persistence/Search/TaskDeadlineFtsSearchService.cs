using FamilyOs.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Infrastructure.Persistence.Search;

public sealed class TaskDeadlineFtsSearchService : ITaskDeadlineFtsSearchService
{
    private readonly FamilyOsDbContext _db;

    public TaskDeadlineFtsSearchService(FamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<FtsHit>> SearchTasksAsync(
        string query, Guid? userId, int limit, bool suggestedOnly, CancellationToken ct)
    {
        var statusPredicate = suggestedOnly ? "t.status = 'Suggested'" : "t.status <> 'Suggested'";
        var tsQueryText = BuildTsQueryText(query);

        var sql = $@"
            SELECT t.id, t.title, t.description AS snippet,
                   ts_rank(t.tsv, websearch_to_tsquery('hungarian_unaccent', @q)) AS rank
            FROM app.task t
            WHERE t.tsv @@ websearch_to_tsquery('hungarian_unaccent', @q)
              AND t.deleted_utc IS NULL
              AND {statusPredicate}
              AND (@userId IS NULL OR t.is_private = false OR t.created_by_user_account_id = @userId::uuid)
            ORDER BY rank DESC
            LIMIT @limit";

        var results = await _db.Database
            .SqlQueryRaw<FtsQueryResult>(
                sql,
                new Npgsql.NpgsqlParameter("@q", tsQueryText),
                new Npgsql.NpgsqlParameter("@userId", (object?)userId?.ToString() ?? DBNull.Value),
                new Npgsql.NpgsqlParameter("@limit", limit))
            .ToListAsync(ct);

        return results.Select(r => new FtsHit(r.Id, r.Title, r.Snippet, r.Rank)).ToList();
    }

    public async Task<IReadOnlyList<FtsHit>> SearchDeadlinesAsync(
        string query, Guid? userId, int limit, CancellationToken ct)
    {
        var tsQueryText = BuildTsQueryText(query);

        const string sql = @"
            SELECT d.id, d.title, d.description AS snippet,
                   ts_rank(d.tsv, websearch_to_tsquery('hungarian_unaccent', @q)) AS rank
            FROM app.deadline d
            WHERE d.tsv @@ websearch_to_tsquery('hungarian_unaccent', @q)
              AND d.deleted_utc IS NULL
              AND (@userId IS NULL OR d.is_private = false OR d.created_by_user_account_id = @userId::uuid)
            ORDER BY rank DESC
            LIMIT @limit";

        var results = await _db.Database
            .SqlQueryRaw<FtsQueryResult>(
                sql,
                new Npgsql.NpgsqlParameter("@q", tsQueryText),
                new Npgsql.NpgsqlParameter("@userId", (object?)userId?.ToString() ?? DBNull.Value),
                new Npgsql.NpgsqlParameter("@limit", limit))
            .ToListAsync(ct);

        return results.Select(r => new FtsHit(r.Id, r.Title, r.Snippet, r.Rank)).ToList();
    }

    // websearch_to_tsquery ANDs space-separated terms by default; joining tokens with
    // "or" broadens the match to "any significant word" (stemmed), so e.g. "áramszámla
    // határideje" still matches an "áramszámla befizetése" deadline title.
    private static string BuildTsQueryText(string query)
    {
        var tokens = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return tokens.Length > 1 ? string.Join(" or ", tokens) : query;
    }

    private sealed class FtsQueryResult
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Snippet { get; set; }
        public double Rank { get; set; }
    }
}
