using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Common.Errors;
using FamilyOs.Application.Family.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FamilyOs.Application.Family.Commands;

public sealed class UpdateFamilyMemberCommandHandler(IFamilyOsDbContext db)
    : IRequestHandler<UpdateFamilyMemberCommand, FamilyMemberDto>
{
    public async Task<FamilyMemberDto> Handle(
        UpdateFamilyMemberCommand request,
        CancellationToken cancellationToken)
    {
        var member = await db.FamilyMembers
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("FamilyMember", request.Id);

        member.Update(
            displayName: request.DisplayName,
            relation: request.Relation,
            fullName: request.FullName,
            birthDate: request.BirthDate,
            notes: request.Notes);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("Az adatot időközben más módosította. Kérjük, frissítse az oldalt.");
        }

        return new FamilyMemberDto(
            Id: member.Id,
            DisplayName: member.DisplayName,
            FullName: member.FullName,
            Relation: member.Relation,
            BirthDate: member.BirthDate,
            Notes: member.Notes,
            HasUserAccount: member.HasUserAccount,
            RowVersion: request.RowVersion,
            DeletedUtc: member.DeletedUtc);
    }
}
