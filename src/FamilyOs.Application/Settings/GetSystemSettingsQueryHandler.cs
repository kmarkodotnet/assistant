using MediatR;
using Microsoft.Extensions.Configuration;

namespace FamilyOs.Application.Settings;

public sealed class GetSystemSettingsQueryHandler(IConfiguration configuration)
    : IRequestHandler<GetSystemSettingsQuery, SystemSettingsDto>
{
    public Task<SystemSettingsDto> Handle(GetSystemSettingsQuery request, CancellationToken cancellationToken)
    {
        var smtp = new SmtpSettingsDto(
            configuration["Notifications:Smtp:Host"],
            configuration.GetValue<int?>("Notifications:Smtp:Port"),
            configuration["Notifications:Smtp:From"]);

        var dto = new SystemSettingsDto(
            "LocalOnly",
            configuration.GetValue<int?>("Audit:RetentionDays"),
            configuration.GetValue<int?>("Notifications:FeedRetentionDays"),
            smtp.Host is not null || smtp.Port.HasValue || smtp.From is not null ? smtp : null);

        return Task.FromResult(dto);
    }
}
