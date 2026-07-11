using FamilyOs.Application.Abstractions.Common;
using FamilyOs.Application.Common.Ai;
using FamilyOs.Application.Search;
using FamilyOs.Application.Search.Dtos;
using FamilyOs.Application.Search.Saved;
using MediatR;
using System.Text.Json;

namespace FamilyOs.Api.Endpoints;

public static class SearchModule
{
    public static void MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/search").RequireAuthorization("RequireAuthenticated");

        // POST /api/v1/search
        group.MapPost("/", async (
            SearchRequest req,
            ISender sender,
            ICurrentUserAccessor userAccessor,
            ToolCallTokenOptions toolCallOptions,
            CancellationToken ct) =>
        {
            // Command mode gate (mvp-backlog.md E8): "FEATURE_NL_COMMANDS=false-szal
            // kikapcsolható" — mirrors the existing 501-stub convention used elsewhere in this
            // codebase (DocumentsModule tag/facet stubs) for not-yet-enabled capabilities.
            if (req.Mode == SearchMode.Command && !toolCallOptions.FeatureEnabled)
                return Results.StatusCode(501);

            var command = new SearchCommand(req, userAccessor.UserAccountId, userAccessor.FamilyMemberId, userAccessor.Role);
            var result = await sender.Send(command, ct);
            return Results.Ok(result);
        });

        // GET /api/v1/search/saved
        group.MapGet("/saved", async (
            ISender sender,
            ICurrentUserAccessor userAccessor,
            CancellationToken ct) =>
        {
            if (userAccessor.UserAccountId is null) return Results.Unauthorized();
            var result = await sender.Send(new ListSavedSearchesQuery(userAccessor.UserAccountId.Value), ct);
            return Results.Ok(result);
        });

        // POST /api/v1/search/saved
        group.MapPost("/saved", async (
            SaveSearchRequest req,
            ISender sender,
            ICurrentUserAccessor userAccessor,
            CancellationToken ct) =>
        {
            if (userAccessor.UserAccountId is null) return Results.Unauthorized();
            var queryJson = JsonSerializer.Serialize(req.Query);
            var result = await sender.Send(
                new SaveSearchCommand(req.Name, queryJson, userAccessor.UserAccountId.Value), ct);
            return Results.Created($"/api/v1/search/saved/{result.Id}", result);
        });

        // DELETE /api/v1/search/saved/{id}
        group.MapDelete("/saved/{id:guid}", async (
            Guid id,
            ISender sender,
            ICurrentUserAccessor userAccessor,
            CancellationToken ct) =>
        {
            if (userAccessor.UserAccountId is null) return Results.Unauthorized();
            await sender.Send(new DeleteSavedSearchCommand(id, userAccessor.UserAccountId.Value), ct);
            return Results.NoContent();
        });
    }
}

public record SaveSearchRequest(string Name, SearchRequest Query);
