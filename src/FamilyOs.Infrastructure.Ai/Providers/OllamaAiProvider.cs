using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Application.Common.Errors;
using FamilyOs.Infrastructure.Ai.Options;
using FamilyOs.Infrastructure.Ai.Prompts;
using Microsoft.Extensions.Options;

namespace FamilyOs.Infrastructure.Ai.Providers;

public sealed class OllamaAiProvider : IAiProvider
{
    private readonly OllamaHttpClient _client;
    private readonly OllamaOptions _options;

    public string ProviderName => "ollama";
    public AiCapabilities Capabilities => AiCapabilities.JsonMode;

    public OllamaAiProvider(OllamaHttpClient client, IOptions<OllamaOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task<AiCompletion> CompleteAsync(AiPrompt prompt, CancellationToken ct = default)
    {
        var sysPrefix = PromptTemplate.Load(PromptCatalog.SysPrefix);
        var systemPrompt = sysPrefix + "\n" + prompt.SystemPrompt;

        try
        {
            var (content, inputTokens, outputTokens) = await _client.PostChatAsync(
                _options.DefaultModel,
                systemPrompt,
                prompt.UserPrompt,
                ct);

            return new AiCompletion(content, inputTokens, outputTokens, _options.DefaultModel);
        }
        catch (AiProviderUnavailableException)
        {
            throw;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new AiProviderUnavailableException("Ollama request timed out.");
        }
    }
}
