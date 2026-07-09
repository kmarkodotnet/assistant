using FamilyOs.Application.Documents.AddDocumentTopic;
using FamilyOs.Application.Documents.DeleteDocument;
using FamilyOs.Application.Documents.DownloadDocument;
using FamilyOs.Application.Documents.GetDocumentClassification;
using FamilyOs.Application.Documents.GetDocumentDetail;
using FamilyOs.Application.Documents.GetDocumentText;
using FamilyOs.Application.Documents.ListDocuments;
using FamilyOs.Application.Documents.PatchDocument;
using FamilyOs.Application.Documents.RemoveDocumentTopic;
using FamilyOs.Application.Documents.ReprocessDocument;
using FamilyOs.Application.Documents.UpdateDocumentText;
using FamilyOs.Application.Documents.UploadDocument;
using FamilyOs.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FamilyOs.Api.Endpoints;

public static class DocumentsModule
{
    public static void MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/documents").RequireAuthorization("RequireAuthenticated");

        // POST /api/v1/documents (multipart upload)
        group.MapPost("/", async (HttpRequest req, ISender sender, CancellationToken ct) =>
        {
            if (!req.HasFormContentType)
                return Results.BadRequest("multipart/form-data szükséges.");

            var form = await req.ReadFormAsync(ct);
            var file = form.Files.GetFile("file");
            if (file is null)
                return Results.BadRequest("A 'file' mező kötelező.");

            var cmd = new UploadDocumentCommand(
                FileStream: file.OpenReadStream(),
                OriginalFileName: file.FileName,
                Title: form["title"].FirstOrDefault(),
                DocumentDate: DateOnly.TryParse(form["documentDate"].FirstOrDefault(), out var d) ? d : null,
                RelatedFamilyMemberId: Guid.TryParse(form["relatedFamilyMemberId"].FirstOrDefault(), out var rfm) ? rfm : null,
                IsPrivate: bool.TryParse(form["isPrivate"].FirstOrDefault(), out var priv) && priv
            );

            var dto = await sender.Send(cmd, ct);
            return Results.Created($"/api/v1/documents/{dto.Id}", dto);
        })
        .RequireAuthorization("RequireAdult")
        .DisableAntiforgery();

        // GET /api/v1/documents
        group.MapGet("/", async (
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] Guid? relatedFamilyMemberId,
            [FromQuery] ProcessingStatus? processingStatus,
            ISender sender, CancellationToken ct) =>
        {
            var dto = await sender.Send(
                new ListDocumentsQuery(page ?? 1, pageSize ?? 50, relatedFamilyMemberId, processingStatus), ct);
            return Results.Ok(dto);
        });

        // GET /api/v1/documents/{id}
        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var dto = await sender.Send(new GetDocumentDetailQuery(id), ct);
            return Results.Ok(dto);
        });

        // GET /api/v1/documents/{id}/classification
        group.MapGet("/{id:guid}/classification", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var dto = await sender.Send(new GetDocumentClassificationQuery(id), ct);
            return Results.Ok(dto);
        });

        // GET /api/v1/documents/{id}/content
        group.MapGet("/{id:guid}/content", async (Guid id, ISender sender, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await sender.Send(new DownloadDocumentQuery(id), ct);
            ctx.Response.Headers.ContentDisposition = $"inline; filename=\"{result.FileName}\"";
            return Results.Stream(result.Stream, result.MimeType);
        });

        // GET /api/v1/documents/{id}/text
        group.MapGet("/{id:guid}/text", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var dto = await sender.Send(new GetDocumentTextQuery(id), ct);
            return Results.Ok(dto);
        });

        // PATCH /api/v1/documents/{id}/text
        group.MapMethods("/{id:guid}/text", ["PATCH"], async (Guid id, UpdateDocumentTextRequest req, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new UpdateDocumentTextCommand(id, req.Content), ct);
            return Results.Ok();
        }).RequireAuthorization("RequireAdult");

        // PATCH /api/v1/documents/{id}
        group.MapMethods("/{id:guid}", ["PATCH"], async (Guid id, PatchDocumentRequest req, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new PatchDocumentCommand(id, req.Title, req.DocumentDate, req.RelatedFamilyMemberId, req.IsPrivate, req.RowVersion), ct);
            return Results.Ok();
        }).RequireAuthorization("RequireAdult");

        // DELETE /api/v1/documents/{id}
        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new DeleteDocumentCommand(id, false), ct);
            return Results.NoContent();
        }).RequireAuthorization("RequireAdult");

        // POST /api/v1/documents/{id}/reprocess
        group.MapPost("/{id:guid}/reprocess", async (Guid id, ReprocessDocumentRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ReprocessDocumentCommand(id, req.Jobs ?? []), ct);
            return Results.Ok(result);
        }).RequireAuthorization("RequireAdult");

        // Tag stubs (T-CBE-17)
        group.MapPost("/{id:guid}/tags", () => Results.StatusCode(501)).RequireAuthorization("RequireAdult");
        group.MapDelete("/{id:guid}/tags/{tagId:guid}", () => Results.StatusCode(501)).RequireAuthorization("RequireAdult");

        // POST /api/v1/documents/{id}/topics
        group.MapPost("/{id:guid}/topics", async (Guid id, AddDocumentTopicRequest req, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new AddDocumentTopicCommand(id, req.TopicId), ct);
            return Results.Ok();
        }).RequireAuthorization("RequireAdult");

        // DELETE /api/v1/documents/{id}/topics/{topicId}
        group.MapDelete("/{id:guid}/topics/{topicId:guid}", async (Guid id, Guid topicId, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new RemoveDocumentTopicCommand(id, topicId), ct);
            return Results.NoContent();
        }).RequireAuthorization("RequireAdult");

        // Facet PATCH stubs (T-CBE-18)
        group.MapMethods("/{id:guid}/warranty", ["PATCH"], () => Results.StatusCode(501)).RequireAuthorization("RequireAdult");
        group.MapMethods("/{id:guid}/medical-record", ["PATCH"], () => Results.StatusCode(501)).RequireAuthorization("RequireAdult");
        group.MapMethods("/{id:guid}/financial-record", ["PATCH"], () => Results.StatusCode(501)).RequireAuthorization("RequireAdult");
    }
}

public record UpdateDocumentTextRequest(string Content);
public record PatchDocumentRequest(string? Title, DateOnly? DocumentDate, Guid? RelatedFamilyMemberId, bool? IsPrivate, string? RowVersion);
public record ReprocessDocumentRequest(IReadOnlyList<string>? Jobs);
public record AddDocumentTopicRequest(Guid TopicId);
