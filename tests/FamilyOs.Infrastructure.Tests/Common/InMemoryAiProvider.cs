using FamilyOs.Application.Abstractions.Ai;

namespace FamilyOs.Infrastructure.Tests.Common;

public sealed class InMemoryAiProvider : IAiProvider
{
    private readonly Dictionary<string, string> _responses;

    public string ProviderName => "in-memory";
    public AiCapabilities Capabilities => AiCapabilities.JsonMode;

    public InMemoryAiProvider(Dictionary<string, string> responses)
    {
        _responses = responses;
    }

    public System.Threading.Tasks.Task<AiCompletion> CompleteAsync(AiPrompt prompt, CancellationToken ct = default)
    {
        // Return response based on prompt PromptId
        var response = _responses.TryGetValue(prompt.PromptId, out var r) ? r : "{}";
        return System.Threading.Tasks.Task.FromResult(new AiCompletion(response, 100, 50, "in-memory"));
    }
}
