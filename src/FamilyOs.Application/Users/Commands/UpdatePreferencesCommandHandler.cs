using FamilyOs.Application.Abstractions.Common;
using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Users.Commands;

public sealed class UpdatePreferencesCommandHandler(
    ICurrentUserAccessor currentUser,
    IFamilyOsDbContext db)
    : IRequestHandler<UpdatePreferencesCommand>
{
    public async Task Handle(UpdatePreferencesCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserAccountId is null)
            throw new ForbiddenException("Hozzáférés megtagadva.");

        var account = await db.UserAccounts
            .FirstOrDefaultAsync(u => u.Id == currentUser.UserAccountId, cancellationToken)
            ?? throw new NotFoundException("Nem található aktív felhasználói fiók.");

        account.UpdatePreferences(
            emailEnabled: request.EmailEnabled,
            quietHoursStart: request.QuietHoursStart,
            quietHoursEnd: request.QuietHoursEnd);

        await db.SaveChangesAsync(cancellationToken);
    }
}
