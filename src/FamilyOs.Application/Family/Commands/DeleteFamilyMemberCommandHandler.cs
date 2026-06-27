using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Family.Commands;

public sealed class DeleteFamilyMemberCommandHandler(IFamilyOsDbContext db)
    : IRequestHandler<DeleteFamilyMemberCommand>
{
    public async Task Handle(
        DeleteFamilyMemberCommand request,
        CancellationToken cancellationToken)
    {
        var member = await db.FamilyMembers
            .Include(m => m.UserAccount)
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("FamilyMember", request.Id);

        if (member.HasUserAccount && member.UserAccount is not null && member.UserAccount.DeletedUtc is null)
            throw new ConflictException("Előbb deaktiváld a hozzá tartozó felhasználói fiókot.");

        member.SoftDelete();
        await db.SaveChangesAsync(cancellationToken);
    }
}
