using MediatR;

namespace FamilyOs.Application.Tasks.Actions;

public sealed record CompleteTaskCommand(Guid TaskId) : IRequest;
