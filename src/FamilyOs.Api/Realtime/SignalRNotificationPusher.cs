using FamilyOs.Application.Abstractions.Notifications;
using FamilyOs.Application.Notifications.Dtos;
using Microsoft.AspNetCore.SignalR;

namespace FamilyOs.Api.Realtime;

public sealed class SignalRNotificationPusher : IInAppNotificationPusher
{
    private readonly IHubContext<NotificationsHub> _hub;

    public SignalRNotificationPusher(IHubContext<NotificationsHub> hubContext)
    {
        _hub = hubContext;
    }

    public Task PushAsync(Guid userId, NotificationDto notification, CancellationToken ct = default)
        => _hub.Clients.All.SendAsync("notificationCreated", notification, ct);
}
