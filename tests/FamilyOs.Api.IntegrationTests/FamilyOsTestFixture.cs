using DotNet.Testcontainers.Builders;
using FamilyOs.Api.IntegrationTests.Fakes;
using FamilyOs.Application.Abstractions.Auth;
using FamilyOs.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace FamilyOs.Api.IntegrationTests;

[CollectionDefinition("IntegrationTests")]
public sealed class IntegrationTestsFixtureGroup : ICollectionFixture<FamilyOsTestFixture> { }

[Trait("Category", "Integration")]
public sealed class FamilyOsTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("pgvector/pgvector:pg16")
        .Build();

    public HttpClient Client { get; private set; } = null!;
    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Run migrations BEFORE building the WebApplicationFactory.
        // The factory's NpgsqlDataSource caches PostgreSQL type metadata (enum OIDs etc.)
        // on first connection. If migrations run after the first connection, the custom
        // enum types (app.user_role, app.relation, …) are not yet in the cache and every
        // enum-column INSERT/SELECT fails with DbType = Object → 500.
        // A plain connection-string DbContext is sufficient for migrations (they use raw SQL).
        await RunMigrationsAsync(_postgres.GetConnectionString());

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:Default", _postgres.GetConnectionString());
                builder.UseSetting("Auth:GoogleClientId", "test-client-id");
                builder.UseSetting("Auth:BootstrapAdmin", "admin@test.com");
                builder.UseSetting("Auth:AllowedEmails:0", "admin@test.com");

                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IGoogleTokenValidator>();
                    services.AddSingleton<IGoogleTokenValidator, FakeGoogleTokenValidator>();
                });
            });

        Client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private static async Task RunMigrationsAsync(string connectionString)
    {
        var options = new DbContextOptionsBuilder<FamilyOsDbContext>()
            .UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "app");
                npgsql.UseVector();
            })
            .UseSnakeCaseNamingConvention()
            .Options;

        await using var db = new FamilyOsDbContext(options);
        await db.Database.MigrateAsync();
    }
}
