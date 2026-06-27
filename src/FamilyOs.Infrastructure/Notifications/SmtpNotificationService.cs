using FamilyOs.Application.Abstractions.Notifications;
using FamilyOs.Domain.Enums;
using FamilyOs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace FamilyOs.Infrastructure.Notifications;

public sealed class SmtpNotificationService : INotificationService
{
    private readonly SmtpOptions _options;
    private readonly FamilyOsDbContext _db;
    private readonly ILogger<SmtpNotificationService> _logger;

    private static readonly Action<ILogger, Exception?> LogSmtpNotConfigured =
        LoggerMessage.Define(LogLevel.Warning, new EventId(1, nameof(LogSmtpNotConfigured)),
            "SMTP not configured — skipping email notification.");

    private static readonly Action<ILogger, Guid, Exception?> LogEmailSkippedOptOut =
        LoggerMessage.Define<Guid>(LogLevel.Debug, new EventId(2, nameof(LogEmailSkippedOptOut)),
            "Email notification skipped — user {UserId} has email disabled.");

    private static readonly Action<ILogger, Guid, int, Exception?> LogEmailSent =
        LoggerMessage.Define<Guid, int>(LogLevel.Information, new EventId(3, nameof(LogEmailSent)),
            "Email notification sent to user {UserId} on attempt {Attempt}.");

    private static readonly Action<ILogger, int, Exception?> LogEmailRetry =
        LoggerMessage.Define<int>(LogLevel.Warning, new EventId(4, nameof(LogEmailRetry)),
            "Email send attempt {Attempt} failed, retrying...");

    public SmtpNotificationService(
        IOptions<SmtpOptions> options,
        FamilyOsDbContext db,
        ILogger<SmtpNotificationService> logger)
    {
        _options = options.Value;
        _db = db;
        _logger = logger;
    }

    public async Task SendAsync(NotificationEnvelope envelope, NotificationChannel channel, CancellationToken ct = default)
    {
        if (channel != NotificationChannel.Email)
            return;

        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            LogSmtpNotConfigured(_logger, null);
            return;
        }

        // Check user opt-in
        var user = await _db.UserAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == envelope.UserId && !u.EmailEnabled, ct);

        if (user is not null)
        {
            LogEmailSkippedOptOut(_logger, envelope.UserId, null);
            return;
        }

        var email = await _db.UserAccounts
            .AsNoTracking()
            .Where(u => u.Id == envelope.UserId)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(ct);

        if (string.IsNullOrWhiteSpace(email))
            return;

        const int maxAttempts = 3;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                using var client = new SmtpClient(_options.Host, _options.Port)
                {
                    EnableSsl = _options.EnableSsl,
                    Credentials = new NetworkCredential(_options.User, _options.Password),
                };

                using var message = new MailMessage(_options.From, email, envelope.Title, envelope.Body ?? string.Empty)
                {
                    IsBodyHtml = false,
                };

                await client.SendMailAsync(message, ct);
                LogEmailSent(_logger, envelope.UserId, attempt, null);
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                LogEmailRetry(_logger, attempt, ex);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct);
            }
        }
    }
}
