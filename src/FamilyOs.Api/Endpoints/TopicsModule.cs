using FamilyOs.Application.Topics;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FamilyOs.Api.Endpoints;

public static class TopicsModule
{
    public static void MapTopicsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/topics").RequireAuthorization("RequireAuthenticated");

        // GET /api/v1/topics?flat=true
        group.MapGet("/", async (
            [FromQuery] bool? flat,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new ListTopicsQuery(flat ?? false), ct);
            return Results.Ok(result);
        });

        // POST /api/v1/topics — Admin only
        group.MapPost("/", async (
            CreateTopicRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateTopicCommand(
                req.Name,
                req.Slug,
                req.ParentId,
                req.Icon,
                req.SortOrder ?? 0), ct);
            return Results.Created($"/api/v1/topics/{result.Id}", result);
        }).RequireAuthorization("RequireAdmin");

        // PATCH /api/v1/topics/{id} — Admin only
        group.MapMethods("/{id:guid}", ["PATCH"], async (
            Guid id,
            PatchTopicRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            await sender.Send(new PatchTopicCommand(id, req.Name, req.Icon, req.SortOrder), ct);
            return Results.Ok();
        }).RequireAuthorization("RequireAdmin");

        // DELETE /api/v1/topics/{id} — Admin only
        group.MapDelete("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            await sender.Send(new DeleteTopicCommand(id), ct);
            return Results.NoContent();
        }).RequireAuthorization("RequireAdmin");
    }
}

public record CreateTopicRequest(string Name, string Slug, Guid? ParentId, string? Icon, int? SortOrder);
public record PatchTopicRequest(string? Name, string? Icon, int? SortOrder);
