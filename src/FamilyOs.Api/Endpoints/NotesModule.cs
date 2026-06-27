using FamilyOs.Application.Abstractions.Common;
using FamilyOs.Application.Notes;
using FamilyOs.Application.Notes.Common;
using FamilyOs.Application.Notes.Linking;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FamilyOs.Api.Endpoints;

public static class NotesModule
{
    public static void MapNotesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/notes").RequireAuthorization("RequireAuthenticated");

        // GET /api/v1/notes
        group.MapGet("/", async (
            [FromQuery] Guid? relatedFamilyMemberId,
            [FromQuery] Guid? tagId,
            [FromQuery] string? topicSlug,
            [FromQuery] bool? includeBody,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            ISender sender,
            ICurrentUserAccessor userAccessor,
            CancellationToken ct) =>
        {
            if (userAccessor.UserAccountId is null) return Results.Unauthorized();
            var result = await sender.Send(new ListNotesQuery(
                userAccessor.UserAccountId.Value,
                relatedFamilyMemberId,
                tagId,
                topicSlug,
                includeBody ?? false,
                page ?? 1,
                pageSize ?? 50), ct);
            return Results.Ok(result);
        });

        // POST /api/v1/notes
        group.MapPost("/", async (
            CreateNoteRequest req,
            ISender sender,
            ICurrentUserAccessor userAccessor,
            CancellationToken ct) =>
        {
            if (userAccessor.UserAccountId is null) return Results.Unauthorized();
            var result = await sender.Send(new CreateNoteCommand(
                req.Title,
                req.Body,
                userAccessor.UserAccountId.Value,
                req.RelatedFamilyMemberId,
                req.IsPrivate ?? false), ct);
            return Results.Created($"/api/v1/notes/{result.Id}", result);
        });

        // GET /api/v1/notes/{id}
        group.MapGet("/{id:guid}", async (
            Guid id,
            ISender sender,
            ICurrentUserAccessor userAccessor,
            CancellationToken ct) =>
        {
            if (userAccessor.UserAccountId is null) return Results.Unauthorized();
            var result = await sender.Send(new GetNoteQuery(id, userAccessor.UserAccountId.Value), ct);
            return Results.Ok(result);
        });

        // PATCH /api/v1/notes/{id}
        group.MapMethods("/{id:guid}", ["PATCH"], async (
            Guid id,
            PatchNoteRequest req,
            ISender sender,
            ICurrentUserAccessor userAccessor,
            CancellationToken ct) =>
        {
            if (userAccessor.UserAccountId is null) return Results.Unauthorized();
            await sender.Send(new PatchNoteCommand(
                id,
                userAccessor.UserAccountId.Value,
                req.Title,
                req.Body), ct);
            return Results.Ok();
        });

        // DELETE /api/v1/notes/{id}
        group.MapDelete("/{id:guid}", async (
            Guid id,
            ISender sender,
            ICurrentUserAccessor userAccessor,
            CancellationToken ct) =>
        {
            if (userAccessor.UserAccountId is null) return Results.Unauthorized();
            await sender.Send(new DeleteNoteCommand(id, userAccessor.UserAccountId.Value), ct);
            return Results.NoContent();
        });

        // POST /api/v1/notes/{id}/tags
        group.MapPost("/{id:guid}/tags", async (
            Guid id,
            AddTagRequest req,
            ISender sender,
            ICurrentUserAccessor userAccessor,
            CancellationToken ct) =>
        {
            if (userAccessor.UserAccountId is null) return Results.Unauthorized();
            await sender.Send(new AddNoteTagCommand(id, req.TagId, userAccessor.UserAccountId.Value), ct);
            return Results.Ok();
        });

        // DELETE /api/v1/notes/{id}/tags/{tagId}
        group.MapDelete("/{id:guid}/tags/{tagId:guid}", async (
            Guid id,
            Guid tagId,
            ISender sender,
            ICurrentUserAccessor userAccessor,
            CancellationToken ct) =>
        {
            if (userAccessor.UserAccountId is null) return Results.Unauthorized();
            await sender.Send(new RemoveNoteTagCommand(id, tagId, userAccessor.UserAccountId.Value), ct);
            return Results.NoContent();
        });

        // POST /api/v1/notes/{id}/topics
        group.MapPost("/{id:guid}/topics", async (
            Guid id,
            AddTopicRequest req,
            ISender sender,
            ICurrentUserAccessor userAccessor,
            CancellationToken ct) =>
        {
            if (userAccessor.UserAccountId is null) return Results.Unauthorized();
            await sender.Send(new AddNoteTopicCommand(id, req.TopicId, userAccessor.UserAccountId.Value), ct);
            return Results.Ok();
        });

        // DELETE /api/v1/notes/{id}/topics/{topicId}
        group.MapDelete("/{id:guid}/topics/{topicId:guid}", async (
            Guid id,
            Guid topicId,
            ISender sender,
            ICurrentUserAccessor userAccessor,
            CancellationToken ct) =>
        {
            if (userAccessor.UserAccountId is null) return Results.Unauthorized();
            await sender.Send(new RemoveNoteTopicCommand(id, topicId, userAccessor.UserAccountId.Value), ct);
            return Results.NoContent();
        });

        // GET /api/v1/notes/{id}/rendered
        group.MapGet("/{id:guid}/rendered", async (
            Guid id,
            ISender sender,
            ICurrentUserAccessor userAccessor,
            IMarkdownSanitizer sanitizer,
            CancellationToken ct) =>
        {
            if (userAccessor.UserAccountId is null) return Results.Unauthorized();
            var note = await sender.Send(new GetNoteQuery(id, userAccessor.UserAccountId.Value), ct);
            var html = sanitizer.Sanitize(note.Body);
            return Results.Content(html, "text/html");
        });
    }
}

public record CreateNoteRequest(
    string Title,
    string Body,
    Guid? RelatedFamilyMemberId,
    bool? IsPrivate);

public record PatchNoteRequest(
    string? Title,
    string? Body);

public record AddTagRequest(Guid TagId);
public record AddTopicRequest(Guid TopicId);
