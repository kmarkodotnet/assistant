using MediatR;

namespace FamilyOs.Application.Dashboard;

public sealed record GetDashboardQuery(Guid UserId) : IRequest<DashboardDto>;
