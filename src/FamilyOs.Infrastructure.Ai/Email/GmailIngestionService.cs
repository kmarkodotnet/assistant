using FamilyOs.Application.Abstractions.Email;
using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Abstractions;
using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FamilyOs.Infrastructure.Ai.Email;

public sealed partial class GmailIngestionService(
    IFamilyOsDbContext db,
    IAuditLogger auditLogger,
    ILogger<GmailIngestionService> logger)
    : IEmailIngestionService
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Source {SourceId} not found for Gmail sync.")]
    private static partial void LogSourceNotFound(ILogger logger, Guid sourceId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting Gmail sync for source {SourceId} ({Name}).")]
    private static partial void LogGmailSyncStarted(ILogger logger, Guid sourceId, string name);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Gmail sync completed for source {SourceId}: fetched={Fetched}, inserted={Inserted}, skipped={Skipped}.")]
    private static partial void LogGmailSyncCompleted(ILogger logger, Guid sourceId, int fetched, int inserted, int skipped);

    public async Task<EmailIngestionReport> SyncAsync(Guid sourceId, CancellationToken ct)
    {
        var source = await db.Sources
            .FirstOrDefaultAsync(s => s.Id == sourceId, ct);

        if (source is null)
        {
            LogSourceNotFound(logger, sourceId);
            return new EmailIngestionReport(0, 0, 0, $"Source {sourceId} not found.");
        }

        if (source.Kind != SourceKind.GmailAccount)
        {
            return new EmailIngestionReport(0, 0, 0, $"Source {sourceId} is not a Gmail account (Kind={source.Kind}).");
        }

        LogGmailSyncStarted(logger, sourceId, source.Name);

        // Gmail API is not yet configured — return a placeholder report.
        // Full implementation: parse ConfigJson for refresh token, call Gmail API with
        // q="label:family-os/import", insert new EmailMessage + AiProcessingJob(ExtractText) records.
        var report = new EmailIngestionReport(0, 0, 0, "Gmail API not configured — set up OAuth2 credentials in source config.");

        await auditLogger.LogAsync(
            AuditAction.ExternalApiCall,
            null,
            "Source",
            sourceId,
            detailsJson: $"{{\"fetched\":{report.Fetched},\"inserted\":{report.Inserted},\"skipped\":{report.Skipped}}}",
            ct: ct);

        source.UpdateLastSync();
        await db.SaveChangesAsync(ct);

        LogGmailSyncCompleted(logger, sourceId, report.Fetched, report.Inserted, report.Skipped);

        return report;
    }
}
