namespace FamilyOs.Application.Audit;

public sealed record AuditLogDto(
    Guid Id,
    DateTime OccurredUtc,
    Guid? UserAccountId,
    string Action,
    string? EntityType,
    Guid? EntityId,
    string? IpAddress,
    string? UserAgent,
    string? DetailsJson);
