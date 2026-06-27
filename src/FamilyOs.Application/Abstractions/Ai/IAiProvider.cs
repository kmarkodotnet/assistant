namespace FamilyOs.Application.Abstractions.Ai;

public interface IAiProvider
{
    string ProviderName { get; }
    AiCapabilities Capabilities { get; }
    Task<AiCompletion> CompleteAsync(AiPrompt prompt, CancellationToken ct = default);
}
