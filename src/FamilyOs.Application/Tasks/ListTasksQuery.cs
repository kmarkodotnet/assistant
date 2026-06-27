using FamilyOs.Application.Tasks.Dtos;
using MediatR;
using DomainTaskStatus = FamilyOs.Domain.Enums.TaskStatus;
using DomainPriority = FamilyOs.Domain.Enums.Priority;
using DomainOrigin = FamilyOs.Domain.Enums.Origin;

namespace FamilyOs.Application.Tasks;

public sealed record ListTasksQuery(
    Guid? UserId,
    DomainTaskStatus? Status,
    Guid? AssignedToFamilyMemberId,
    DomainPriority? Priority,
    DomainOrigin? Origin,
    int Page = 1,
    int PageSize = 50
) : IRequest<IReadOnlyList<TaskListItemDto>>;
