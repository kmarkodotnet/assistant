using FamilyOs.Domain.Entities;

namespace FamilyOs.Application.Common.Ai;

public interface IAiProcessingJobRepository
{
    Task<IReadOnlyList<AiProcessingJob>> GetQueuedJobsAsync(int limit, CancellationToken ct);
    Task AddAsync(AiProcessingJob job, CancellationToken ct);
    Task AddRangeAsync(IEnumerable<AiProcessingJob> jobs, CancellationToken ct);
    Task<AiProcessingJob?> GetByIdAsync(Guid id, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
