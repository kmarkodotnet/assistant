using FamilyOs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace FamilyOs.Infrastructure.Tests;

[Trait("Category", "Integration")]
public sealed class MigrationsTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("pgvector/pgvector:pg16")
        .Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task Migrations_ApplyCleanly_OnEmptyDatabase()
    {
        var options = new DbContextOptionsBuilder<FamilyOsDbContext>()
            .UseNpgsql(
                _postgres.GetConnectionString(),
                npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", "app");
                    npgsql.UseVector();
                })
            .UseSnakeCaseNamingConvention()
            .Options;

        await using var db = new FamilyOsDbContext(options);

        // Act — should not throw
        await db.Database.MigrateAsync();

        // Assert — tables exist
        var tables = await db.Database
            .SqlQueryRaw<string>(
                "SELECT table_name FROM information_schema.tables WHERE table_schema = 'app'")
            .ToListAsync();

        Assert.Contains("family_member", tables);
        Assert.Contains("user_account", tables);
    }
}
