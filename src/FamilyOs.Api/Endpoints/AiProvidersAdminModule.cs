using FamilyOs.Application.Admin.AiProviders;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FamilyOs.Api.Endpoints;

public static class AiProvidersAdminModule
{
    public static void MapAiProvidersAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai-providers").RequireAuthorization("RequireAdmin");

        // GET /api/v1/ai-providers
        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ListAiProvidersQuery(), ct);
            return Results.Ok(result);
        });

        // PATCH /api/v1/ai-providers/{name}
        group.MapMethods("/{name}", ["PATCH"], async (
            string name,
            PatchAiProviderRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            await sender.Send(new PatchAiProviderCommand(name, req.Enabled, req.Model), ct);
            return Results.NoContent();
        });
    }
}

public record PatchAiProviderRequest(bool? Enabled, string? Model);
