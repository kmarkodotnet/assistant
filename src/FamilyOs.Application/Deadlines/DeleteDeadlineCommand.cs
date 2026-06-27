using MediatR;

namespace FamilyOs.Application.Deadlines;

public sealed record DeleteDeadlineCommand(Guid DeadlineId, Guid? UserId) : IRequest;
