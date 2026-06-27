using FamilyOs.Application.Family.Dtos;
using FamilyOs.Domain.Enums;
using MediatR;

namespace FamilyOs.Application.Family.Commands;

public record CreateFamilyMemberCommand(
    string DisplayName,
    Relation Relation,
    string? FullName,
    DateOnly? BirthDate,
    string? Notes)
    : IRequest<FamilyMemberDto>;
