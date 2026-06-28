using FamilyOs.Application.Common.Errors;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FamilyOs.Application.Admin.AiProviders;

public sealed class PatchAiProviderCommandHandler(ILogger<PatchAiProviderCommandHandler> logger)
    : IRequestHandler<PatchAiProviderCommand>
{
    public Task Handle(PatchAiProviderCommand request, CancellationToken cancellationToken)
    {
        if (string.Equals(request.Name, "PrivacyMode", StringComparison.OrdinalIgnoreCase))
            throw new DomainBusinessRuleException("Az adatvédelmi mód kódba van égetve és nem módosítható.");

        logger.LogWarning(
            "AI provider '{Name}' patch requested (Enabled={Enabled}, Model={Model}). " +
            "Runtime config persistence is not available in MVP — restart the application after changing appsettings.json.",
            request.Name, request.Enabled, request.Model);

        return Task.CompletedTask;
    }
}
