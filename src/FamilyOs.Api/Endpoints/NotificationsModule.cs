using FamilyOs.Application.Abstractions.Common;
using FamilyOs.Application.Notifications;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FamilyOs.Api.Endpoints;

public static class NotificationsModule
{
    public static void MapNotificationsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/notifications").RequireAuthorization("RequireAuthenticated");

        // GET /api/v1/notifications
        group.MapGet("/", async (
            [FromQuery] bool? onlyUnread,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            ISender sender,
            ICurrentUserAccessor userAccessor,
            CancellationToken ct) =>
        {
            if (userAccessor.UserAccountId is null) return Results.Unauthorized();
            var result = await sender.Send(new GetNotificationFeedQuery(
                userAccessor.UserAccountId.Value,
                onlyUnread ?? false,
                page ?? 1,
                pageSize ?? 50), ct);
            return Results.Ok(result);
        });

        // POST /api/v1/notifications/{id}/read
        group.MapPost("/{id:guid}/read", async (
            Guid id,
            ISender sender,
            ICurrentUserAccessor userAccessor,
            CancellationToken ct) =>
        {
            if (userAccessor.UserAccountId is null) return Results.Unauthorized();
            await sender.Send(new MarkAsReadCommand(id, userAccessor.UserAccountId.Value), ct);
            return Results.Ok();
        });

        // POST /api/v1/notifications/read-all
        group.MapPost("/read-all", async (
            ISender sender,
            ICurrentUserAccessor userAccessor,
            CancellationToken ct) =>
        {
            if (userAccessor.UserAccountId is null) return Results.Unauthorized();
            await sender.Send(new MarkAllAsReadCommand(userAccessor.UserAccountId.Value), ct);
            return Results.Ok();
        });
    }
}
