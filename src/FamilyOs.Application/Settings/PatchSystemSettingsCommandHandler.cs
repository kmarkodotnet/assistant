using FamilyOs.Application.Common.Errors;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FamilyOs.Application.Settings;

public sealed class PatchSystemSettingsCommandHandler(ILogger<PatchSystemSettingsCommandHandler> logger)
    : IRequestHandler<PatchSystemSettingsCommand>
{
    public Task Handle(PatchSystemSettingsCommand request, CancellationToken cancellationToken)
    {
        logger.LogWarning(
            "System settings patch accepted in-memory only (MVP). " +
            "SMTP and retention config changes require an application restart after updating appsettings.json. " +
            "Smtp={@Smtp}, AuditRetentionDays={AuditRetentionDays}",
            request.Smtp, request.AuditRetentionDays);

        return Task.CompletedTask;
    }
}
