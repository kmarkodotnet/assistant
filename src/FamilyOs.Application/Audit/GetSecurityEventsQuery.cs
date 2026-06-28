using MediatR;

namespace FamilyOs.Application.Audit;

public sealed record GetSecurityEventsQuery(DateTime? From, DateTime? To) : IRequest<IReadOnlyList<AuditLogDto>>;
