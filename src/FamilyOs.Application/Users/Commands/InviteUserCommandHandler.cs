using FamilyOs.Application.Abstractions.Common;
using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Errors;
using FamilyOs.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Users.Commands;

public sealed class InviteUserCommandHandler(
    IFamilyOsDbContext db,
    IClock clock)
    : IRequestHandler<InviteUserCommand>
{
    public async Task Handle(InviteUserCommand request, CancellationToken cancellationToken)
    {
        var familyMember = await db.FamilyMembers
            .FirstOrDefaultAsync(f => f.Id == request.FamilyMemberId, cancellationToken)
            ?? throw new NotFoundException("FamilyMember", request.FamilyMemberId);

        var existing = await db.PendingInvites
            .FirstOrDefaultAsync(i => i.Email == request.Email.ToLowerInvariant().Trim(), cancellationToken);

        if (existing is not null)
            throw new ConflictException("Erre az e-mail címre már van függőben lévő meghívó.");

        var invite = PendingInvite.Create(request.Email, familyMember.Id, request.Role, clock.UtcNow);
        db.PendingInvites.Add(invite);
        await db.SaveChangesAsync(cancellationToken);
    }
}
