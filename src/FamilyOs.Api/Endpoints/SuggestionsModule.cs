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

            var items = new List<BatchApproveItem>();

            foreach (var id in req.Approve?.Tasks ?? [])
                items.Add(new BatchApproveItem("task", Guid.Parse(id), "approve"));
            foreach (var id in req.Approve?.Deadlines ?? [])
                items.Add(new BatchApproveItem("deadline", Guid.Parse(id), "approve"));

            foreach (var id in req.Reject?.Tasks ?? [])
                items.Add(new BatchApproveItem("task", Guid.Parse(id), "reject"));
            foreach (var id in req.Reject?.Deadlines ?? [])
                items.Add(new BatchApproveItem("deadline", Guid.Parse(id), "reject"));

            var result = await sender.Send(new BatchApproveCommand(items, userAccessor.UserAccountId.Value), ct);
            return Results.Ok(result);
        }).RequireAuthorization("RequireAdult");
    }
}

public record BatchApproveCategories(
    IReadOnlyList<string>? Tasks,
    IReadOnlyList<string>? Deadlines);

public record BatchApproveRequest(
    BatchApproveCategories? Approve,
    BatchApproveCategories? Reject);
