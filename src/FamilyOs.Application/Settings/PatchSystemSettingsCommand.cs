using MediatR;

namespace FamilyOs.Application.Settings;

public sealed record PatchSystemSettingsCommand(SmtpSettingsDto? Smtp, int? AuditRetentionDays) : IRequest;
