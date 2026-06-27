using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Infrastructure.Ai.Options;
using FamilyOs.Infrastructure.Ai.Providers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace FamilyOs.Infrastructure.Tests.Ai;

public sealed class AiProviderFactoryTests
{
    private static AiProviderFactory CreateFactory(PrivacyMode mode, OllamaAiProvider? provider = null)
    {
        var privacyOptions = Options.Create(new AiPrivacyOptions { PrivacyMode = mode });
        var ollamaOptions = Options.Create(new OllamaOptions
        {
            BaseUrl = "http://localhost:11434",
            DefaultModel = "llama3.2:3b",
            TimeoutSeconds = 30,
        });

        // OllamaAiProvider requires OllamaHttpClient — use a substitute or real instance with null client
        var ollamaProvider = provider ?? new OllamaAiProvider(null!, ollamaOptions);

        return new AiProviderFactory(privacyOptions, ollamaProvider);
    }

    [Fact]
    public void GetProvider_LocalOnlyMode_ReturnsOllamaProvider()
    {
        var factory = CreateFactory(PrivacyMode.LocalOnly);

        var provider = factory.GetProvider();

        provider.ProviderName.Should().Be("ollama");
    }

    [Fact]
    public void GetProvider_HybridAllowedMode_ThrowsNotImplementedException()
    {
        var factory = CreateFactory(PrivacyMode.HybridAllowed);

        var act = () => factory.GetProvider();

        act.Should().Throw<NotImplementedException>();
    }

    [Fact]
    public void GetProvider_AnyProviderMode_ThrowsNotImplementedException()
    {
        var factory = CreateFactory(PrivacyMode.AnyProvider);

        var act = () => factory.GetProvider();

        act.Should().Throw<NotImplementedException>();
    }

    [Fact]
    public void GetProvider_InvalidMode_ThrowsAiProviderNotAllowedException()
    {
        var factory = CreateFactory((PrivacyMode)999);

        var act = () => factory.GetProvider();

        act.Should().Throw<AiProviderNotAllowedException>();
    }
}
