using FamilyOs.Application.Abstractions.Notifications;
using FamilyOs.Application.Notifications.Dtos;
using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using FamilyOs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FamilyOs.Infrastructure.Notifications;

public sealed class InAppNotificationService : INotificationService
{
    private readonly FamilyOsDbContext _db;
    private readonly IInAppNotificationPusher _pusher;
    private readonly ILogger<InAppNotificationService> _logger;

    private static readonly Action<ILogger, string?, Exception?> LogIdempotencySkip =
        LoggerMessage.Define<string?>(LogLevel.Debug, new EventId(1, nameof(LogIdempotencySkip)),
            "InApp notification skipped (idempotency hit): key={Key}");

    private static readonly Action<ILogger, Guid, Exception?> LogSent =
        LoggerMessage.Define<Guid>(LogLevel.Information, new EventId(2, nameof(LogSent)),
            "InApp notification sent to user {UserId}");

    public InAppNotificationService(
        FamilyOsDbContext db,
        IInAppNotificationPusher pusher,
        ILogger<InAppNotificationService> logger)
    {
        _db = db;
        _pusher = pusher;
        _logger = logger;
    }

    public async Task SendAsync(NotificationEnvelope envelope, NotificationChannel channel, CancellationToken ct = default)
    {
        if (channel != NotificationChannel.InApp)
            return;

        // Idempotency check
        if (envelope.IdempotencyKey is not null)
        {
            var exists = await _db.NotificationFeed
                .AnyAsync(n => n.IdempotencyKey == envelope.IdempotencyKey, ct);

            if (exists)
            {
                LogIdempotencySkip(_logger, envelope.IdempotencyKey, null);
                return;
            }
        }

        var feed = NotificationFeed.Create(
            envelope.UserId,
            envelope.Type,
            envelope.Title,
            envelope.Body,
            envelope.ActionUrl,
            envelope.IdempotencyKey);

        _db.NotificationFeed.Add(feed);
        await _db.SaveChangesAsync(ct);

        var dto = new NotificationDto
        {
            Id = feed.Id,
            Type = feed.Type,
            Title = feed.Title,
            Body = feed.Body,
            ActionUrl = feed.ActionUrl,
            ReadUtc = feed.ReadUtc,
            CreatedUtc = feed.CreatedUtc,
        };

        await _pusher.PushAsync(envelope.UserId, dto, ct);

        LogSent(_logger, envelope.UserId, null);
    }
}
