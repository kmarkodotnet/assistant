using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Auth.Commands;
using FamilyOs.Application.Auth.Dtos;
using FamilyOs.Application.Auth.Queries;
using FamilyOs.Application.Users.Commands;
using FamilyOs.Domain.Entities;
using FamilyOs.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FamilyOs.Api.Endpoints;

public static class AuthModule
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth");

        // Public endpoint — returns the Google Client ID for the frontend login page
        group.MapGet("/config", (IConfiguration config) =>
            Results.Ok(new { googleClientId = config["Auth:GoogleClientId"] ?? string.Empty }))
            .AllowAnonymous();

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

        // DEV-ONLY: test-login endpoint for E2E tests.
        // Disabled unless ASPNETCORE_ENVIRONMENT != Production OR Auth:AllowTestLogin == "true".
        group.MapPost("/test-login", async (
            TestLoginRequest req,
            IWebHostEnvironment env,
            IConfiguration configuration,
            IFamilyOsDbContext db,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var allowed = !env.IsProduction() || configuration["Auth:AllowTestLogin"] == "true";
            if (!allowed)
                return Results.NotFound();

            if (string.IsNullOrWhiteSpace(req.Email))
                return Results.BadRequest("Email is required.");

            var email = req.Email.ToLowerInvariant().Trim();

            // Parse role, default to Child
            var role = Enum.TryParse<UserRole>(req.Role, ignoreCase: true, out var parsedRole)
                ? parsedRole
                : UserRole.Child;

            var displayName = string.IsNullOrWhiteSpace(req.DisplayName)
                ? email
                : req.DisplayName.Trim();

            // Find or create the user account
            var existingAccount = await db.UserAccounts
                .Include(u => u.FamilyMember)
                .FirstOrDefaultAsync(u => u.Email == email, ct);

            UserAccount account;
            if (existingAccount is not null)
            {
                existingAccount.RecordLogin();
                await db.SaveChangesAsync(ct);
                account = existingAccount;
            }
            else
            {
                var familyMember = FamilyMember.Create(
                    displayName: displayName,
                    relation: Relation.Self);
                db.FamilyMembers.Add(familyMember);

                // Use email as a synthetic GoogleSubject for test accounts
                account = UserAccount.Create(
                    familyMemberId: familyMember.Id,
                    googleSubject: $"test|{email}",
                    email: email,
                    displayName: displayName,
                    role: role);

                account.RecordLogin();
                db.UserAccounts.Add(account);
                await db.SaveChangesAsync(ct);
            }

            var jti = Guid.NewGuid().ToString();
            var claims = new List<Claim>
            {
                new("sub", account.Id.ToString()),
                new("family_member_id", account.FamilyMemberId.ToString()),
                new(ClaimTypes.Role, account.Role.ToString()),
                new("jti", jti),
                new(ClaimTypes.Email, account.Email),
                new(ClaimTypes.Name, account.DisplayName),
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await ctx.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { IsPersistent = true });

            return Results.Ok(new CurrentUserDto(
                UserAccountId: account.Id,
                FamilyMemberId: account.FamilyMemberId,
                DisplayName: account.DisplayName,
                Email: account.Email,
                Role: account.Role.ToString(),
                Preferences: new UserPreferencesDto(
                    EmailEnabled: account.EmailEnabled,
                    QuietHoursStart: account.QuietHoursStart,
                    QuietHoursEnd: account.QuietHoursEnd)));
        }).AllowAnonymous();
    }
}

public record LoginGoogleRequest(string IdToken);
public record UpdatePreferencesRequest(bool EmailEnabled, string? QuietHoursStart, string? QuietHoursEnd);
public record TestLoginRequest(string Email, string Role, string DisplayName);
