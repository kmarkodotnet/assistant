using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Audit;

public sealed class GetSecurityEventsQueryHandler(IFamilyOsDbContext db)
    : IRequestHandler<GetSecurityEventsQuery, IReadOnlyList<AuditLogDto>>
{
    private static readonly AuditAction[] SecurityActions =
    [
        AuditAction.Login,
        AuditAction.LoginFailed,
        AuditAction.PermissionChange,
        AuditAction.ExternalApiCall,
    ];

    public async Task<IReadOnlyList<AuditLogDto>> Handle(GetSecurityEventsQuery request, CancellationToken cancellationToken)
    {
        var from = request.From ?? DateTime.UtcNow.AddDays(-7);
        var to = request.To ?? DateTime.UtcNow;

        var items = await db.AuditLogs
            .AsNoTracking()
            .Where(l => SecurityActions.Contains(l.Action))
            .Where(l => l.OccurredUtc >= from && l.OccurredUtc <= to)
            .OrderByDescending(l => l.OccurredUtc)
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

        return items;
    }
}
