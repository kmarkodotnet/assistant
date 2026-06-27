using FamilyOs.Application.Abstractions.Common;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace FamilyOs.Infrastructure.Auth;

public sealed class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserAccessor
{
    public Guid? UserAccountId
    {
        get
        {
            var sub = accessor.HttpContext?.User.FindFirstValue("sub");
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public Guid? FamilyMemberId
    {
        get
        {
            var claim = accessor.HttpContext?.User.FindFirstValue("family_member_id");
            return Guid.TryParse(claim, out var id) ? id : null;
        }
    }

    public string? Role => accessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role);
}
