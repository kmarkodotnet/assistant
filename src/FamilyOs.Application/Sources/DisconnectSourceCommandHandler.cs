using FamilyOs.Application.Abstractions.Common;
using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Abstractions;
using FamilyOs.Application.Common.Errors;
using FamilyOs.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Sources;

public sealed class DisconnectSourceCommandHandler(
    IFamilyOsDbContext db,
    IAuditLogger auditLogger,
    ICurrentUserAccessor currentUser)
    : IRequestHandler<DisconnectSourceCommand>
{
    public async Task Handle(DisconnectSourceCommand request, CancellationToken cancellationToken)
    {
        var source = await db.Sources
            .FirstOrDefaultAsync(s => s.Id == request.SourceId, cancellationToken)
            ?? throw new NotFoundException("Source", request.SourceId);

        source.SoftDelete();

        await db.SaveChangesAsync(cancellationToken);

        await auditLogger.LogAsync(
            AuditAction.Delete,
            currentUser.UserAccountId,
            "Source",
            request.SourceId,
            ct: cancellationToken);
    }
}
