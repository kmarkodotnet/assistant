using FamilyOs.Application.Common.Ai;
using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using FamilyOs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FamilyOs.Workers.Services;

/// <summary>
/// Runs once at startup. Creates Embed jobs for tasks and deadlines that have no chunk yet,
/// so that entities created before the embedding infrastructure was deployed get indexed.
/// </summary>
public sealed class EmbedBackfillService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmbedBackfillService> _logger;

    private static readonly Action<ILogger, int, int, Exception?> LogBackfill =
        LoggerMessage.Define<int, int>(LogLevel.Information, new EventId(1, nameof(LogBackfill)),
            "EmbedBackfill: queued {Tasks} task(s) and {Deadlines} deadline(s) for embedding.");

    public EmbedBackfillService(IServiceScopeFactory scopeFactory, ILogger<EmbedBackfillService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FamilyOsDbContext>();
        var jobRepo = scope.ServiceProvider.GetRequiredService<IAiProcessingJobRepository>();

        var tasksWithoutChunks = await db.Tasks
            .Where(t => t.DeletedUtc == null && !db.TaskChunks.Any(c => c.TaskId == t.Id))
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        var deadlinesWithoutChunks = await db.Deadlines
            .Where(d => d.DeletedUtc == null && !db.DeadlineChunks.Any(c => c.DeadlineId == d.Id))
            .Select(d => d.Id)
            .ToListAsync(cancellationToken);

        foreach (var taskId in tasksWithoutChunks)
            await jobRepo.AddAsync(AiProcessingJob.CreateForTask(AiJobType.Embed, taskId), cancellationToken);

        foreach (var deadlineId in deadlinesWithoutChunks)
            await jobRepo.AddAsync(AiProcessingJob.CreateForDeadline(AiJobType.Embed, deadlineId), cancellationToken);

        LogBackfill(_logger, tasksWithoutChunks.Count, deadlinesWithoutChunks.Count, null);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
