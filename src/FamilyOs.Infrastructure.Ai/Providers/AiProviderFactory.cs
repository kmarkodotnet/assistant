using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Infrastructure.Ai.Options;
using Microsoft.Extensions.Options;

namespace FamilyOs.Infrastructure.Ai.Providers;

public interface IAiProviderFactory
{
    IAiProvider GetProvider();
}

public sealed class AiProviderFactory : IAiProviderFactory
{
    private readonly AiPrivacyOptions _options;
    private readonly OllamaAiProvider _ollamaProvider;

    public AiProviderFactory(IOptions<AiPrivacyOptions> options, OllamaAiProvider ollamaProvider)
    {
        _options = options.Value;
        _ollamaProvider = ollamaProvider;
    }

    public IAiProvider GetProvider() => _options.PrivacyMode switch
    {
        PrivacyMode.LocalOnly => _ollamaProvider,
        PrivacyMode.HybridAllowed => throw new NotImplementedException("Hybrid mode is post-MVP."),
        PrivacyMode.AnyProvider => throw new NotImplementedException("Any-provider mode is post-MVP."),
        _ => throw new AiProviderNotAllowedException(),
    };
}
