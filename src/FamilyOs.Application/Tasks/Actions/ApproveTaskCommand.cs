using MediatR;

namespace FamilyOs.Application.Tasks.Actions;

public sealed record ApproveTaskCommand(Guid TaskId, Guid ApprovedByUserId) : IRequest;
