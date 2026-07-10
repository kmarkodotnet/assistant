namespace FamilyOs.Application.Abstractions.Persistence;

public sealed record FtsHit(Guid EntityId, string Title, string? Snippet, double Rank);

public interface ITaskDeadlineFtsSearchService
{
    Task<IReadOnlyList<FtsHit>> SearchTasksAsync(
        string query, Guid? userId, int limit, bool suggestedOnly, CancellationToken ct);

    Task<IReadOnlyList<FtsHit>> SearchDeadlinesAsync(
        string query, Guid? userId, int limit, CancellationToken ct);
}
