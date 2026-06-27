using FamilyOs.Application.Deadlines.Dtos;
using FamilyOs.Domain.Enums;
using MediatR;

namespace FamilyOs.Application.Deadlines;

public sealed record ListDeadlinesQuery(
    Guid? UserId,
    DateOnly? From,
    DateOnly? To,
    DeadlineCategory? Category,
    DeadlineStatus? Status,
    int Page = 1,
    int PageSize = 50
) : IRequest<IReadOnlyList<DeadlineListItemDto>>;
