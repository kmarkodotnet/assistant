using FamilyOs.Application.Abstractions.Auth;
using FamilyOs.Application.Abstractions.Common;
using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Infrastructure.Auth;
using FamilyOs.Infrastructure.Common;
using FamilyOs.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FamilyOs.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserAccessor, CurrentUserService>();

        services.AddDbContext<FamilyOsDbContext>(opts =>
            opts.UseNpgsql(
                    configuration.GetConnectionString("Default")
                    ?? configuration.GetConnectionString("DefaultConnection"),
                    npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "app"))
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IFamilyOsDbContext>(sp => sp.GetRequiredService<FamilyOsDbContext>());

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "__Host-family-os-session";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
                options.SlidingExpiration = true;
                options.Events.OnValidatePrincipal = RevokedSessionChecker.ValidatePrincipalAsync;
                options.Events.OnRedirectToLogin = ctx =>
                {
                    ctx.Response.StatusCode = 401;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = ctx =>
                {
                    ctx.Response.StatusCode = 403;
                    return Task.CompletedTask;
                };
            });

        services.AddSingleton<IAllowlistService, AllowlistService>();
        services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();

        return services;
    }
}
