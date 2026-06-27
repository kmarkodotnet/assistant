using MediatR;

namespace FamilyOs.Application.Deadlines.Actions;

public sealed record DismissDeadlineCommand(Guid DeadlineId) : IRequest;
