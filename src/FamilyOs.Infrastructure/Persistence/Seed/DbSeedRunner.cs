using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace FamilyOs.Infrastructure.Persistence.Seed;

public sealed class DbSeedRunner
{
    private static readonly Action<ILogger, Exception?> LogMigratingDatabase =
        LoggerMessage.Define(LogLevel.Information, new EventId(1, nameof(LogMigratingDatabase)),
            "Running database migrations...");

    private static readonly Action<ILogger, Exception?> LogMigrationsComplete =
        LoggerMessage.Define(LogLevel.Information, new EventId(2, nameof(LogMigrationsComplete)),
            "Database migrations complete.");

    private static readonly Action<ILogger, Exception?> LogSyncingPassword =
        LoggerMessage.Define(LogLevel.Information, new EventId(3, nameof(LogSyncingPassword)),
            "Synchronizing family_app role password.");

    private static readonly Action<ILogger, Exception?> LogGrantingPermissions =
        LoggerMessage.Define(LogLevel.Information, new EventId(4, nameof(LogGrantingPermissions)),
            "Granting schema permissions to family_app.");

    private readonly IConfiguration _configuration;
    private readonly ILogger<DbSeedRunner> _logger;

    public DbSeedRunner(IConfiguration configuration, ILogger<DbSeedRunner> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var migrationConnStr = _configuration.GetConnectionString("MigrationConnection")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:MigrationConnection is required. " +
                "Set it to a connection string using the family_migrator (superuser) credentials.");

        var migrationOptions = new DbContextOptionsBuilder<FamilyOsDbContext>()
            .UseNpgsql(migrationConnStr, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "app");
                npgsql.UseVector();
            })
            .UseSnakeCaseNamingConvention()
            .Options;

        await using var migrationDb = new FamilyOsDbContext(migrationOptions);

        LogMigratingDatabase(_logger, null);
        await migrationDb.Database.MigrateAsync(ct);
        LogMigrationsComplete(_logger, null);

        // Sync family_app's password to match APP_DB_PASSWORD from the runtime connection string.
        // The InitialSetup migration creates family_app with a placeholder password; this corrects it.
        var appConnStr = _configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        var appPassword = new NpgsqlConnectionStringBuilder(appConnStr).Password;

        await using var conn = new NpgsqlConnection(migrationConnStr);
        await conn.OpenAsync(ct);

        if (!string.IsNullOrEmpty(appPassword))
        {
            LogSyncingPassword(_logger, null);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"ALTER ROLE family_app PASSWORD '{appPassword.Replace("'", "''")}'";
            await cmd.ExecuteNonQueryAsync(ct);
        }

        LogGrantingPermissions(_logger, null);
        await using var grantCmd = conn.CreateCommand();
        grantCmd.CommandText = """
            GRANT USAGE ON SCHEMA app TO family_app;
            GRANT USAGE ON SCHEMA hangfire TO family_app;
            GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA app TO family_app;
            GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA hangfire TO family_app;
            GRANT USAGE, SELECT, UPDATE ON ALL SEQUENCES IN SCHEMA app TO family_app;
            GRANT USAGE, SELECT, UPDATE ON ALL SEQUENCES IN SCHEMA hangfire TO family_app;
            ALTER DEFAULT PRIVILEGES IN SCHEMA app
                GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO family_app;
            ALTER DEFAULT PRIVILEGES IN SCHEMA app
                GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO family_app;
            ALTER DEFAULT PRIVILEGES IN SCHEMA hangfire
                GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO family_app;
            ALTER DEFAULT PRIVILEGES IN SCHEMA hangfire
                GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO family_app;
            """;
        await grantCmd.ExecuteNonQueryAsync(ct);
    }
}
