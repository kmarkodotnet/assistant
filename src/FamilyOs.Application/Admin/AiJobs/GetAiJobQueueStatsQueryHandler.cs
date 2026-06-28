using FamilyOs.Application.Abstractions.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Admin.AiJobs;

public sealed class GetAiJobQueueStatsQueryHandler(IFamilyOsDbContext db)
    : IRequestHandler<GetAiJobQueueStatsQuery, IReadOnlyList<QueueStatEntry>>
{
    public async Task<IReadOnlyList<QueueStatEntry>> Handle(GetAiJobQueueStatsQuery request, CancellationToken cancellationToken)
    {
        var stats = await db.AiProcessingJobs
            .AsNoTracking()
            .GroupBy(j => new { j.JobType, j.Status })
            .Select(g => new QueueStatEntry(
                g.Key.JobType.ToString(),
                g.Key.Status.ToString(),
                g.Count()))
            .ToListAsync(cancellationToken);

        return stats;
    }
}
