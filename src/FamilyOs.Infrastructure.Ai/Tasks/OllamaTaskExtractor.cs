using System.Text.Json;
using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Infrastructure.Ai.Prompts;
using FamilyOs.Infrastructure.Ai.Providers;
using Microsoft.Extensions.Logging;

namespace FamilyOs.Infrastructure.Ai.Tasks;

public sealed class OllamaTaskExtractor : ITaskExtractor
{
    private readonly IAiProviderFactory _providerFactory;
    private readonly ILogger<OllamaTaskExtractor> _logger;

    private static readonly Action<ILogger, int, Exception?> LogTasksFound =
        LoggerMessage.Define<int>(LogLevel.Debug, new EventId(1, nameof(LogTasksFound)),
            "OllamaTaskExtractor: found {Count} tasks.");

    public OllamaTaskExtractor(IAiProviderFactory providerFactory, ILogger<OllamaTaskExtractor> logger)
    {
        _providerFactory = providerFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<TaskSuggestion>> ExtractAsync(
        string documentText,
        IReadOnlyList<string> familyMemberNames,
        CancellationToken ct = default)
    {
        var provider = _providerFactory.GetProvider();
        var template = PromptTemplate.Load(PromptCatalog.ExtractTasks);
        var truncated = documentText.Length > 12000 ? documentText[..12000] : documentText;
        var familyMembers = familyMemberNames.Count > 0
            ? string.Join(", ", familyMemberNames)
            : "(no family members)";

        var userPrompt = PromptTemplate.Replace(template, new Dictionary<string, string>
        {
            ["document"] = truncated,
            ["familyMembers"] = familyMembers,
        });

        var prompt = new AiPrompt(
            SystemPrompt: "You are a helpful assistant that extracts tasks from documents.",
            UserPrompt: userPrompt,
            PromptId: PromptCatalog.ExtractTasks,
            PromptVersion: PromptCatalog.GetVersion(PromptCatalog.ExtractTasks));

        var completion = await provider.CompleteAsync(prompt, ct);
        var results = ParseTasks(completion.Content);

        LogTasksFound(_logger, results.Count, null);
        return results;
    }

    private static List<TaskSuggestion> ParseTasks(string json)
    {
        var suggestions = new List<TaskSuggestion>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("tasks", out var tasksEl)
                || tasksEl.ValueKind != JsonValueKind.Array)
                return suggestions;

            foreach (var item in tasksEl.EnumerateArray())
            {
                var title = item.TryGetProperty("title", out var t) ? t.GetString() : null;
                var assignedToHint = item.TryGetProperty("assignedToHint", out var a) ? a.GetString() : null;
                var description = item.TryGetProperty("description", out var d) ? d.GetString() : null;
                var dueDateStr = item.TryGetProperty("dueDate", out var dd) ? dd.GetString() : null;

                if (string.IsNullOrWhiteSpace(title))
                    continue;

                DateOnly? dueDate = null;
                if (!string.IsNullOrWhiteSpace(dueDateStr) && dueDateStr != "null")
                {
                    if (DateOnly.TryParseExact(dueDateStr, "yyyy-MM-dd", out var parsed))
                        dueDate = parsed;
                }

                var hint = string.IsNullOrWhiteSpace(assignedToHint) ? null : assignedToHint;
                suggestions.Add(new TaskSuggestion(title, hint, dueDate, description));
            }
        }
        catch (JsonException)
        {
            // Return empty on parse failure
        }
        return suggestions;
    }
}
