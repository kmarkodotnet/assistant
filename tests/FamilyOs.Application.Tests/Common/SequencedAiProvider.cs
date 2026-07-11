using FamilyOs.Application.Abstractions.Ai;

namespace FamilyOs.Application.Tests.Common;

/// <summary>
/// Returns a fixed sequence of raw completions, one per call (last response repeats once
/// exhausted) — used to exercise ToolCallPlanner's single-retry logic deterministically.
/// </summary>
public sealed class SequencedAiProvider(params string[] responses) : IAiProvider
{
    private int _index;

    public string ProviderName => "sequenced-test-stub";
    public AiCapabilities Capabilities => AiCapabilities.JsonMode;
    public int CallCount { get; private set; }

    public Task<AiCompletion> CompleteAsync(AiPrompt prompt, CancellationToken ct = default)
    {
        CallCount++;
        var response = responses[Math.Min(_index, responses.Length - 1)];
        _index++;
        return Task.FromResult(new AiCompletion(response, 10, 10, "sequenced-test-stub"));
    }
}
