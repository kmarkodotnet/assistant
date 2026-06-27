using Microsoft.AspNetCore.Authorization;

namespace FamilyOs.Api.Auth;

public static class AuthorizationPolicies
{
    public static void AddFamilyOsPolicies(this AuthorizationOptions opts)
    {
        opts.AddPolicy("RequireAdmin", p => p.RequireRole("Admin"));
        opts.AddPolicy("RequireAdult", p => p.RequireRole("Admin", "Adult"));
        opts.AddPolicy("RequireAuthenticated", p => p.RequireAuthenticatedUser());
    }
}
