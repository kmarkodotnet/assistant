using FamilyOs.Application.Abstractions.Common;
using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace FamilyOs.Application.Auth.Commands;

public sealed class LogoutCommandHandler(
    IFamilyOsDbContext db,
    IClock clock,
    IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var jti = httpContextAccessor.HttpContext?.User.FindFirst("jti")?.Value;
        if (!string.IsNullOrWhiteSpace(jti))
        {
            var revoked = RevokedSession.Create(jti, clock.UtcNow);
            db.RevokedSessions.Add(revoked);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
