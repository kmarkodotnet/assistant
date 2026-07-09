using FamilyOs.Application.Sources;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

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

        // POST /api/v1/sources/gmail/connect  — builds OAuth2 authorization URL
        group.MapPost("/gmail/connect", (IConfiguration config, HttpContext ctx) =>
        {
            var clientId = config["Gmail:ClientId"];
            var redirectUri = config["Gmail:RedirectUri"];

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(redirectUri))
                return Results.Problem(
                    "Gmail OAuth nincs konfigurálva. Állítsd be a Gmail:ClientId és Gmail:RedirectUri értékeket.",
                    statusCode: 503);

            var state = Guid.NewGuid().ToString("N");
            ctx.Response.Cookies.Append("gmail_oauth_state", state, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = ctx.Request.IsHttps,
                MaxAge = TimeSpan.FromMinutes(10),
            });

            var url = "https://accounts.google.com/o/oauth2/v2/auth" +
                      $"?client_id={Uri.EscapeDataString(clientId)}" +
                      $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                      $"&scope={Uri.EscapeDataString("https://www.googleapis.com/auth/gmail.readonly")}" +
                      "&response_type=code" +
                      "&access_type=offline" +
                      "&prompt=consent" +
                      $"&state={state}";

            return Results.Ok(new { redirectUrl = url });
        });

        // GET /api/v1/sources/gmail/callback  — OAuth2 authorization code callback
        // Note: mapped on the auth group — browser sends session cookie during top-level redirect (SameSite=Lax)
        group.MapGet("/gmail/callback", async (
            [FromQuery] string? code,
            [FromQuery] string? state,
            [FromQuery] string? error,
            IConfiguration config,
            HttpContext ctx,
            ISender sender,
            CancellationToken ct) =>
        {
            const string frontendBase = "/settings/integrations";

            if (!string.IsNullOrEmpty(error))
                return Results.Redirect($"{frontendBase}?gmailError={Uri.EscapeDataString(error)}");

            // CSRF state validation
            var storedState = ctx.Request.Cookies["gmail_oauth_state"];
            ctx.Response.Cookies.Delete("gmail_oauth_state");

            if (string.IsNullOrEmpty(storedState) || storedState != state)
                return Results.Redirect($"{frontendBase}?gmailError=invalid_state");

            if (string.IsNullOrEmpty(code))
                return Results.Redirect($"{frontendBase}?gmailError=missing_code");

            var clientId = config["Gmail:ClientId"];
            var clientSecret = config["Gmail:ClientSecret"];
            var redirectUri = config["Gmail:RedirectUri"];

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret) || string.IsNullOrWhiteSpace(redirectUri))
                return Results.Redirect($"{frontendBase}?gmailError=not_configured");

            // Exchange authorization code for tokens
            using var http = new HttpClient();
            var tokenResp = await http.PostAsync("https://oauth2.googleapis.com/token",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = clientId,
                    ["client_secret"] = clientSecret,
                    ["code"] = code,
                    ["redirect_uri"] = redirectUri,
                    ["grant_type"] = "authorization_code",
                }), ct);

            if (!tokenResp.IsSuccessStatusCode)
                return Results.Redirect($"{frontendBase}?gmailError=token_exchange_failed");

            var tokens = await tokenResp.Content.ReadFromJsonAsync<GmailTokenResponse>(cancellationToken: ct);

            if (string.IsNullOrEmpty(tokens?.RefreshToken))
                return Results.Redirect($"{frontendBase}?gmailError=no_refresh_token");

            await sender.Send(new ConnectGmailCommand(tokens.RefreshToken), ct);

            return Results.Redirect($"{frontendBase}?gmailConnected=true");
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

file sealed record GmailTokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn);
