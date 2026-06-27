using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FamilyOs.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FamilyOsDbContext>
{
    public FamilyOsDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<FamilyOsDbContext>();
        optionsBuilder
            .UseNpgsql(
                config.GetConnectionString("DefaultConnection")
                    ?? "Host=localhost;Port=5432;Database=family_os;Username=family_migrator;Password=changeme",
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "app"))
            .UseSnakeCaseNamingConvention();

        return new FamilyOsDbContext(optionsBuilder.Options);
    }
}
