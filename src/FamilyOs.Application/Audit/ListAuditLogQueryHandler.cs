using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common;
using FamilyOs.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Audit;

public sealed class ListAuditLogQueryHandler(IFamilyOsDbContext db)
    : IRequestHandler<ListAuditLogQuery, PagedResult<AuditLogDto>>
{
    public async Task<PagedResult<AuditLogDto>> Handle(ListAuditLogQuery request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Min(request.PageSize > 0 ? request.PageSize : 50, 200);
        var page = request.Page > 0 ? request.Page : 1;

        var query = db.AuditLogs.AsNoTracking();

        if (request.From.HasValue)
            query = query.Where(l => l.OccurredUtc >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(l => l.OccurredUtc <= request.To.Value);

        if (request.UserAccountId.HasValue)
            query = query.Where(l => l.UserAccountId == request.UserAccountId.Value);

        if (!string.IsNullOrWhiteSpace(request.Action) &&
            Enum.TryParse<AuditAction>(request.Action, ignoreCase: true, out var action))
            query = query.Where(l => l.Action == action);

        if (!string.IsNullOrWhiteSpace(request.EntityType))
            query = query.Where(l => l.EntityType == request.EntityType);

        if (request.EntityId.HasValue)
            query = query.Where(l => l.EntityId == request.EntityId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(l => l.OccurredUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new AuditLogDto(
                l.Id,
                l.OccurredUtc,
                l.UserAccountId,
                l.Action.ToString(),
                l.EntityType,
                l.EntityId,
                l.IpAddress,
                l.UserAgent,
                l.DetailsJson))
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedResult<AuditLogDto>(items, page, pageSize, totalCount, totalPages);
    }
}
