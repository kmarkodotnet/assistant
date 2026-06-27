using FamilyOs.Application.Abstractions.Common;
using FamilyOs.Application.Tasks;
using FamilyOs.Application.Tasks.Actions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using DomainTaskStatus = FamilyOs.Domain.Enums.TaskStatus;
using DomainPriority = FamilyOs.Domain.Enums.Priority;
using DomainOrigin = FamilyOs.Domain.Enums.Origin;

namespace FamilyOs.Api.Endpoints;

public static class TasksModule
{
    public static void MapTasksEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tasks").RequireAuthorization("RequireAuthenticated");

        // GET /api/v1/tasks
        group.MapGet("/", async (
            [FromQuery] string? status,
            [FromQuery] Guid? assignedToFamilyMemberId,
            [FromQuery] string? priority,
            [FromQuery] string? origin,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            ISender sender,
            ICurrentUserAccessor userAccessor,
            CancellationToken ct) =>
        {
            DomainTaskStatus? parsedStatus = Enum.TryParse<DomainTaskStatus>(status, true, out var s) ? s : null;
            DomainPriority? parsedPriority = Enum.TryParse<DomainPriority>(priority, true, out var p) ? p : null;
            DomainOrigin? parsedOrigin = Enum.TryParse<DomainOrigin>(origin, true, out var o) ? o : null;

            var result = await sender.Send(new ListTasksQuery(
                userAccessor.UserAccountId,
                parsedStatus,
                assignedToFamilyMemberId,
                parsedPriority,
                parsedOrigin,
                page ?? 1,
                pageSize ?? 50), ct);
            return Results.Ok(result);
        });

        // GET /api/v1/tasks/{id}
        group.MapGet("/{id:guid}", async (
            Guid id,
            ISender sender,
            ICurrentUserAccessor userAccessor,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetTaskQuery(id, userAccessor.UserAccountId), ct);
            return Results.Ok(result);
        });

        // POST /api/v1/tasks
        group.MapPost("/", async (
            CreateTaskRequest req,
            ISender sender,
            ICurrentUserAccessor userAccessor,
            CancellationToken ct) =>
        {
            if (userAccessor.UserAccountId is null) return Results.Unauthorized();
            var result = await sender.Send(new CreateTaskCommand(
                req.Title,
                req.Description,
                req.DueDateUtc,
                req.Priority ?? DomainPriority.Normal,
                req.AssignedToFamilyMemberId,
                req.IsPrivate ?? false,
                userAccessor.UserAccountId.Value), ct);
            return Results.Created($"/api/v1/tasks/{result.Id}", result);
        }).RequireAuthorization("RequireAdult");

        // PATCH /api/v1/tasks/{id}
        group.MapMethods("/{id:guid}", ["PATCH"], async (
            Guid id,
            PatchTaskRequest req,
            ISender sender,
            ICurrentUserAccessor userAccessor,
            CancellationToken ct) =>
        {
            await sender.Send(new PatchTaskCommand(
                id,
                userAccessor.UserAccountId,
                req.Title,
                req.Description,
                req.DueDateUtc,
                req.Priority,
                req.AssignedToFamilyMemberId,
                req.IsPrivate), ct);
            return Results.Ok();
        }).RequireAuthorization("RequireAdult");

        // DELETE /api/v1/tasks/{id}
        group.MapDelete("/{id:guid}", async (
            Guid id,
            ISender sender,
            ICurrentUserAccessor userAccessor,
            CancellationToken ct) =>
        {
            await sender.Send(new DeleteTaskCommand(id, userAccessor.UserAccountId), ct);
            return Results.NoContent();
        }).RequireAuthorization("RequireAdult");

        // POST /api/v1/tasks/{id}/approve
        group.MapPost("/{id:guid}/approve", async (
            Guid id,
            ISender sender,
            ICurrentUserAccessor userAccessor,
            CancellationToken ct) =>
        {
            if (userAccessor.UserAccountId is null) return Results.Unauthorized();
            await sender.Send(new ApproveTaskCommand(id, userAccessor.UserAccountId.Value), ct);
            return Results.Ok();
        }).RequireAuthorization("RequireAdult");

        // POST /api/v1/tasks/{id}/reject
        group.MapPost("/{id:guid}/reject", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            await sender.Send(new RejectTaskCommand(id), ct);
            return Results.Ok();
        }).RequireAuthorization("RequireAdult");

        // POST /api/v1/tasks/{id}/start
        group.MapPost("/{id:guid}/start", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            await sender.Send(new StartTaskCommand(id), ct);
            return Results.Ok();
        }).RequireAuthorization("RequireAdult");

        // POST /api/v1/tasks/{id}/complete
        group.MapPost("/{id:guid}/complete", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            await sender.Send(new CompleteTaskCommand(id), ct);
            return Results.Ok();
        }).RequireAuthorization("RequireAdult");

        // POST /api/v1/tasks/{id}/cancel
        group.MapPost("/{id:guid}/cancel", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            await sender.Send(new CancelTaskCommand(id), ct);
            return Results.Ok();
        }).RequireAuthorization("RequireAdult");
    }
}

public record CreateTaskRequest(
    string Title,
    string? Description,
    DateTime? DueDateUtc,
    DomainPriority? Priority,
    Guid? AssignedToFamilyMemberId,
    bool? IsPrivate);

public record PatchTaskRequest(
    string? Title,
    string? Description,
    DateTime? DueDateUtc,
    DomainPriority? Priority,
    Guid? AssignedToFamilyMemberId,
    bool? IsPrivate);
