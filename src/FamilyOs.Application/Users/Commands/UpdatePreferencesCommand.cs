using MediatR;

namespace FamilyOs.Application.Users.Commands;

public record UpdatePreferencesCommand(
    bool EmailEnabled,
    string? QuietHoursStart,
    string? QuietHoursEnd)
    : IRequest;
