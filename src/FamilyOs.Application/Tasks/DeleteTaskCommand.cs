using MediatR;

namespace FamilyOs.Application.Tasks;

public sealed record DeleteTaskCommand(Guid TaskId, Guid? UserId) : IRequest;
