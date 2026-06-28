using FamilyOs.Application.Common;
using MediatR;

namespace FamilyOs.Application.Admin.AiJobs;

public sealed record ListAiJobsQuery(
    string? Status,
    string? JobType,
    int Page,
    int PageSize) : IRequest<PagedResult<AiJobDto>>;
