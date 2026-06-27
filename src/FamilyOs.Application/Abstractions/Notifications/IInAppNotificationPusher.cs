using FamilyOs.Application.Notifications.Dtos;

namespace FamilyOs.Application.Abstractions.Notifications;

public interface IInAppNotificationPusher
{
    Task PushAsync(Guid userId, NotificationDto notification, CancellationToken ct = default);
}
