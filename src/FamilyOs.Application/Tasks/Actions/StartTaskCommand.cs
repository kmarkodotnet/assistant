using MediatR;

namespace FamilyOs.Application.Tasks.Actions;

public sealed record StartTaskCommand(Guid TaskId) : IRequest;
