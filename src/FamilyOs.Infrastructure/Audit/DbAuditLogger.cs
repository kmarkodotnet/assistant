using FamilyOs.Application.Common.Abstractions;
using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using FamilyOs.Infrastructure.Persistence;

namespace FamilyOs.Infrastructure.Audit;

public sealed class DbAuditLogger(FamilyOsDbContext db) : IAuditLogger
{
    public async Task LogAsync(
        AuditAction action,
        Guid? userAccountId,
        string? entityType = null,
        Guid? entityId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? detailsJson = null,
        CancellationToken ct = default)
    {
        var log = AuditLog.Create(
            action,
            userAccountId,
            entityType,
            entityId,
            ipAddress,
            userAgent,
            detailsJson);

        db.AuditLogs.Add(log);
        await db.SaveChangesAsync(ct);
    }
}
