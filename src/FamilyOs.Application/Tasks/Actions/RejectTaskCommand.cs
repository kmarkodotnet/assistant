using MediatR;

namespace FamilyOs.Application.Tasks.Actions;

public sealed record RejectTaskCommand(Guid TaskId) : IRequest;
