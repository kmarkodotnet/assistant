using FamilyOs.Application.Admin.AiJobs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FamilyOs.Api.Endpoints;

public static class AiJobsAdminModule
{
    public static void MapAiJobsAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai-jobs").RequireAuthorization("RequireAdmin");

        // GET /api/v1/ai-jobs
        group.MapGet("/", async (
            [FromQuery] string? status,
            [FromQuery] string? jobType,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new ListAiJobsQuery(status, jobType, page ?? 1, pageSize ?? 50), ct);
            return Results.Ok(result);
        });

        // GET /api/v1/ai-jobs/queue-stats
        group.MapGet("/queue-stats", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetAiJobQueueStatsQuery(), ct);
            return Results.Ok(result);
        });

        // POST /api/v1/ai-jobs/{id}/retry
        group.MapPost("/{id:guid}/retry", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new RetryAiJobCommand(id), ct);
            return Results.Ok();
        });

        // POST /api/v1/ai-jobs/{id}/cancel
        group.MapPost("/{id:guid}/cancel", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new CancelAiJobCommand(id), ct);
            return Results.Ok();
        });
    }
}
