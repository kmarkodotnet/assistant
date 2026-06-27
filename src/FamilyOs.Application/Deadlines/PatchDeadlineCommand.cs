using FamilyOs.Domain.Enums;
using MediatR;

namespace FamilyOs.Application.Deadlines;

public sealed record PatchDeadlineCommand(
    Guid DeadlineId,
    Guid? UserId,
    string? Title,
    string? Description,
    DateTime? DueDateUtc,
    DeadlineCategory? Category,
    Guid? RelatedFamilyMemberId,
    bool? IsPrivate
) : IRequest;
