using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FamilyOs.Infrastructure.Persistence.Seed;

public sealed class DbSeedRunner
{
    private static readonly Action<ILogger, Exception?> LogMigratingDatabase =
        LoggerMessage.Define(LogLevel.Information, new EventId(1, nameof(LogMigratingDatabase)),
            "Running database migrations...");

    private static readonly Action<ILogger, Exception?> LogMigrationsComplete =
        LoggerMessage.Define(LogLevel.Information, new EventId(2, nameof(LogMigrationsComplete)),
            "Database migrations complete.");

    private readonly FamilyOsDbContext _db;
    private readonly ILogger<DbSeedRunner> _logger;

    public DbSeedRunner(FamilyOsDbContext db, ILogger<DbSeedRunner> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        LogMigratingDatabase(_logger, null);
        await _db.Database.MigrateAsync(ct);
        LogMigrationsComplete(_logger, null);

        // Seed data will be added here in later phases (topics, default source)
    }
}
