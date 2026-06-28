using FamilyOs.Application.Settings;
using MediatR;

namespace FamilyOs.Api.Endpoints;

public static class SettingsModule
{
    public static void MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/settings").RequireAuthorization("RequireAdmin");

        // GET /api/v1/settings/system
        group.MapGet("/system", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetSystemSettingsQuery(), ct);
            return Results.Ok(result);
        });

        // PATCH /api/v1/settings/system
        group.MapMethods("/system", ["PATCH"], async (PatchSystemSettingsRequest req, ISender sender, CancellationToken ct) =>
        {
            SmtpSettingsDto? smtp = req.Smtp is not null
                ? new SmtpSettingsDto(req.Smtp.Host, req.Smtp.Port, req.Smtp.From)
                : null;

            await sender.Send(new PatchSystemSettingsCommand(smtp, req.AuditRetentionDays), ct);
            return Results.NoContent();
        });
    }
}

public record PatchSystemSettingsRequest(PatchSmtpRequest? Smtp, int? AuditRetentionDays);
public record PatchSmtpRequest(string? Host, int? Port, string? From);
