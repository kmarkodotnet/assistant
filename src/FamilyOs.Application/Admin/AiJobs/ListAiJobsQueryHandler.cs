using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common;
using FamilyOs.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Admin.AiJobs;

public sealed class ListAiJobsQueryHandler(IFamilyOsDbContext db)
    : IRequestHandler<ListAiJobsQuery, PagedResult<AiJobDto>>
{
    public async Task<PagedResult<AiJobDto>> Handle(ListAiJobsQuery request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Min(request.PageSize > 0 ? request.PageSize : 50, 200);
        var page = request.Page > 0 ? request.Page : 1;

        var query = db.AiProcessingJobs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<JobStatus>(request.Status, ignoreCase: true, out var status))
            query = query.Where(j => j.Status == status);

        if (!string.IsNullOrWhiteSpace(request.JobType) &&
            Enum.TryParse<AiJobType>(request.JobType, ignoreCase: true, out var jobType))
            query = query.Where(j => j.JobType == jobType);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(j => j.CreatedUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new AiJobDto(
                j.Id,
                j.JobType.ToString(),
                j.TargetType.ToString(),
                j.TargetId,
                j.Status.ToString(),
                j.Attempt,
                j.ErrorMessage,
                null,
                null,
                j.CreatedUtc))
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedResult<AiJobDto>(items, page, pageSize, totalCount, totalPages);
    }
}
