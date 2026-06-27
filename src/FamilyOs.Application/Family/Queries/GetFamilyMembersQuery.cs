using FamilyOs.Application.Family.Dtos;
using FamilyOs.Domain.Enums;
using MediatR;

namespace FamilyOs.Application.Family.Queries;

public record GetFamilyMembersQuery(Relation? Relation) : IRequest<IReadOnlyList<FamilyMemberDto>>;
