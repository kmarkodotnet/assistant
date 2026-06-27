using FamilyOs.Domain.Enums;

namespace FamilyOs.Application.Common.Abstractions;

public interface IAuditLogger
{
    Task LogAsync(
        AuditAction action,
        Guid? userAccountId,
        string? entityType = null,
        Guid? entityId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? detailsJson = null,
        CancellationToken ct = default);
}
