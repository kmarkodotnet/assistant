using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Infrastructure.Ai.Options;
using FamilyOs.Infrastructure.Ai.Providers;
using FamilyOs.Infrastructure.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;
using System.Text.Json;

namespace FamilyOs.Infrastructure.Tests.Ai;

public sealed class AiProviderPrivacyGuardTests
{
    [Fact]
    public void LocalOnly_GetProvider_ReturnsOllamaProvider()
    {
        // Arrange
        var privacyOptions = Options.Create(new AiPrivacyOptions { PrivacyMode = PrivacyMode.LocalOnly });
        var ollamaOptions = Options.Create(new OllamaOptions
        {
            BaseUrl = "http://ollama:11434",
            DefaultModel = "llama3.2:3b",
            TimeoutSeconds = 30,
        });
        var ollamaProvider = new OllamaAiProvider(null!, ollamaOptions);
        var factory = new AiProviderFactory(privacyOptions, ollamaProvider);

        // Act
        var provider = factory.GetProvider();

        // Assert
        provider.ProviderName.Should().Be("ollama");
    }

    [Fact]
    public void HybridAllowed_GetProvider_ThrowsNotImplementedException()
    {
        // Arrange
        var privacyOptions = Options.Create(new AiPrivacyOptions { PrivacyMode = PrivacyMode.HybridAllowed });
        var ollamaOptions = Options.Create(new OllamaOptions());
        var ollamaProvider = new OllamaAiProvider(null!, ollamaOptions);
        var factory = new AiProviderFactory(privacyOptions, ollamaProvider);

        // Act
        var act = () => factory.GetProvider();

        // Assert
        act.Should().Throw<NotImplementedException>();
    }

    [Fact]
    public async Task OllamaProvider_OnlyCallsOllamaHost()
    {
        // Arrange: mocked HttpMessageHandler that records all request URIs
        var recordedUris = new List<Uri>();
        var handler = new RecordingHttpMessageHandler(recordedUris, new Uri("http://ollama:11434"));

        var httpClient = new HttpClient(handler);
        var ollamaOptions = Options.Create(new OllamaOptions
        {
            BaseUrl = "http://ollama:11434",
            DefaultModel = "llama3.2:3b",
            TimeoutSeconds = 10,
        });

        var ollamaHttpClient = new OllamaHttpClient(httpClient, ollamaOptions);
        var provider = new OllamaAiProvider(ollamaHttpClient, ollamaOptions);

        var prompt = new AiPrompt("system", "user", "test-prompt", "v1");

        // Act
        await provider.CompleteAsync(prompt);

        // Assert: all requests go to ollama:11434 (no external hosts)
        recordedUris.Should().NotBeEmpty();
        recordedUris.Should().AllSatisfy(uri =>
            uri.Host.Should().Be("ollama"));
    }

    /// <summary>
    /// Helper HttpMessageHandler that records URIs and returns a valid Ollama response.
    /// </summary>
    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly List<Uri> _recordedUris;
        private readonly Uri _expectedBaseUri;

        public RecordingHttpMessageHandler(List<Uri> recordedUris, Uri expectedBaseUri)
        {
            _recordedUris = recordedUris;
            _expectedBaseUri = expectedBaseUri;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri is not null)
                _recordedUris.Add(request.RequestUri);

            var response = new
            {
                message = new { role = "assistant", content = "{\"summary\": \"test\"}" },
                prompt_eval_count = 10,
                eval_count = 5,
            };

            var json = JsonSerializer.Serialize(response);
            return System.Threading.Tasks.Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            });
        }
    }
}
