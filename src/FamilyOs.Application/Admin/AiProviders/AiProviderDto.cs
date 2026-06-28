namespace FamilyOs.Application.Admin.AiProviders;

public sealed record AiProviderDto(string Name, bool Enabled, string? Model, string? LastHealth);
