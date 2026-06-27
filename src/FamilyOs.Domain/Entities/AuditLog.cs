using FamilyOs.Domain.Enums;

namespace FamilyOs.Domain.Entities;

public sealed class AuditLog
{
    private AuditLog() { }

    public Guid Id { get; private set; }
    public DateTime OccurredUtc { get; private set; }
    public Guid? UserAccountId { get; private set; }
    public AuditAction Action { get; private set; }
    public string? EntityType { get; private set; }
    public Guid? EntityId { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? DetailsJson { get; private set; }

    public static AuditLog Create(
        AuditAction action,
        Guid? userAccountId,
        string? entityType = null,
        Guid? entityId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? detailsJson = null)
        => new()
        {
            Id = Guid.NewGuid(),
            OccurredUtc = DateTime.UtcNow,
            Action = action,
            UserAccountId = userAccountId,
            EntityType = entityType,
            EntityId = entityId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            DetailsJson = detailsJson,
        };
}
