using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Errors;
using FamilyOs.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Users.Commands;

public sealed class PatchUserAccountCommandHandler(IFamilyOsDbContext db)
    : IRequestHandler<PatchUserAccountCommand>
{
    public async Task Handle(PatchUserAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await db.UserAccounts
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("UserAccount", request.Id);

        if (request.Role is not null)
        {
            if (!Enum.TryParse<UserRole>(request.Role, out var newRole))
                throw new DomainBusinessRuleException($"Az '{request.Role}' szerepkör nem érvényes.");
            account.ChangeRole(newRole);
        }

        if (request.IsActive.HasValue)
            account.SetActive(request.IsActive.Value);

        await db.SaveChangesAsync(cancellationToken);
    }
}
