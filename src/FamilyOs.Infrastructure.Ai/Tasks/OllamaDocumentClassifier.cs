using System.Text.Json;
using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Infrastructure.Ai.Prompts;
using FamilyOs.Infrastructure.Ai.Providers;
using Microsoft.Extensions.Logging;

namespace FamilyOs.Infrastructure.Ai.Tasks;

public sealed class OllamaDocumentClassifier : IDocumentClassifier
{
    private readonly IAiProviderFactory _providerFactory;
    private readonly ILogger<OllamaDocumentClassifier> _logger;

    private static readonly Action<ILogger, Exception?> LogClassifyDone =
        LoggerMessage.Define(LogLevel.Debug, new EventId(1, nameof(LogClassifyDone)),
            "OllamaDocumentClassifier: classification complete.");

    public OllamaDocumentClassifier(IAiProviderFactory providerFactory, ILogger<OllamaDocumentClassifier> logger)
    {
        _providerFactory = providerFactory;
        _logger = logger;
    }

    public async Task<ClassificationResult> ClassifyAsync(string documentText, CancellationToken ct = default)
    {
        var provider = _providerFactory.GetProvider();
        var template = PromptTemplate.Load(PromptCatalog.Classify);
        var truncated = documentText.Length > 12000 ? documentText[..12000] : documentText;

        var userPrompt = PromptTemplate.Replace(template, new Dictionary<string, string>
        {
            ["document"] = truncated,
        });

        var prompt = new AiPrompt(
            SystemPrompt: "You are a helpful assistant that classifies documents.",
            UserPrompt: userPrompt,
            PromptId: PromptCatalog.Classify,
            PromptVersion: PromptCatalog.GetVersion(PromptCatalog.Classify));

        var completion = await provider.CompleteAsync(prompt, ct);

        LogClassifyDone(_logger, null);
        return ParseResult(completion.Content);
    }

    private static ClassificationResult ParseResult(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var topics = ParseStringArray(root, "topics");
            var tags = ParseStringArray(root, "tags");

            string? facetType = null;
            if (root.TryGetProperty("facetType", out var facetEl)
                && facetEl.ValueKind == JsonValueKind.String)
            {
                var raw = facetEl.GetString();
                facetType = raw is "null" or "" ? null : raw;
            }

            return new ClassificationResult(topics, tags, facetType);
        }
        catch (JsonException)
        {
            return new ClassificationResult([], [], null);
        }
    }

    private static string[] ParseStringArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var el) || el.ValueKind != JsonValueKind.Array)
            return [];

        var result = new List<string>();
        foreach (var item in el.EnumerateArray())
        {
            var val = item.GetString();
            if (!string.IsNullOrWhiteSpace(val))
                result.Add(val);
        }
        return [.. result];
    }
}
