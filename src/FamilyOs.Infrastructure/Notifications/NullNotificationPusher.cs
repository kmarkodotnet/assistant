using FamilyOs.Application.Abstractions.Notifications;
using FamilyOs.Application.Notifications.Dtos;

namespace FamilyOs.Infrastructure.Notifications;

/// <summary>
/// Null implementation for contexts where SignalR is not available (e.g., Workers host).
/// The notification is still persisted to the DB; real-time push is skipped.
/// </summary>
public sealed class NullNotificationPusher : IInAppNotificationPusher
{
    public Task PushAsync(Guid userId, NotificationDto notification, CancellationToken ct = default)
        => Task.CompletedTask;
}
