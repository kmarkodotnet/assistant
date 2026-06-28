namespace FamilyOs.Application.Settings;

public sealed record SystemSettingsDto(
    string PrivacyMode,
    int? AuditRetentionDays,
    int? NotificationFeedRetentionDays,
    SmtpSettingsDto? Smtp);

public sealed record SmtpSettingsDto(string? Host, int? Port, string? From);
