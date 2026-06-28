namespace FamilyOs.Application.Abstractions.Email;

public sealed record EmailIngestionReport(int Fetched, int Inserted, int Skipped, string? Error);

public interface IEmailIngestionService
{
    Task<EmailIngestionReport> SyncAsync(Guid sourceId, CancellationToken ct);
}
