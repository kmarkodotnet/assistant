using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Users.Commands;

public sealed class DeleteUserAccountCommandHandler(IFamilyOsDbContext db)
    : IRequestHandler<DeleteUserAccountCommand>
{
    public async Task Handle(DeleteUserAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await db.UserAccounts
            .Include(u => u.FamilyMember)
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("UserAccount", request.Id);

        account.SoftDelete();
        account.FamilyMember.MarkHasUserAccount(false);
        await db.SaveChangesAsync(cancellationToken);
    }
}
