using FamilyOs.Application.Family.Dtos;
using FamilyOs.Domain.Enums;
using MediatR;

namespace FamilyOs.Application.Family.Commands;

public record UpdateFamilyMemberCommand(
    Guid Id,
    string DisplayName,
    Relation Relation,
    string? FullName,
    DateOnly? BirthDate,
    string? Notes,
    string RowVersion)
    : IRequest<FamilyMemberDto>;
