using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using FamilyOs.Domain.Services;
using FamilyOs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FamilyOs.Workers.Services;

public sealed class EscalationScheduler : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EscalationScheduler> _logger;

    private static readonly Action<ILogger, Exception?> LogStarted =
        LoggerMessage.Define(LogLevel.Information, new EventId(1, nameof(LogStarted)), "EscalationScheduler started.");

    private static readonly Action<ILogger, Exception?> LogStopped =
        LoggerMessage.Define(LogLevel.Information, new EventId(2, nameof(LogStopped)), "EscalationScheduler stopped.");

    private static readonly Action<ILogger, Guid, int, Exception?> LogEscalated =
        LoggerMessage.Define<Guid, int>(LogLevel.Information, new EventId(3, nameof(LogEscalated)),
            "Reminder {Id} escalated to level {Level}.");

    private static readonly Action<ILogger, Guid, Exception?> LogMaxEscalation =
        LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(4, nameof(LogMaxEscalation)),
            "Reminder {Id} reached max escalation level — skipping.");

    private static readonly Action<ILogger, Exception?> LogError =
        LoggerMessage.Define(LogLevel.Error, new EventId(5, nameof(LogError)),
            "EscalationScheduler error.");

    public EscalationScheduler(IServiceScopeFactory scopeFactory, ILogger<EscalationScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogStarted(_logger, null);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                LogError(_logger, ex);
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }

        LogStopped(_logger, null);
    }

    private async Task RunAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FamilyOsDbContext>();
        var now = DateTime.UtcNow;

        var candidates = await db.Reminders
            .Where(r => r.Status == ReminderStatus.Fired
                && r.AcknowledgedUtc == null)
            .ToListAsync(ct);

        foreach (var reminder in candidates)
        {
            ct.ThrowIfCancellationRequested();

            var timeout = EscalationPolicyEvaluator.GetEscalationTimeout(reminder.EscalationLevel);
            if (reminder.FiredUtc == null || now - reminder.FiredUtc.Value < timeout)
                continue;

            if (!EscalationPolicyEvaluator.CanEscalate(reminder.EscalationLevel))
            {
                reminder.Skip();
                LogMaxEscalation(_logger, reminder.Id, null);
                continue;
            }

            var newLevel = reminder.EscalationLevel + 1;
            var newTrigger = now.AddMinutes(15); // fire escalated reminder soon

            Reminder escalated;
            if (reminder.TaskId.HasValue)
            {
                escalated = Reminder.ForTask(
                    reminder.TaskId.Value,
                    reminder.TargetUserAccountId,
                    newTrigger,
                    reminder.Channel,
                    reminder.CreatedByUserAccountId);
            }
            else
            {
                escalated = Reminder.ForDeadline(
                    reminder.DeadlineId!.Value,
                    reminder.TargetUserAccountId,
                    newTrigger,
                    reminder.Channel,
                    reminder.CreatedByUserAccountId);
            }

            escalated.SetEscalationLevel(newLevel);
            db.Reminders.Add(escalated);
            reminder.Skip();

            LogEscalated(_logger, reminder.Id, newLevel, null);
        }

        if (candidates.Any(r => r.Status == ReminderStatus.Skipped))
        {
            await db.SaveChangesAsync(ct);
        }
    }
}
