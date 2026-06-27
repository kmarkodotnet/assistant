using FamilyOs.Domain.Enums;

namespace FamilyOs.Application.Family.Dtos;

public record FamilyMemberDto(
    Guid Id,
    string DisplayName,
    string? FullName,
    Relation Relation,
    DateOnly? BirthDate,
    string? Notes,
    bool HasUserAccount,
    string RowVersion,
    DateTime? DeletedUtc);
