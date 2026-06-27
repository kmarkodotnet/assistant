using FamilyOs.Application.Deadlines.Dtos;
using MediatR;

namespace FamilyOs.Application.Deadlines;

public sealed record GetDeadlineQuery(Guid DeadlineId, Guid? UserId) : IRequest<DeadlineDto>;
