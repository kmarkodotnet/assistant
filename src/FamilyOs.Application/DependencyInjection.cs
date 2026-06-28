using FamilyOs.Application.Auth.Options;
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
        services.AddScoped<QaHandler>();

        return services;
    }
}
