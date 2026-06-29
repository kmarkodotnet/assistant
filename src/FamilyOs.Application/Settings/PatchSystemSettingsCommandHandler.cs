using MediatR;
using Microsoft.Extensions.Logging;

namespace FamilyOs.Application.Settings;

public sealed partial class PatchSystemSettingsCommandHandler(ILogger<PatchSystemSettingsCommandHandler> logger)
    : IRequestHandler<PatchSystemSettingsCommand>
{
    [LoggerMessage(Level = LogLevel.Warning,
        Message = "System settings patch accepted in-memory only (MVP). " +
                  "SMTP and retention config changes require an application restart after updating appsettings.json. " +
                  "Smtp={Smtp}, AuditRetentionDays={AuditRetentionDays}")]
    private static partial void LogSystemSettingsPatch(ILogger logger, SmtpSettingsDto? smtp, int? auditRetentionDays);

    public Task Handle(PatchSystemSettingsCommand request, CancellationToken cancellationToken)
    {
        LogSystemSettingsPatch(logger, request.Smtp, request.AuditRetentionDays);

        return Task.CompletedTask;
    }
}
