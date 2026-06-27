namespace FamilyOs.Infrastructure.Ai.Options;

public sealed class OllamaOptions
{
    public const string Section = "Ai:Ollama";
    public string BaseUrl { get; set; } = "http://ollama:11434";
    public string DefaultModel { get; set; } = "llama3.2:3b";
    public int TimeoutSeconds { get; set; } = 120;
}
