namespace FamilyOs.Application.Users.Dtos;

public record UserAccountDto(
    Guid Id,
    Guid FamilyMemberId,
    string Email,
    string DisplayName,
    string Role,
    bool IsActive,
    DateTime? LastLoginUtc,
    DateTime CreatedUtc);
