using FamilyOs.Application.Deadlines.Dtos;
using FamilyOs.Domain.Enums;
using MediatR;

namespace FamilyOs.Application.Deadlines;

public sealed record CreateDeadlineCommand(
    string Title,
    string? Description,
    DateTime DueDateUtc,
    DeadlineCategory Category,
    Guid? RelatedFamilyMemberId,
    bool IsPrivate,
    Guid CreatedByUserAccountId
) : IRequest<DeadlineDto>;
