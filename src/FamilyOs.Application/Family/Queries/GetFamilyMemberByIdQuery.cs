using FamilyOs.Application.Family.Dtos;
using MediatR;

namespace FamilyOs.Application.Family.Queries;

public record GetFamilyMemberByIdQuery(Guid Id) : IRequest<FamilyMemberDto>;
