using MediatR;

namespace FamilyOs.Application.Admin.AiProviders;

public sealed record PatchAiProviderCommand(string Name, bool? Enabled, string? Model) : IRequest;
