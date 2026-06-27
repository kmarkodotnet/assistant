using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Errors;
using FamilyOs.Application.Family.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Family.Queries;

public sealed class GetFamilyMemberByIdQueryHandler(IFamilyOsDbContext db)
    : IRequestHandler<GetFamilyMemberByIdQuery, FamilyMemberDto>
{
    public async Task<FamilyMemberDto> Handle(
        GetFamilyMemberByIdQuery request,
        CancellationToken cancellationToken)
    {
        var m = await db.FamilyMembers
            .AsNoTracking()
            .Where(f => f.Id == request.Id)
            .Select(f => new
            {
                f.Id,
                f.DisplayName,
                f.FullName,
                f.Relation,
                f.BirthDate,
                f.Notes,
                f.HasUserAccount,
                f.DeletedUtc,
                xmin = EF.Property<uint>(f, "xmin"),
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("FamilyMember", request.Id);

        return new FamilyMemberDto(
            Id: m.Id,
            DisplayName: m.DisplayName,
            FullName: m.FullName,
            Relation: m.Relation,
            BirthDate: m.BirthDate,
            Notes: m.Notes,
            HasUserAccount: m.HasUserAccount,
            RowVersion: m.xmin.ToString(System.Globalization.CultureInfo.InvariantCulture),
            DeletedUtc: m.DeletedUtc);
    }
}
