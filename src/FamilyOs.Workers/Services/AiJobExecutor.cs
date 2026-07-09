using FamilyOs.Application.Common.Ai;
using FamilyOs.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FamilyOs.Workers.Services;

/// <summary>
/// Hangfire-activated job executor. Resolved as transient — Hangfire creates one per job invocation.
/// </summary>
public sealed class AiJobExecutor
{
    private readonly IAiProcessingJobRepository _jobRepository;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AiJobExecutor> _logger;

    // LoggerMessage delegates (CA1848 compliance)
    private static readonly Action<ILogger, Guid, Exception?> LogJobNotFound =
        LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(1, nameof(LogJobNotFound)), "AiJobExecutor: job {Id} not found.");

    private static readonly Action<ILogger, Guid, JobStatus, Exception?> LogJobAlreadyProcessed =
        LoggerMessage.Define<Guid, JobStatus>(LogLevel.Debug, new EventId(2, nameof(LogJobAlreadyProcessed)), "AiJobExecutor: job {Id} already in status {Status} — skipping.");

    private static readonly Action<ILogger, Guid, AiJobType, int, Exception?> LogJobFailed =
        LoggerMessage.Define<Guid, AiJobType, int>(LogLevel.Error, new EventId(3, nameof(LogJobFailed)), "AiJobExecutor: job {Id} ({Type}) failed on attempt {Attempt}.");

    public AiJobExecutor(
        IAiProcessingJobRepository jobRepository,
        IServiceProvider serviceProvider,
        ILogger<AiJobExecutor> logger)
    {
        _jobRepository = jobRepository;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid jobId, CancellationToken ct)
    {
        var job = await _jobRepository.GetByIdAsync(jobId, ct);
        if (job is null)
        {
            LogJobNotFound(_logger, jobId, null);
            return;
        }

        if (job.Status is JobStatus.Done or JobStatus.Running)
        {
            LogJobAlreadyProcessed(_logger, jobId, job.Status, null);
            return;
        }

        job.MarkRunning();
        await _jobRepository.SaveChangesAsync(ct);

        try
        {
            await DispatchAsync(job, ct);
            job.MarkDone();
        }
        catch (Exception ex)
        {
            LogJobFailed(_logger, jobId, job.JobType, job.Attempt, ex);
            job.MarkFailed(ex.Message);
        }

        await _jobRepository.SaveChangesAsync(ct);

        // After each job completes (done or failed), check if all parallel jobs are done
        var parallelTypes = new List<AiJobType>
        {
            AiJobType.Summarize, AiJobType.Classify, AiJobType.ExtractDeadlines,
            AiJobType.ExtractTasks, AiJobType.Embed,
        };

        if (parallelTypes.Contains(job.JobType))
        {
            using var scope = _serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<PipelineOrchestrator>();
            await orchestrator.CheckAndFinalizeAsync(job.TargetId, ct);
        }
    }

    private async Task DispatchAsync(Domain.Entities.AiProcessingJob job, CancellationToken ct)
    {
        switch (job.JobType)
        {
            case AiJobType.ExtractText:
                var extractRunner = _serviceProvider.GetRequiredService<ExtractTextJobRunner>();
                await extractRunner.RunAsync(job, ct);
                break;

            case AiJobType.DetectLanguage:
                var detectRunner = _serviceProvider.GetRequiredService<DetectLanguageJobRunner>();
                await detectRunner.RunAsync(job, ct);
                break;

            case AiJobType.Summarize:
                var summarizeRunner = _serviceProvider.GetRequiredService<SummarizeJobRunner>();
                await summarizeRunner.RunAsync(job, ct);
                break;

            case AiJobType.Classify:
                var classifyRunner = _serviceProvider.GetRequiredService<ClassifyJobRunner>();
                await classifyRunner.RunAsync(job, ct);
                break;

            case AiJobType.ExtractDeadlines:
                var deadlinesRunner = _serviceProvider.GetRequiredService<ExtractDeadlinesJobRunner>();
                await deadlinesRunner.RunAsync(job, ct);
                break;

            case AiJobType.ExtractTasks:
                var tasksRunner = _serviceProvider.GetRequiredService<ExtractTasksJobRunner>();
                await tasksRunner.RunAsync(job, ct);
                break;

            case AiJobType.ExtractFacet:
                var facetRunner = _serviceProvider.GetRequiredService<ExtractFacetJobRunner>();
                await facetRunner.RunAsync(job, ct);
                break;

            case AiJobType.Embed:
                var embedRunner = _serviceProvider.GetRequiredService<EmbedJobRunner>();
                await embedRunner.RunAsync(job, ct);
                break;

            case AiJobType.ExtractEntities:
                // Post-MVP: entity extraction not yet implemented
                break;

            default:
                throw new InvalidOperationException($"Unknown AiJobType: {job.JobType}");
        }
    }
}
