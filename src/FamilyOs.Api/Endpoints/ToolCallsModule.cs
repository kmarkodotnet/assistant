using FamilyOs.Application.Common.Ai;
using FamilyOs.Application.ToolCalls;
using MediatR;

namespace FamilyOs.Api.Endpoints;

/// <summary>api-design.md §16.3 — tool-call proposal confirm/reject (CR260710-07).</summary>
public static class ToolCallsModule
{
    public static void MapToolCallsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tool-calls").RequireAuthorization("RequireAdult");

        // POST /api/v1/tool-calls/confirm
        group.MapPost("/confirm", async (
            ConfirmToolCallRequest req,
            ISender sender,
            ToolCallTokenOptions toolCallOptions,
            CancellationToken ct) =>
        {
            if (!toolCallOptions.FeatureEnabled)
                return Results.StatusCode(501);

            var result = await sender.Send(new ConfirmToolCallCommand(req.ProposalToken), ct);
            return Results.Ok(result);
        });

        // POST /api/v1/tool-calls/reject
        group.MapPost("/reject", async (
            RejectToolCallRequest req,
            ISender sender,
            ToolCallTokenOptions toolCallOptions,
            CancellationToken ct) =>
        {
            if (!toolCallOptions.FeatureEnabled)
                return Results.StatusCode(501);

            await sender.Send(new RejectToolCallCommand(req.ProposalToken, req.Reason), ct);
            return Results.NoContent();
        });
    }
}

public record ConfirmToolCallRequest(string ProposalToken);
public record RejectToolCallRequest(string ProposalToken, string? Reason);
