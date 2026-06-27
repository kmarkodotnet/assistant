using FamilyOs.Application.Abstractions.Notifications;
using FamilyOs.Domain.Enums;

namespace FamilyOs.Infrastructure.Notifications;

public sealed class CompositeNotificationService : INotificationService
{
    private readonly InAppNotificationService _inApp;
    private readonly SmtpNotificationService _smtp;

    public CompositeNotificationService(
        InAppNotificationService inApp,
        SmtpNotificationService smtp)
    {
        _inApp = inApp;
        _smtp = smtp;
    }

    public Task SendAsync(NotificationEnvelope envelope, NotificationChannel channel, CancellationToken ct = default)
        => channel switch
        {
            NotificationChannel.Email => _smtp.SendAsync(envelope, channel, ct),
            _ => _inApp.SendAsync(envelope, channel, ct),
        };
}
