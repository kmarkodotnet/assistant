using FamilyOs.Application.Abstractions.Persistence;
using FamilyOs.Application.Family.Dtos;
using FamilyOs.Domain.Entities;
using MediatR;

namespace FamilyOs.Application.Family.Commands;

public sealed class CreateFamilyMemberCommandHandler(IFamilyOsDbContext db)
    : IRequestHandler<CreateFamilyMemberCommand, FamilyMemberDto>
{
    public async Task<FamilyMemberDto> Handle(
        CreateFamilyMemberCommand request,
        CancellationToken cancellationToken)
    {
        var member = FamilyMember.Create(
            displayName: request.DisplayName,
            relation: request.Relation,
            fullName: request.FullName,
            birthDate: request.BirthDate,
            notes: request.Notes);

        db.FamilyMembers.Add(member);
        await db.SaveChangesAsync(cancellationToken);

        return new FamilyMemberDto(
            Id: member.Id,
            DisplayName: member.DisplayName,
            FullName: member.FullName,
            Relation: member.Relation,
            BirthDate: member.BirthDate,
            Notes: member.Notes,
            HasUserAccount: member.HasUserAccount,
            RowVersion: "0",
            DeletedUtc: member.DeletedUtc);
    }
}
