using FamilyOs.Application.Common.Ai;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FamilyOs.Workers.Services;

/// <summary>
/// Background service that polls the AI job queue every 10 seconds
/// and enqueues pending jobs into Hangfire for execution.
/// </summary>
public sealed class AiJobScheduler : BackgroundService
{
    private const int PollIntervalSeconds = 10;
    private const int JobBatchSize = 20;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AiJobScheduler> _logger;

    // LoggerMessage delegates (CA1848 compliance)
    private static readonly Action<ILogger, Exception?> LogStarted =
        LoggerMessage.Define(LogLevel.Information, new EventId(1, nameof(LogStarted)), "AiJobScheduler started.");

    private static readonly Action<ILogger, Exception?> LogStopped =
        LoggerMessage.Define(LogLevel.Information, new EventId(2, nameof(LogStopped)), "AiJobScheduler stopped.");

    private static readonly Action<ILogger, Exception?> LogPollError =
        LoggerMessage.Define(LogLevel.Error, new EventId(3, nameof(LogPollError)), "AiJobScheduler: error during job scheduling poll.");

    private static readonly Action<ILogger, int, Exception?> LogJobsFound =
        LoggerMessage.Define<int>(LogLevel.Debug, new EventId(4, nameof(LogJobsFound)), "AiJobScheduler: found {Count} queued jobs to schedule.");

    public AiJobScheduler(
        IServiceScopeFactory scopeFactory,
        ILogger<AiJobScheduler> logger)
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
                await ScheduleJobsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                LogPollError(_logger, ex);
            }

            await Task.Delay(TimeSpan.FromSeconds(PollIntervalSeconds), stoppingToken);
        }

        LogStopped(_logger, null);
    }

    private async Task ScheduleJobsAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var jobRepository = scope.ServiceProvider.GetRequiredService<IAiProcessingJobRepository>();

        var jobs = await jobRepository.GetQueuedJobsAsync(JobBatchSize, ct);
        if (jobs.Count == 0)
            return;

        LogJobsFound(_logger, jobs.Count, null);

        foreach (var job in jobs)
        {
            BackgroundJob.Enqueue<AiJobExecutor>(
                executor => executor.ExecuteAsync(job.Id, CancellationToken.None));
        }
    }
}
