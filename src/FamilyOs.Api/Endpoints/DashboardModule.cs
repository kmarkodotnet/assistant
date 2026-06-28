using FamilyOs.Application.Abstractions.Common;
using FamilyOs.Application.Dashboard;
using MediatR;

namespace FamilyOs.Api.Endpoints;

public static class DashboardModule
{
    public static void MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/dashboard").RequireAuthorization("RequireAuthenticated");

        // GET /api/v1/dashboard
        group.MapGet("/", async (
            ISender sender,
            ICurrentUserAccessor userAccessor,
            CancellationToken ct) =>
        {
            if (userAccessor.UserAccountId is null) return Results.Unauthorized();
            var result = await sender.Send(new GetDashboardQuery(userAccessor.UserAccountId.Value), ct);
            return Results.Ok(result);
        });
    }
}
