using FamilyOs.Application.Abstractions.Notifications;
using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using FamilyOs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FamilyOs.Workers.Services;

/// <summary>
/// Proactive daily/weekly digest (contract: docs/contracts/daily-digest-contract.md,
/// ADR-0011). Poll-loop BackgroundService — the "run once a day" guarantee comes
/// from the idempotency key (contract §6), not from the scheduler itself.
/// </summary>
public sealed class DailyDigestJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<DailyDigestOptions> _options;
    private readonly ILogger<DailyDigestJob> _logger;

    private static readonly Action<ILogger, Exception?> LogStarted =
        LoggerMessage.Define(LogLevel.Information, new EventId(1, nameof(LogStarted)), "DailyDigestJob started.");

    private static readonly Action<ILogger, Exception?> LogStopped =
        LoggerMessage.Define(LogLevel.Information, new EventId(2, nameof(LogStopped)), "DailyDigestJob stopped.");

    private static readonly Action<ILogger, Exception?> LogRunError =
        LoggerMessage.Define(LogLevel.Error, new EventId(3, nameof(LogRunError)), "Error during daily digest run.");

    private static readonly Action<ILogger, Guid, Exception?> LogSkippedEmpty =
        LoggerMessage.Define<Guid>(LogLevel.Debug, new EventId(4, nameof(LogSkippedEmpty)),
            "Daily digest skipped for user {UserId}: nothing to report today.");

    private static readonly Action<ILogger, Guid, Exception?> LogDigestSent =
        LoggerMessage.Define<Guid>(LogLevel.Information, new EventId(5, nameof(LogDigestSent)),
            "Daily digest sent to user {UserId}.");

    public DailyDigestJob(
        IServiceScopeFactory scopeFactory,
        IOptions<DailyDigestOptions> options,
        ILogger<DailyDigestJob> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogStarted(_logger, null);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                LogRunError(_logger, ex);
            }

            await Task.Delay(_options.Value.PollInterval, stoppingToken);
        }

        LogStopped(_logger, null);
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        var options = _options.Value;
        if (!options.Enabled)
            return;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FamilyOsDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var now = DateTime.UtcNow;
        var digestDate = DateOnly.FromDateTime(now);
        var today00 = now.Date;
        var tomorrow24 = today00.AddHours(48);
        var deadlineWindowEnd = now.AddDays(options.DeadlineLookaheadDays);
        var documentWindowStart = now.AddHours(-options.DocumentLookbackHours);

        var users = await db.UserAccounts
            .AsNoTracking()
            .Where(u => u.IsActive && u.DeletedUtc == null)
            .ToListAsync(ct);

        foreach (var user in users)
        {
            ct.ThrowIfCancellationRequested();

            await ProcessUserAsync(
                db,
                notificationService,
                user,
                now,
                digestDate,
                today00,
                tomorrow24,
                deadlineWindowEnd,
                documentWindowStart,
                options,
                ct);
        }
    }

    private async Task ProcessUserAsync(
        FamilyOsDbContext db,
        INotificationService notificationService,
        UserAccount user,
        DateTime now,
        DateOnly digestDate,
        DateTime today00,
        DateTime tomorrow24,
        DateTime deadlineWindowEnd,
        DateTime documentWindowStart,
        DailyDigestOptions options,
        CancellationToken ct)
    {
        var key = $"daily-digest-{user.Id}-{digestDate:yyyy-MM-dd}";

        // Early dedup (contract §6) — avoids building the digest and avoids a
        // duplicate email even before the InAppNotificationService's own AnyAsync check.
        var already = await db.NotificationFeed
            .AsNoTracking()
            .AnyAsync(n => n.IdempotencyKey == key, ct);

        if (!DailyDigestEligibility.ShouldProcessUser(
                now, options.RunAtLocal, user.QuietHoursStart, user.QuietHoursEnd, already))
        {
            // Not yet run-time, in quiet hours, or already dispatched today —
            // re-evaluated on the next poll cycle (contract §1.1 step 4).
            return;
        }

        // §4.1 — today/tomorrow due reminders (already user-scoped, no extra RBAC needed).
        var reminders = await db.Reminders
            .AsNoTracking()
            .Where(r => r.TargetUserAccountId == user.Id
                && r.Status == ReminderStatus.Scheduled
                && r.DeletedUtc == null
                && r.TriggerUtc >= today00
                && r.TriggerUtc < tomorrow24)
            .OrderBy(r => r.TriggerUtc)
            .Select(r => new DailyDigestReminderItem(
                r.TriggerUtc,
                r.Task != null ? r.Task.Title : (r.Deadline != null ? r.Deadline.Title : string.Empty)))
            .ToListAsync(ct);

        // §4.2 — deadlines due within the lookahead window, RBAC-filtered (§3).
        var deadlines = await db.Deadlines
            .AsNoTracking()
            .Where(d => d.DeletedUtc == null
                && d.Status == DeadlineStatus.Upcoming
                && d.DueDateUtc >= now
                && d.DueDateUtc < deadlineWindowEnd)
            .VisibleTo(user)
            .OrderBy(d => d.DueDateUtc)
            .Select(d => new DailyDigestDeadlineItem(d.DueDateUtc, d.Title, d.Category))
            .ToListAsync(ct);

        // §4.3 — new documents within the lookback window, RBAC-filtered (§3).
        var documents = await db.Documents
            .AsNoTracking()
            .Where(doc => doc.DeletedUtc == null && doc.CreatedUtc >= documentWindowStart)
            .VisibleTo(user)
            .OrderByDescending(doc => doc.CreatedUtc)
            .Select(doc => new DailyDigestDocumentItem(doc.Title))
            .ToListAsync(ct);

        var content = new DailyDigestContent(
            reminders, deadlines, documents,
            options.DeadlineLookaheadDays, options.DocumentLookbackHours);

        if (content.IsEmpty)
        {
            // §5 / ADR-0011: no feed row, no email, no idempotency marker written —
            // the user is simply re-evaluated (and likely skipped again) on the next poll.
            LogSkippedEmpty(_logger, user.Id, null);
            return;
        }

        var envelope = new NotificationEnvelope(
            UserId: user.Id,
            Type: "DailyDigest",
            Title: $"Napi összefoglaló – {digestDate:yyyy. MM. dd.}",
            Body: content.BuildBody(),
            ActionUrl: "/dashboard",
            IdempotencyKey: key);

        await notificationService.SendAsync(envelope, NotificationChannel.InApp, ct);

        // §4.5 — email only if the user opted in; SMTP itself no-ops if unconfigured.
        if (user.EmailEnabled)
        {
            await notificationService.SendAsync(envelope, NotificationChannel.Email, ct);
        }

        LogDigestSent(_logger, user.Id, null);
    }
}
