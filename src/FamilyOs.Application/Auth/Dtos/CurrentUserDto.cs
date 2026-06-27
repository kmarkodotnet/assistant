namespace FamilyOs.Application.Auth.Dtos;

public record CurrentUserDto(
    Guid UserAccountId,
    Guid? FamilyMemberId,
    string DisplayName,
    string Email,
    string Role,
    UserPreferencesDto? Preferences);

public record UserPreferencesDto(
    bool EmailEnabled,
    string? QuietHoursStart,
    string? QuietHoursEnd);
