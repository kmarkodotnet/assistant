using FamilyOs.Application.Abstractions.Common;
using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Auth.Dtos;
using FamilyOs.Application.Common.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Auth.Queries;

public sealed class GetCurrentUserQueryHandler(
    ICurrentUserAccessor currentUser,
    IFamilyOsDbContext db)
    : IRequestHandler<GetCurrentUserQuery, CurrentUserDto>
{
    public async Task<CurrentUserDto> Handle(
        GetCurrentUserQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserAccountId is null)
            throw new NotFoundException("Nem található aktív munkamenet.");

        var account = await db.UserAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == currentUser.UserAccountId, cancellationToken)
            ?? throw new NotFoundException("Nem található aktív munkamenet.");

        return new CurrentUserDto(
            UserAccountId: account.Id,
            FamilyMemberId: account.FamilyMemberId,
            DisplayName: account.DisplayName,
            Email: account.Email,
            Role: account.Role.ToString(),
            Preferences: new UserPreferencesDto(
                EmailEnabled: account.EmailEnabled,
                QuietHoursStart: account.QuietHoursStart,
                QuietHoursEnd: account.QuietHoursEnd));
    }
}
