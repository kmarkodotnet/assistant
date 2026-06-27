using FamilyOs.Application.Tasks.Dtos;
using MediatR;

namespace FamilyOs.Application.Tasks;

public sealed record GetTaskQuery(Guid TaskId, Guid? UserId) : IRequest<TaskDto>;
