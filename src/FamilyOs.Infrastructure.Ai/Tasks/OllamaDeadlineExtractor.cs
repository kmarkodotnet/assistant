using System.Text.Json;
using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Infrastructure.Ai.Prompts;
using FamilyOs.Infrastructure.Ai.Providers;
using Microsoft.Extensions.Logging;

namespace FamilyOs.Infrastructure.Ai.Tasks;

public sealed class OllamaDeadlineExtractor : IDeadlineExtractor
{
    private readonly IAiProviderFactory _providerFactory;
    private readonly ILogger<OllamaDeadlineExtractor> _logger;

    private static readonly Action<ILogger, int, Exception?> LogDeadlinesFound =
        LoggerMessage.Define<int>(LogLevel.Debug, new EventId(1, nameof(LogDeadlinesFound)),
            "OllamaDeadlineExtractor: found {Count} future deadlines.");

    public OllamaDeadlineExtractor(IAiProviderFactory providerFactory, ILogger<OllamaDeadlineExtractor> logger)
    {
        _providerFactory = providerFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DeadlineSuggestion>> ExtractAsync(
        string documentText, DateOnly today, CancellationToken ct = default)
    {
        var provider = _providerFactory.GetProvider();
        var template = PromptTemplate.Load(PromptCatalog.ExtractDeadlines);
        var truncated = documentText.Length > 12000 ? documentText[..12000] : documentText;

        var userPrompt = PromptTemplate.Replace(template, new Dictionary<string, string>
        {
            ["document"] = truncated,
            ["today"] = today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
        });

        var prompt = new AiPrompt(
            SystemPrompt: "You are a helpful assistant that extracts deadlines from documents.",
            UserPrompt: userPrompt,
            PromptId: PromptCatalog.ExtractDeadlines,
            PromptVersion: PromptCatalog.GetVersion(PromptCatalog.ExtractDeadlines));

        var completion = await provider.CompleteAsync(prompt, ct);
        var results = ParseDeadlines(completion.Content, today);

        LogDeadlinesFound(_logger, results.Count, null);
        return results;
    }

    private static List<DeadlineSuggestion> ParseDeadlines(string json, DateOnly today)
    {
        var suggestions = new List<DeadlineSuggestion>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("deadlines", out var deadlinesEl)
                || deadlinesEl.ValueKind != JsonValueKind.Array)
                return suggestions;

            foreach (var item in deadlinesEl.EnumerateArray())
            {
                var title = item.TryGetProperty("title", out var t) ? t.GetString() : null;
                var dueDateStr = item.TryGetProperty("dueDate", out var d) ? d.GetString() : null;
                var description = item.TryGetProperty("description", out var desc) ? desc.GetString() : null;

                if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(dueDateStr))
                    continue;

                if (!DateOnly.TryParseExact(dueDateStr, "yyyy-MM-dd", out var dueDate))
                    continue;

                // Only future dates
                if (dueDate < today)
                    continue;

                suggestions.Add(new DeadlineSuggestion(title, dueDate, description));
            }
        }
        catch (JsonException)
        {
            // Return empty on parse failure
        }
        return suggestions;
    }
}
