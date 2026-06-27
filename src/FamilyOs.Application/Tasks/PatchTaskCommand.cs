using FamilyOs.Domain.Enums;
using MediatR;

namespace FamilyOs.Application.Tasks;

public sealed record PatchTaskCommand(
    Guid TaskId,
    Guid? UserId,
    string? Title,
    string? Description,
    DateTime? DueDateUtc,
    Priority? Priority,
    Guid? AssignedToFamilyMemberId,
    bool? IsPrivate
) : IRequest;
