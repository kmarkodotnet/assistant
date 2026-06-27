using FamilyOs.Application.Abstractions.Common;
using FamilyOs.Application.Suggestions;
using MediatR;

namespace FamilyOs.Api.Endpoints;

public static class SuggestionsModule
{
    public static void MapSuggestionsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/suggestions").RequireAuthorization("RequireAuthenticated");

        // GET /api/v1/suggestions
        group.MapGet("/", async (
            ISender sender,
            ICurrentUserAccessor userAccessor,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetSuggestionsQuery(userAccessor.UserAccountId), ct);
            return Results.Ok(result);
        });

        // POST /api/v1/suggestions/batch
        group.MapPost("/batch", async (
            BatchApproveRequest req,
            ISender sender,
            ICurrentUserAccessor userAccessor,
            CancellationToken ct) =>
        {
            if (userAccessor.UserAccountId is null) return Results.Unauthorized();
            var items = req.Items.Select(i => new BatchApproveItem(i.EntityType, i.Id, i.Action)).ToList();
            var result = await sender.Send(new BatchApproveCommand(items, userAccessor.UserAccountId.Value), ct);
            return Results.Ok(result);
        }).RequireAuthorization("RequireAdult");
    }
}

public record BatchApproveItemRequest(string EntityType, Guid Id, string Action);
public record BatchApproveRequest(IReadOnlyList<BatchApproveItemRequest> Items);
