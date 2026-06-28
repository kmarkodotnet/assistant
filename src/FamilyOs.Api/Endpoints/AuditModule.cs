using FamilyOs.Application.Abstractions.Common;
using FamilyOs.Application.Audit;
using FamilyOs.Application.Common.Abstractions;
using FamilyOs.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FamilyOs.Api.Endpoints;

public static class AuditModule
{
    public static void MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/audit-log").RequireAuthorization("RequireAdmin");

        // GET /api/v1/audit-log
        group.MapGet("/", async (
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] Guid? userAccountId,
            [FromQuery] string? action,
            [FromQuery] string? entityType,
            [FromQuery] Guid? entityId,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new ListAuditLogQuery(from, to, userAccountId, action, entityType, entityId, page ?? 1, pageSize ?? 50),
                ct);
            return Results.Ok(result);
        });

        // GET /api/v1/audit-log/security-events
        group.MapGet("/security-events", async (
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetSecurityEventsQuery(from, to), ct);
            return Results.Ok(result);
        });

        // GET /api/v1/audit-log/export
        group.MapGet("/export", async (
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string? format,
            ISender sender,
            IAuditLogger auditLogger,
            ICurrentUserAccessor currentUser,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new ExportAuditLogQuery(from, to, format ?? "csv"), ct);

            await auditLogger.LogAsync(
                AuditAction.FileAccess,
                currentUser.UserAccountId,
                "AuditLog",
                null,
                ct: ct);

            httpContext.Response.ContentType = result.ContentType;
            httpContext.Response.Headers.ContentDisposition = $"attachment; filename=\"{result.FileName}\"";

            await foreach (var line in result.Lines.WithCancellation(ct))
            {
                await httpContext.Response.WriteAsync(line + "\n", ct);
            }

            return Results.Empty;
        });
    }
}
