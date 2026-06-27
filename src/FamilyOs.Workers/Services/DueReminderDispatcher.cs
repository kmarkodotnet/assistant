using FamilyOs.Application.Abstractions.Notifications;
using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using FamilyOs.Domain.Services;
using FamilyOs.Infrastructure.Persistence;
using FamilyOs.Infrastructure.Recurrence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FamilyOs.Workers.Services;

public sealed class DueReminderDispatcher : BackgroundService
{
    private const int BatchSize = 100;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DueReminderDispatcher> _logger;

    private static readonly Action<ILogger, Exception?> LogStarted =
        LoggerMessage.Define(LogLevel.Information, new EventId(1, nameof(LogStarted)), "DueReminderDispatcher started.");

    private static readonly Action<ILogger, Exception?> LogStopped =
        LoggerMessage.Define(LogLevel.Information, new EventId(2, nameof(LogStopped)), "DueReminderDispatcher stopped.");

    private static readonly Action<ILogger, int, Exception?> LogDispatched =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(3, nameof(LogDispatched)), "Dispatched {Count} reminders.");

    private static readonly Action<ILogger, Guid, Exception?> LogQuietHoursReschedule =
        LoggerMessage.Define<Guid>(LogLevel.Debug, new EventId(4, nameof(LogQuietHoursReschedule)),
            "Reminder {Id} rescheduled due to quiet hours.");

    private static readonly Action<ILogger, Guid, Exception?> LogReminderFired =
        LoggerMessage.Define<Guid>(LogLevel.Information, new EventId(5, nameof(LogReminderFired)),
            "Reminder {Id} fired.");

    private static readonly Action<ILogger, Exception?> LogDispatchError =
        LoggerMessage.Define(LogLevel.Error, new EventId(6, nameof(LogDispatchError)),
            "Error during reminder dispatch.");

    public DueReminderDispatcher(IServiceScopeFactory scopeFactory, ILogger<DueReminderDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogStarted(_logger, null);

        await StartupCatchUpAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchDueRemindersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                LogDispatchError(_logger, ex);
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }

        LogStopped(_logger, null);
    }

    private async Task DispatchDueRemindersAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FamilyOsDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var now = DateTime.UtcNow;

        // Use raw SQL for SKIP LOCKED
        var dueReminders = await db.Reminders
            .FromSqlRaw(@"
                SELECT * FROM app.reminder
                WHERE status = 'Scheduled'
                  AND trigger_utc <= NOW()
                  AND deleted_utc IS NULL
                ORDER BY trigger_utc
                LIMIT {0}
                FOR UPDATE SKIP LOCKED", BatchSize)
            .ToListAsync(ct);

        var dispatched = 0;

        foreach (var reminder in dueReminders)
        {
            ct.ThrowIfCancellationRequested();

            // Load user preferences for quiet hours
            var user = await db.UserAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == reminder.TargetUserAccountId, ct);

            if (user is not null && IsQuietHour(user.QuietHoursStart, user.QuietHoursEnd, now))
            {
                // Reschedule to QuietHoursEnd
                var rescheduleTime = GetQuietHoursEnd(user.QuietHoursEnd, now);
                reminder.UpdateTrigger(rescheduleTime, reminder.Channel);
                LogQuietHoursReschedule(_logger, reminder.Id, null);
                continue;
            }

            // Fire the reminder
            var envelope = new NotificationEnvelope(
                UserId: reminder.TargetUserAccountId,
                Type: "ReminderFired",
                Title: "Emlékeztető",
                Body: GetReminderBody(reminder),
                ActionUrl: GetActionUrl(reminder),
                IdempotencyKey: $"reminder-fired-{reminder.Id}");

            await notificationService.SendAsync(envelope, reminder.Channel, ct);

            reminder.Fire();
            LogReminderFired(_logger, reminder.Id, null);

            // Handle recurrence
            if (!string.IsNullOrWhiteSpace(reminder.RruleExpression))
            {
                var nextTrigger = IcalRecurrenceEvaluator.GetNextOccurrence(
                    reminder.RruleExpression, reminder.FiredUtc ?? now);

                if (nextTrigger.HasValue)
                {
                    Reminder next;
                    if (reminder.TaskId.HasValue)
                    {
                        next = Reminder.ForTask(
                            reminder.TaskId.Value,
                            reminder.TargetUserAccountId,
                            nextTrigger.Value,
                            reminder.Channel,
                            reminder.CreatedByUserAccountId,
                            reminder.RruleExpression);
                    }
                    else
                    {
                        next = Reminder.ForDeadline(
                            reminder.DeadlineId!.Value,
                            reminder.TargetUserAccountId,
                            nextTrigger.Value,
                            reminder.Channel,
                            reminder.CreatedByUserAccountId,
                            reminder.RruleExpression);
                    }
                    db.Reminders.Add(next);
                }
            }

            dispatched++;
        }

        if (dueReminders.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }

        if (dispatched > 0)
        {
            LogDispatched(_logger, dispatched, null);
        }
    }

    private async Task StartupCatchUpAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FamilyOsDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var now = DateTime.UtcNow;
        var cutoff = now.AddDays(-14);

        var missedReminders = await db.Reminders
            .Where(r => r.Status == ReminderStatus.Scheduled
                && r.TriggerUtc >= cutoff
                && r.TriggerUtc <= now)
            .OrderBy(r => r.TriggerUtc)
            .Take(500)
            .ToListAsync(ct);

        var tooOld = await db.Reminders
            .Where(r => r.Status == ReminderStatus.Scheduled && r.TriggerUtc < cutoff)
            .ToListAsync(ct);

        foreach (var reminder in tooOld)
        {
            reminder.Skip();
        }

        foreach (var reminder in missedReminders)
        {
            ct.ThrowIfCancellationRequested();

            var envelope = new NotificationEnvelope(
                UserId: reminder.TargetUserAccountId,
                Type: "ReminderFired",
                Title: "Emlékeztető (lemaradt)",
                Body: GetReminderBody(reminder),
                ActionUrl: GetActionUrl(reminder),
                IdempotencyKey: $"reminder-catchup-{reminder.Id}");

            await notificationService.SendAsync(envelope, reminder.Channel, ct);
            reminder.Fire();
        }

        if (missedReminders.Count > 0 || tooOld.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }
    }

    private static bool IsQuietHour(string? start, string? end, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(start) || string.IsNullOrWhiteSpace(end))
            return false;

        if (!TimeOnly.TryParse(start, out var quietStart) || !TimeOnly.TryParse(end, out var quietEnd))
            return false;

        var currentTime = TimeOnly.FromDateTime(now);

        if (quietStart <= quietEnd)
            return currentTime >= quietStart && currentTime < quietEnd;

        // Wrap around midnight
        return currentTime >= quietStart || currentTime < quietEnd;
    }

    private static DateTime GetQuietHoursEnd(string? end, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(end) || !TimeOnly.TryParse(end, out var quietEnd))
            return now.AddHours(8);

        var candidate = now.Date + quietEnd.ToTimeSpan();
        if (candidate <= now)
            candidate = candidate.AddDays(1);

        return candidate;
    }

    private static string GetReminderBody(Reminder reminder)
    {
        if (reminder.TaskId.HasValue)
            return $"Feladat emlékeztető: {reminder.TaskId}";
        return $"Határidő emlékeztető: {reminder.DeadlineId}";
    }

    private static string GetActionUrl(Reminder reminder)
    {
        if (reminder.TaskId.HasValue)
            return $"/tasks/{reminder.TaskId}";
        return $"/deadlines/{reminder.DeadlineId}";
    }
}
