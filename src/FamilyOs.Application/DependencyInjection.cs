using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Application.Ai;
using FamilyOs.Application.Ai.Tools;
using FamilyOs.Application.Auth.Options;
using FamilyOs.Application.Common.Ai;
using FamilyOs.Application.Common.Behaviors;
using FamilyOs.Application.Search.Handlers;
using FluentValidation;
using Mapster;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace FamilyOs.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AuthOptions>(configuration.GetSection("Auth"));
        services.AddSingleton(BuildToolCallTokenOptions(configuration));
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AuditBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(assembly);
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();

        // Search sub-handlers (not MediatR, registered as scoped services)
        services.AddScoped<FilterSearchHandler>();
        services.AddScoped<FtsSearchHandler>();
        services.AddScoped<SemanticSearchHandler>();
        services.AddScoped<HybridSearchHandler>();
        services.AddScoped<AggregateSearchHandler>();
        services.AddScoped<QaHandler>();
        services.AddScoped<CommandHandler>();

        // Tool-calling (CR260710-07 / ADR-0011) — whitelisted tools + planner + token service.
        services.AddScoped<ITool, CreateReminderTool>();
        services.AddScoped<ITool, AssignDocumentTool>();
        services.AddScoped<ITool, AddDocumentTagTool>();
        services.AddScoped<IToolRegistry, ToolRegistry>();
        services.AddScoped<IToolCallTokenService, ToolCallTokenService>();
        services.AddScoped<ToolCallPlanner>();

        // In-process replay guard for /tool-calls/confirm (code review finding on c43dd87) —
        // singleton lifetime is required since the whole point is to remember jtis across
        // requests within the token's TTL window.
        services.AddSingleton<IToolCallReplayGuard, ToolCallReplayGuard>();

        return services;
    }

    // ADR-0011 D1: FEATURE_NL_COMMANDS / TOOLCALL_SIGNING_KEY / TOOLCALL_PROPOSAL_TTL_SECONDS
    // are flat env vars (no "Section:" nesting), so they're read directly here — at service
    // registration time — rather than bound via services.Configure<T>(section). This also
    // gives true fail-fast behavior: an invalid setup throws before the host finishes building,
    // not lazily on first request.
    private static ToolCallTokenOptions BuildToolCallTokenOptions(IConfiguration configuration)
    {
        var featureEnabled = configuration.GetValue<bool>("FEATURE_NL_COMMANDS");
        var signingKey = configuration["TOOLCALL_SIGNING_KEY"];
        var ttlSeconds = configuration.GetValue<int?>("TOOLCALL_PROPOSAL_TTL_SECONDS") ?? 600;

        if (featureEnabled && string.IsNullOrWhiteSpace(signingKey))
            throw new InvalidOperationException(
                "TOOLCALL_SIGNING_KEY is required when FEATURE_NL_COMMANDS=true.");

        return new ToolCallTokenOptions
        {
            FeatureEnabled = featureEnabled,
            SigningKey = signingKey,
            TtlSeconds = ttlSeconds,
        };
    }
}
