using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;

namespace FamilyOs.Application.Audit;

public sealed class ExportAuditLogQueryHandler(IFamilyOsDbContext db)
    : IRequestHandler<ExportAuditLogQuery, AuditExportResult>
{
    public async Task<AuditExportResult> Handle(ExportAuditLogQuery request, CancellationToken cancellationToken)
    {
        var query = db.AuditLogs.AsNoTracking();

        if (request.From.HasValue)
            query = query.Where(l => l.OccurredUtc >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(l => l.OccurredUtc <= request.To.Value);

        var logs = await query
            .OrderByDescending(l => l.OccurredUtc)
            .ToListAsync(cancellationToken);

        var isCsv = !string.Equals(request.Format, "json", StringComparison.OrdinalIgnoreCase);
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);

        if (isCsv)
        {
            return new AuditExportResult(
                "text/csv",
                $"audit-log-{stamp}.csv",
                GenerateCsvLines(logs));
        }

        return new AuditExportResult(
            "application/json",
            $"audit-log-{stamp}.json",
            GenerateJsonLines(logs));
    }

    private static async IAsyncEnumerable<string> GenerateCsvLines(List<AuditLog> logs)
    {
        yield return "Id,OccurredUtc,UserAccountId,Action,EntityType,EntityId,IpAddress,UserAgent,DetailsJson";
        foreach (var log in logs)
        {
            yield return string.Join(",",
                log.Id,
                log.OccurredUtc.ToString("O"),
                log.UserAccountId?.ToString() ?? "",
                log.Action.ToString(),
                EscapeCsv(log.EntityType),
                log.EntityId?.ToString() ?? "",
                EscapeCsv(log.IpAddress),
                EscapeCsv(log.UserAgent),
                EscapeCsv(log.DetailsJson));
        }
        await Task.CompletedTask;
    }

    private static async IAsyncEnumerable<string> GenerateJsonLines(List<AuditLog> logs)
    {
        yield return "[";
        for (var i = 0; i < logs.Count; i++)
        {
            var log = logs[i];
            var dto = new AuditLogDto(
                log.Id,
                log.OccurredUtc,
                log.UserAccountId,
                log.Action.ToString(),
                log.EntityType,
                log.EntityId,
                log.IpAddress,
                log.UserAgent,
                log.DetailsJson);
            var json = JsonSerializer.Serialize(dto);
            yield return i < logs.Count - 1 ? json + "," : json;
        }
        yield return "]";
        await Task.CompletedTask;
    }

    private static string EscapeCsv(string? value)
    {
        if (value is null) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
