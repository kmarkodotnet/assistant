using FamilyOs.Application.Abstractions.Common;
using FamilyOs.Application.Deadlines;
using FamilyOs.Application.Deadlines.Actions;
using FamilyOs.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FamilyOs.Api.Endpoints;

public static class DeadlinesModule
{
    public static void MapDeadlinesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/deadlines").RequireAuthorization("RequireAuthenticated");

        // GET /api/v1/deadlines
        group.MapGet("/", async (
            [FromQuery] DateOnly? from,
            [FromQuery] DateOnly? to,
            [FromQuery] DeadlineCategory? category,
            [FromQuery] DeadlineStatus? status,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            ISender sender,
            ICurrentUserAccessor userAccessor,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new ListDeadlinesQuery(
                userAccessor.UserAccountId,
                from,
                to,
                category,
                status,
                page ?? 1,
                pageSize ?? 50), ct);
            return Results.Ok(result);
        });

        // GET /api/v1/deadlines/{id}
        group.MapGet("/{id:guid}", async (
            Guid id,
            ISender sender,
            ICurrentUserAccessor userAccessor,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetDeadlineQuery(id, userAccessor.UserAccountId), ct);
            return Results.Ok(result);
        });

        // POST /api/v1/deadlines
        group.MapPost("/", async (
            CreateDeadlineRequest req,
            ISender sender,
            ICurrentUserAccessor userAccessor,
            CancellationToken ct) =>
        {
            if (userAccessor.UserAccountId is null) return Results.Unauthorized();
            var result = await sender.Send(new CreateDeadlineCommand(
                req.Title,
                req.Description,
                req.DueDateUtc,
                req.Category ?? DeadlineCategory.Other,
                req.RelatedFamilyMemberId,
                req.IsPrivate ?? false,
                userAccessor.UserAccountId.Value), ct);
            return Results.Created($"/api/v1/deadlines/{result.Id}", result);
        }).RequireAuthorization("RequireAdult");

        // PATCH /api/v1/deadlines/{id}
        group.MapMethods("/{id:guid}", ["PATCH"], async (
            Guid id,
            PatchDeadlineRequest req,
            ISender sender,
            ICurrentUserAccessor userAccessor,
            CancellationToken ct) =>
        {
            await sender.Send(new PatchDeadlineCommand(
                id,
                userAccessor.UserAccountId,
                req.Title,
                req.Description,
                req.DueDateUtc,
                req.Category,
                req.RelatedFamilyMemberId,
                req.IsPrivate), ct);
            return Results.Ok();
        }).RequireAuthorization("RequireAdult");

        // DELETE /api/v1/deadlines/{id}
        group.MapDelete("/{id:guid}", async (
            Guid id,
            ISender sender,
            ICurrentUserAccessor userAccessor,
            CancellationToken ct) =>
        {
            await sender.Send(new DeleteDeadlineCommand(id, userAccessor.UserAccountId), ct);
            return Results.NoContent();
        }).RequireAuthorization("RequireAdult");

        // POST /api/v1/deadlines/{id}/approve
        group.MapPost("/{id:guid}/approve", async (
            Guid id,
            ISender sender,
            ICurrentUserAccessor userAccessor,
            CancellationToken ct) =>
        {
            if (userAccessor.UserAccountId is null) return Results.Unauthorized();
            await sender.Send(new ApproveDeadlineCommand(id, userAccessor.UserAccountId.Value), ct);
            return Results.Ok();
        }).RequireAuthorization("RequireAdult");

        // POST /api/v1/deadlines/{id}/resolve
        group.MapPost("/{id:guid}/resolve", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            await sender.Send(new ResolveDeadlineCommand(id), ct);
            return Results.Ok();
        }).RequireAuthorization("RequireAdult");

        // POST /api/v1/deadlines/{id}/dismiss
        group.MapPost("/{id:guid}/dismiss", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            await sender.Send(new DismissDeadlineCommand(id), ct);
            return Results.Ok();
        }).RequireAuthorization("RequireAdult");
    }
}

public record CreateDeadlineRequest(
    string Title,
    string? Description,
    DateTime DueDateUtc,
    DeadlineCategory? Category,
    Guid? RelatedFamilyMemberId,
    bool? IsPrivate);

public record PatchDeadlineRequest(
    string? Title,
    string? Description,
    DateTime? DueDateUtc,
    DeadlineCategory? Category,
    Guid? RelatedFamilyMemberId,
    bool? IsPrivate);
