using FamilyOs.Application.Tasks.Dtos;
using FamilyOs.Domain.Enums;
using MediatR;

namespace FamilyOs.Application.Tasks;

public sealed record CreateTaskCommand(
    string Title,
    string? Description,
    DateTime? DueDateUtc,
    Priority Priority,
    Guid? AssignedToFamilyMemberId,
    bool IsPrivate,
    Guid CreatedByUserAccountId
) : IRequest<TaskDto>;
