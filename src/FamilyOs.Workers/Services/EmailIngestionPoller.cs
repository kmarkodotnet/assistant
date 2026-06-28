using FamilyOs.Application.Abstractions.Email;
using FamilyOs.Domain.Enums;
using FamilyOs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FamilyOs.Workers.Services;

public sealed class EmailIngestionPoller : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailIngestionPoller> _logger;

    private static readonly Action<ILogger, Exception?> LogStarted =
        LoggerMessage.Define(LogLevel.Information, new EventId(1, nameof(LogStarted)), "EmailIngestionPoller started.");

    private static readonly Action<ILogger, Exception?> LogStopped =
        LoggerMessage.Define(LogLevel.Information, new EventId(2, nameof(LogStopped)), "EmailIngestionPoller stopped.");

    private static readonly Action<ILogger, Exception?> LogPollError =
        LoggerMessage.Define(LogLevel.Error, new EventId(3, nameof(LogPollError)), "EmailIngestionPoller: error during poll cycle.");

    private static readonly Action<ILogger, int, Exception?> LogSyncCycle =
        LoggerMessage.Define<int>(LogLevel.Debug, new EventId(4, nameof(LogSyncCycle)), "EmailIngestionPoller: syncing {Count} active Gmail sources.");

    private static readonly Action<ILogger, Guid, int, int, Exception?> LogSourceSynced =
        LoggerMessage.Define<Guid, int, int>(LogLevel.Information, new EventId(5, nameof(LogSourceSynced)),
            "EmailIngestionPoller: source {SourceId} synced — fetched={Fetched}, inserted={Inserted}.");

    public EmailIngestionPoller(IServiceScopeFactory scopeFactory, ILogger<EmailIngestionPoller> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogStarted(_logger, null);

        using var timer = new PeriodicTimer(PollInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await PollAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                LogPollError(_logger, ex);
            }
        }

        LogStopped(_logger, null);
    }

    private async Task PollAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FamilyOsDbContext>();
        var ingestionService = scope.ServiceProvider.GetRequiredService<IEmailIngestionService>();

        var activeSources = await db.Sources
            .Where(s => s.Kind == SourceKind.GmailAccount && s.IsActive && s.DeletedUtc == null)
            .Select(s => s.Id)
            .ToListAsync(ct);

        LogSyncCycle(_logger, activeSources.Count, null);

        foreach (var sourceId in activeSources)
        {
            ct.ThrowIfCancellationRequested();

            var report = await ingestionService.SyncAsync(sourceId, ct);
            LogSourceSynced(_logger, sourceId, report.Fetched, report.Inserted, null);
        }
    }
}
