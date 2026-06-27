using FamilyOs.Application.Abstractions.Common;
using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Family.Dtos;
using FamilyOs.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Family.Queries;

public sealed class GetFamilyMembersQueryHandler(
    IFamilyOsDbContext db,
    ICurrentUserAccessor currentUser)
    : IRequestHandler<GetFamilyMembersQuery, IReadOnlyList<FamilyMemberDto>>
{
    public async Task<IReadOnlyList<FamilyMemberDto>> Handle(
        GetFamilyMembersQuery request,
        CancellationToken cancellationToken)
    {
        var query = db.FamilyMembers.AsNoTracking();

        if (request.Relation.HasValue)
            query = query.Where(m => m.Relation == request.Relation.Value);

        var isChildRole = string.Equals(currentUser.Role, "Child", StringComparison.OrdinalIgnoreCase);
        var ownMemberId = currentUser.FamilyMemberId;

        var members = await query
            .Select(m => new
            {
                m.Id,
                m.DisplayName,
                m.FullName,
                m.Relation,
                m.BirthDate,
                m.Notes,
                m.HasUserAccount,
                m.DeletedUtc,
                xmin = EF.Property<uint>(m, "xmin"),
            })
            .ToListAsync(cancellationToken);

        return members
            .Select(m =>
            {
                var birthDate = isChildRole && m.Id != ownMemberId ? null : m.BirthDate;
                var notes = isChildRole && m.Id != ownMemberId ? null : m.Notes;

                return new FamilyMemberDto(
                    Id: m.Id,
                    DisplayName: m.DisplayName,
                    FullName: m.FullName,
                    Relation: m.Relation,
                    BirthDate: birthDate,
                    Notes: notes,
                    HasUserAccount: m.HasUserAccount,
                    RowVersion: m.xmin.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    DeletedUtc: m.DeletedUtc);
            })
            .ToList()
            .AsReadOnly();
    }
}
