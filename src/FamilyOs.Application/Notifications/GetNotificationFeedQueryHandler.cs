using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Notifications.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Notifications;

public sealed class GetNotificationFeedQueryHandler : IRequestHandler<GetNotificationFeedQuery, NotificationFeedResponse>
{
    private readonly IFamilyOsDbContext _db;

    public GetNotificationFeedQueryHandler(IFamilyOsDbContext db)
    {
        _db = db;
    }

    public async Task<NotificationFeedResponse> Handle(GetNotificationFeedQuery request, CancellationToken cancellationToken)
    {
        var query = _db.NotificationFeed
            .AsNoTracking()
            .Where(n => n.TargetUserAccountId == request.UserId);

        if (request.OnlyUnread)
        {
            query = query.Where(n => n.ReadUtc == null);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(n => n.CreatedUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = n.Type,
                Title = n.Title,
                Body = n.Body,
                ActionUrl = n.ActionUrl,
                ReadUtc = n.ReadUtc,
                CreatedUtc = n.CreatedUtc,
            })
            .ToListAsync(cancellationToken);

        return new NotificationFeedResponse
        {
            Items = items,
            TotalCount = totalCount,
            HasMore = totalCount > request.Page * request.PageSize,
        };
    }
}
