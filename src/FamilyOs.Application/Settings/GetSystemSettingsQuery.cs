using MediatR;

namespace FamilyOs.Application.Settings;

public sealed record GetSystemSettingsQuery : IRequest<SystemSettingsDto>;
