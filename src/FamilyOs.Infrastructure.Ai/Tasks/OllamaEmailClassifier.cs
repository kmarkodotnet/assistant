using System.Text.Json;
using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Domain.Enums;
using FamilyOs.Infrastructure.Ai.Prompts;
using FamilyOs.Infrastructure.Ai.Providers;
using Microsoft.Extensions.Logging;

namespace FamilyOs.Infrastructure.Ai.Tasks;

public sealed class OllamaEmailClassifier : IEmailImportanceClassifier
{
    private const int MaxBodyChars = 8000;

    private readonly IAiProviderFactory _providerFactory;
    private readonly ILogger<OllamaEmailClassifier> _logger;

    private static readonly Action<ILogger, Exception?> LogClassifyDone =
        LoggerMessage.Define(LogLevel.Debug, new EventId(1, nameof(LogClassifyDone)),
            "OllamaEmailClassifier: classification complete.");

    private static readonly Action<ILogger, Exception?> LogParseFailed =
        LoggerMessage.Define(LogLevel.Warning, new EventId(2, nameof(LogParseFailed)),
            "OllamaEmailClassifier: failed to parse classifier response — defaulting to Low.");

    public OllamaEmailClassifier(IAiProviderFactory providerFactory, ILogger<OllamaEmailClassifier> logger)
    {
        _providerFactory = providerFactory;
        _logger = logger;
    }

    public async Task<EmailImportanceResult> ClassifyAsync(string subject, string? bodyText, CancellationToken ct = default)
    {
        var provider = _providerFactory.GetProvider();
        var template = PromptTemplate.Load(PromptCatalog.ClassifyEmail);

        var body = bodyText ?? string.Empty;
        var truncatedBody = body.Length > MaxBodyChars ? body[..MaxBodyChars] : body;

        var userPrompt = PromptTemplate.Replace(template, new Dictionary<string, string>
        {
            ["subject"] = subject,
            ["body"] = truncatedBody,
        });

        var prompt = new AiPrompt(
            SystemPrompt: "You are a helpful assistant that classifies emails for a family.",
            UserPrompt: userPrompt,
            PromptId: PromptCatalog.ClassifyEmail,
            PromptVersion: PromptCatalog.GetVersion(PromptCatalog.ClassifyEmail));

        var completion = await provider.CompleteAsync(prompt, ct);

        var result = ParseResult(completion.Content, _logger);
        LogClassifyDone(_logger, null);
        return result;
    }

    private static EmailImportanceResult ParseResult(string json, ILogger logger)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var importance = ParseImportance(root);

            string? category = null;
            if (root.TryGetProperty("category", out var categoryEl)
                && categoryEl.ValueKind == JsonValueKind.String)
            {
                var raw = categoryEl.GetString();
                category = string.IsNullOrWhiteSpace(raw) || raw is "null" ? null : raw;
            }

            var hasDeadline = root.TryGetProperty("hasDeadline", out var deadlineEl)
                               && deadlineEl.ValueKind == JsonValueKind.True;

            return new EmailImportanceResult(importance, category, hasDeadline);
        }
        catch (JsonException)
        {
            // Defensive default: never throw on a malformed AI response — a parse failure must
            // not generate a High-importance notification (noise), so Low is the safe fallback.
            LogParseFailed(logger, null);
            return new EmailImportanceResult(EmailImportance.Low, null, false);
        }
    }

    private static EmailImportance ParseImportance(JsonElement root)
    {
        if (root.TryGetProperty("importance", out var importanceEl)
            && importanceEl.ValueKind == JsonValueKind.String
            && Enum.TryParse<EmailImportance>(importanceEl.GetString(), ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        // Unknown / missing / malformed importance value — safe default, no noise notification.
        return EmailImportance.Low;
    }
}
