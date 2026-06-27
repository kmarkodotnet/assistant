using FamilyOs.Application.Notifications.Dtos;
using MediatR;

namespace FamilyOs.Application.Notifications;

public sealed record GetNotificationFeedQuery(
    Guid UserId,
    bool OnlyUnread,
    int Page,
    int PageSize) : IRequest<NotificationFeedResponse>;
