using FamilyOs.Application.Sources;
using MediatR;

namespace FamilyOs.Api.Endpoints;

public static class SourcesModule
{
    public static void MapSourcesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/sources").RequireAuthorization("RequireAdmin");

        // GET /api/v1/sources
        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ListSourcesQuery(), ct);
            return Results.Ok(result);
        });

        // POST /api/v1/sources/gmail/connect
        group.MapPost("/gmail/connect", (CancellationToken ct) =>
        {
            var placeholder = new
            {
                redirectUrl = "https://accounts.google.com/o/oauth2/auth?client_id=MVP_PLACEHOLDER&scope=https://www.googleapis.com/auth/gmail.readonly&response_type=code&access_type=offline",
                note = "OAuth2 flow not yet implemented — configure credentials in appsettings.json and restart."
            };
            return Results.Ok(placeholder);
        });

        // DELETE /api/v1/sources/{id}
        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new DisconnectSourceCommand(id), ct);
            return Results.NoContent();
        });

        // POST /api/v1/sources/{id}/sync
        group.MapPost("/{id:guid}/sync", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SyncSourceCommand(id), ct);
            return Results.Accepted(null as string, result);
        });
    }
}
