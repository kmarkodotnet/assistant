using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Users.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Users.Queries;

public sealed class GetUserAccountsQueryHandler(IFamilyOsDbContext db)
    : IRequestHandler<GetUserAccountsQuery, IReadOnlyList<UserAccountDto>>
{
    public async Task<IReadOnlyList<UserAccountDto>> Handle(
        GetUserAccountsQuery request,
        CancellationToken cancellationToken)
    {
        return await db.UserAccounts
            .AsNoTracking()
            .Select(u => new UserAccountDto(
                u.Id,
                u.FamilyMemberId,
                u.Email,
                u.DisplayName,
                u.Role.ToString(),
                u.IsActive,
                u.LastLoginUtc,
                u.CreatedUtc))
            .ToListAsync(cancellationToken);
    }
}
