using FamilyOs.Application.Common.Errors;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FamilyOs.Application.Admin.AiProviders;

public sealed partial class PatchAiProviderCommandHandler(ILogger<PatchAiProviderCommandHandler> logger)
    : IRequestHandler<PatchAiProviderCommand>
{
    [LoggerMessage(Level = LogLevel.Warning,
        Message = "AI provider '{Name}' patch requested (Enabled={Enabled}, Model={Model}). " +
                  "Runtime config persistence is not available in MVP — restart the application after changing appsettings.json.")]
    private static partial void LogAiProviderPatchRequested(ILogger logger, string name, bool? enabled, string? model);

    public Task Handle(PatchAiProviderCommand request, CancellationToken cancellationToken)
    {
        if (string.Equals(request.Name, "PrivacyMode", StringComparison.OrdinalIgnoreCase))
            throw new DomainBusinessRuleException("Az adatvédelmi mód kódba van égetve és nem módosítható.");

        LogAiProviderPatchRequested(logger, request.Name, request.Enabled, request.Model);

        return Task.CompletedTask;
    }
}
