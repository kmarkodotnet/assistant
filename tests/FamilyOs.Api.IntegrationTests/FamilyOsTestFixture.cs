using DotNet.Testcontainers.Builders;
using FamilyOs.Api.IntegrationTests.Fakes;
using FamilyOs.Application.Abstractions.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace FamilyOs.Api.IntegrationTests;

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
}
