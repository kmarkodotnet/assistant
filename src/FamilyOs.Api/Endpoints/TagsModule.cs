using FamilyOs.Application.Abstractions.Common;
using FamilyOs.Application.Tags;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FamilyOs.Api.Endpoints;

public static class TagsModule
{
    public static void MapTagsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tags").RequireAuthorization("RequireAuthenticated");

        // GET /api/v1/tags?q=&sort=usageCount:desc
        group.MapGet("/", async (
            [FromQuery] string? q,
            [FromQuery] string? sort,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new ListTagsQuery(q, sort, page ?? 1, pageSize ?? 100), ct);
            return Results.Ok(result);
        });

        // POST /api/v1/tags
        group.MapPost("/", async (
            CreateTagRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateTagCommand(req.Name, req.Color), ct);
            return Results.Created($"/api/v1/tags/{result.Id}", result);
        }).RequireAuthorization("RequireAdult");

        // PATCH /api/v1/tags/{id}
        group.MapMethods("/{id:guid}", ["PATCH"], async (
            Guid id,
            PatchTagRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            await sender.Send(new PatchTagCommand(id, req.Name, req.Color), ct);
            return Results.Ok();
        }).RequireAuthorization("RequireAdult");

        // DELETE /api/v1/tags/{id}?force=true
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromQuery] bool? force,
            ISender sender,
            CancellationToken ct) =>
        {
            await sender.Send(new DeleteTagCommand(id, force ?? false), ct);
            return Results.NoContent();
        }).RequireAuthorization("RequireAdmin");
    }
}

public record CreateTagRequest(string Name, string? Color);
public record PatchTagRequest(string? Name, string? Color);
