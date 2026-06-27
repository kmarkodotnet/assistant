using MediatR;

namespace FamilyOs.Application.Tasks.Actions;

public sealed record CancelTaskCommand(Guid TaskId) : IRequest;
