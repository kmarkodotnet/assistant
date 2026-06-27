using FamilyOs.Application.Abstractions.Common;
using FamilyOs.Application.Reminders;
using FamilyOs.Application.Reminders.Actions;
using FamilyOs.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FamilyOs.Api.Endpoints;

public static class RemindersModule
{
    public static void MapRemindersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/reminders").RequireAuthorization("RequireAuthenticated");

        // GET /api/v1/reminders
        group.MapGet("/", async (
            [FromQuery] bool? upcoming,
            [FromQuery] string? status,
            ISender sender,
            ICurrentUserAccessor userAccessor,
            CancellationToken ct) =>
        {
            if (userAccessor.UserAccountId is null) return Results.Unauthorized();
            ReminderStatus? parsedStatus = Enum.TryParse<ReminderStatus>(status, true, out var s) ? s : null;
            var result = await sender.Send(new ListRemindersQuery(
                userAccessor.UserAccountId.Value,
                upcoming ?? false,
                parsedStatus), ct);
            return Results.Ok(result);
        });

        // GET /api/v1/reminders/{id}
        group.MapGet("/{id:guid}", async (
            Guid id,
            ISender sender,
            ICurrentUserAccessor userAccessor,
            CancellationToken ct) =>
        {
            if (userAccessor.UserAccountId is null) return Results.Unauthorized();
            var result = await sender.Send(new GetReminderQuery(id, userAccessor.UserAccountId.Value), ct);
            return Results.Ok(result);
        });

        // POST /api/v1/reminders
        group.MapPost("/", async (
            CreateReminderRequest req,
            ISender sender,
            ICurrentUserAccessor userAccessor,
            CancellationToken ct) =>
        {
            if (userAccessor.UserAccountId is null) return Results.Unauthorized();
            var result = await sender.Send(new CreateReminderCommand(
                req.TaskId,
                req.DeadlineId,
                req.TargetUserAccountId ?? userAccessor.UserAccountId.Value,
                req.Channel ?? NotificationChannel.InApp,
                req.TriggerUtc,
                req.RruleExpression,
                userAccessor.UserAccountId.Value), ct);
            return Results.Created($"/api/v1/reminders/{result.Id}", result);
        });

        // PATCH /api/v1/reminders/{id}
        group.MapMethods("/{id:guid}", ["PATCH"], async (
            Guid id,
            PatchReminderRequest req,
            ISender sender,
            ICurrentUserAccessor userAccessor,
            CancellationToken ct) =>
        {
            if (userAccessor.UserAccountId is null) return Results.Unauthorized();
            await sender.Send(new PatchReminderCommand(
                id,
                userAccessor.UserAccountId.Value,
                req.TriggerUtc,
                req.Channel), ct);
            return Results.Ok();
        });

        // DELETE /api/v1/reminders/{id}
        group.MapDelete("/{id:guid}", async (
            Guid id,
            ISender sender,
            ICurrentUserAccessor userAccessor,
            CancellationToken ct) =>
        {
            if (userAccessor.UserAccountId is null) return Results.Unauthorized();
            await sender.Send(new DeleteReminderCommand(id, userAccessor.UserAccountId.Value), ct);
            return Results.NoContent();
        });

        // POST /api/v1/reminders/{id}/acknowledge
        group.MapPost("/{id:guid}/acknowledge", async (
            Guid id,
            ISender sender,
            ICurrentUserAccessor userAccessor,
            CancellationToken ct) =>
        {
            if (userAccessor.UserAccountId is null) return Results.Unauthorized();
            await sender.Send(new AcknowledgeReminderCommand(id, userAccessor.UserAccountId.Value), ct);
            return Results.Ok();
        });

        // POST /api/v1/reminders/{id}/snooze
        group.MapPost("/{id:guid}/snooze", async (
            Guid id,
            SnoozeRequest req,
            ISender sender,
            ICurrentUserAccessor userAccessor,
            CancellationToken ct) =>
        {
            if (userAccessor.UserAccountId is null) return Results.Unauthorized();
            await sender.Send(new SnoozeReminderCommand(id, userAccessor.UserAccountId.Value, req.SnoozeMinutes), ct);
            return Results.Ok();
        });

        // POST /api/v1/reminders/{id}/skip
        group.MapPost("/{id:guid}/skip", async (
            Guid id,
            ISender sender,
            ICurrentUserAccessor userAccessor,
            CancellationToken ct) =>
        {
            if (userAccessor.UserAccountId is null) return Results.Unauthorized();
            await sender.Send(new SkipReminderCommand(id, userAccessor.UserAccountId.Value), ct);
            return Results.Ok();
        });

        // POST /api/v1/reminders/{id}/delegate
        group.MapPost("/{id:guid}/delegate", async (
            Guid id,
            DelegateRequest req,
            ISender sender,
            ICurrentUserAccessor userAccessor,
            CancellationToken ct) =>
        {
            if (userAccessor.UserAccountId is null) return Results.Unauthorized();
            await sender.Send(new DelegateReminderCommand(id, userAccessor.UserAccountId.Value, req.TargetUserAccountId), ct);
            return Results.Ok();
        });
    }
}

public record CreateReminderRequest(
    Guid? TaskId,
    Guid? DeadlineId,
    Guid? TargetUserAccountId,
    NotificationChannel? Channel,
    DateTime TriggerUtc,
    string? RruleExpression);

public record PatchReminderRequest(
    DateTime? TriggerUtc,
    NotificationChannel? Channel);

public record SnoozeRequest(int SnoozeMinutes);

public record DelegateRequest(Guid TargetUserAccountId);
