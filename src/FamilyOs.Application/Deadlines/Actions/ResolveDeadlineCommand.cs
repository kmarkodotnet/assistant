using MediatR;

namespace FamilyOs.Application.Deadlines.Actions;

public sealed record ResolveDeadlineCommand(Guid DeadlineId) : IRequest;
