using FamilyOs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FamilyOs.Workers.Services;

public sealed class NotificationFeedRetentionJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationFeedRetentionJob> _logger;

    private static readonly Action<ILogger, Exception?> LogStarted =
        LoggerMessage.Define(LogLevel.Information, new EventId(1, nameof(LogStarted)), "NotificationFeedRetentionJob started.");

    private static readonly Action<ILogger, int, Exception?> LogDeleted =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(2, nameof(LogDeleted)),
            "NotificationFeedRetentionJob: deleted {Count} old read notifications.");

    private static readonly Action<ILogger, Exception?> LogError =
        LoggerMessage.Define(LogLevel.Error, new EventId(3, nameof(LogError)),
            "NotificationFeedRetentionJob error.");

    public NotificationFeedRetentionJob(IServiceScopeFactory scopeFactory, ILogger<NotificationFeedRetentionJob> logger)
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

            // Run daily
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FamilyOsDbContext>();

        var cutoff = DateTime.UtcNow.AddDays(-90);

        var deleted = await db.NotificationFeed
            .Where(n => n.ReadUtc != null && n.CreatedUtc < cutoff)
            .ExecuteDeleteAsync(ct);

        LogDeleted(_logger, deleted, null);
    }
}
