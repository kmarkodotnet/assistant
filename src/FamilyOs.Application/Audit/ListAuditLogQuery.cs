using FamilyOs.Application.Common;
using MediatR;

namespace FamilyOs.Application.Audit;

public sealed record ListAuditLogQuery(
    DateTime? From,
    DateTime? To,
    Guid? UserAccountId,
    string? Action,
    string? EntityType,
    Guid? EntityId,
    int Page,
    int PageSize) : IRequest<PagedResult<AuditLogDto>>;
