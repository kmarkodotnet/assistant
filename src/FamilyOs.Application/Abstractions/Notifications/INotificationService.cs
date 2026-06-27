using FamilyOs.Domain.Enums;

namespace FamilyOs.Application.Abstractions.Notifications;

public record NotificationEnvelope(
    Guid UserId,
    string Type,
    string Title,
    string? Body,
    string? ActionUrl,
    string? IdempotencyKey);

public interface INotificationService
{
    Task SendAsync(NotificationEnvelope envelope, NotificationChannel channel, CancellationToken ct = default);
}
