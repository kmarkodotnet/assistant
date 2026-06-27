using FamilyOs.Application.Common.Ai;
using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using FamilyOs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Infrastructure.Persistence.Repositories;

public sealed class AiProcessingJobRepository : IAiProcessingJobRepository
{
    private readonly FamilyOsDbContext _context;

    public AiProcessingJobRepository(FamilyOsDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AiProcessingJob>> GetQueuedJobsAsync(int limit, CancellationToken ct)
    {
        // FOR UPDATE SKIP LOCKED requires raw SQL — EF cannot express this
        var jobs = await _context.AiProcessingJobs
            .FromSqlInterpolated($@"
                SELECT * FROM app.ai_processing_job
                WHERE status IN ('Queued', 'Failed')
                  AND next_attempt_utc <= NOW()
                ORDER BY created_utc
                LIMIT {limit}
                FOR UPDATE SKIP LOCKED")
            .ToListAsync(ct);

        return jobs;
    }

    public async Task AddAsync(AiProcessingJob job, CancellationToken ct)
    {
        await _context.AiProcessingJobs.AddAsync(job, ct);
    }

    public async Task AddRangeAsync(IEnumerable<AiProcessingJob> jobs, CancellationToken ct)
    {
        await _context.AiProcessingJobs.AddRangeAsync(jobs, ct);
    }

    public async Task<AiProcessingJob?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _context.AiProcessingJobs
            .FirstOrDefaultAsync(j => j.Id == id, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await _context.SaveChangesAsync(ct);
    }
}
