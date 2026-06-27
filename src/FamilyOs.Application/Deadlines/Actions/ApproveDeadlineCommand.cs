using MediatR;

namespace FamilyOs.Application.Deadlines.Actions;

public sealed record ApproveDeadlineCommand(Guid DeadlineId, Guid ApprovedByUserId) : IRequest;
