using FamilyOs.Application.Common.Ai;
using FamilyOs.Infrastructure;
using FamilyOs.Infrastructure.Ai.DependencyInjection;
using FamilyOs.Infrastructure.Hangfire;
using FamilyOs.Infrastructure.Persistence.Repositories;
using FamilyOs.Workers.Services;
using Hangfire;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        // Core infrastructure (DB, auth, storage, audit, etc.)
        services.AddInfrastructure(ctx.Configuration);

        // AI services (Ollama, Tesseract, extractors, language detector, content analyzers)
        services.AddFamilyOsAiServices(ctx.Configuration);

        // Hangfire with PostgreSQL storage
        var connStr =
            ctx.Configuration.GetConnectionString("DefaultConnection") is { Length: > 0 } cs
                ? cs
                : ctx.Configuration.GetConnectionString("Default")
                    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddFamilyOsHangfire(connStr);
        services.AddHangfireServer(opts => opts.WorkerCount = 4);

        // AI job runners (scoped — one per job execution)
        services.AddScoped<ExtractTextJobRunner>();
        services.AddScoped<DetectLanguageJobRunner>();
        services.AddScoped<SummarizeJobRunner>();
        services.AddScoped<ClassifyJobRunner>();
        services.AddScoped<ExtractDeadlinesJobRunner>();
        services.AddScoped<ExtractTasksJobRunner>();
        services.AddScoped<ExtractFacetJobRunner>();
        services.AddScoped<EmbedJobRunner>();
        services.AddScoped<PipelineOrchestrator>();

        // AiJobExecutor is transient — Hangfire resolves it per invocation
        services.AddTransient<AiJobExecutor>();

        // Scheduler background service
        services.AddHostedService<AiJobScheduler>();

        // Reminder engine background services
        services.AddHostedService<DueReminderDispatcher>();
        services.AddHostedService<EscalationScheduler>();
        services.AddHostedService<NotificationFeedRetentionJob>();
    });

var host = builder.Build();
await host.RunAsync();
