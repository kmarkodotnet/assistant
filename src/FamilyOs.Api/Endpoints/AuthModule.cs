using FamilyOs.Application.Auth.Commands;
using FamilyOs.Application.Auth.Queries;
using FamilyOs.Application.Users.Commands;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace FamilyOs.Api.Endpoints;

public static class AuthModule
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth");

        group.MapPost("/login/google", async (LoginGoogleRequest req, ISender sender, HttpContext ctx) =>
        {
            var dto = await sender.Send(new LoginGoogleCommand(req.IdToken));

            var jti = Guid.NewGuid().ToString();
            var claims = new List<Claim>
            {
                new("sub", dto.UserAccountId.ToString()),
                new("family_member_id", dto.FamilyMemberId?.ToString() ?? string.Empty),
                new(ClaimTypes.Role, dto.Role),
                new("jti", jti),
                new(ClaimTypes.Email, dto.Email),
                new(ClaimTypes.Name, dto.DisplayName),
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await ctx.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { IsPersistent = true });

            return Results.Ok(dto);
        });

        group.MapPost("/logout", async (ISender sender, HttpContext ctx) =>
        {
            await sender.Send(new LogoutCommand());
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.NoContent();
        }).RequireAuthorization("RequireAuthenticated");

        group.MapGet("/me", async (ISender sender) =>
        {
            var dto = await sender.Send(new GetCurrentUserQuery());
            return Results.Ok(dto);
        }).RequireAuthorization("RequireAuthenticated");

        group.MapMethods("/me/preferences", ["PATCH"], async (UpdatePreferencesRequest req, ISender sender) =>
        {
            await sender.Send(new UpdatePreferencesCommand(req.EmailEnabled, req.QuietHoursStart, req.QuietHoursEnd));
            return Results.NoContent();
        }).RequireAuthorization("RequireAuthenticated");
    }
}

public record LoginGoogleRequest(string IdToken);
public record UpdatePreferencesRequest(bool EmailEnabled, string? QuietHoursStart, string? QuietHoursEnd);
