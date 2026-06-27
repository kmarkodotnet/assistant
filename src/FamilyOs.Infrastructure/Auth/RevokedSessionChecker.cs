using FamilyOs.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace FamilyOs.Infrastructure.Auth;

public static class RevokedSessionChecker
{
    public static async Task ValidatePrincipalAsync(CookieValidatePrincipalContext ctx)
    {
        var db = ctx.HttpContext.RequestServices.GetRequiredService<FamilyOsDbContext>();
        var jti = ctx.Principal?.FindFirstValue("jti");

        if (jti is not null && await db.RevokedSessions.AnyAsync(r => r.SessionId == jti))
        {
            ctx.RejectPrincipal();
            ctx.Response.StatusCode = 401;
        }
    }
}
