using FamilyOs.Application.Abstractions.Common;
using FamilyOs.Infrastructure.Common;
using FamilyOs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FamilyOs.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IClock, SystemClock>();

        services.AddDbContext<FamilyOsDbContext>(opts =>
            opts.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "app"))
                .UseSnakeCaseNamingConvention());

        return services;
    }
}
