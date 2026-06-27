using System.Globalization;
using System.Text;
using System.Text.Json;
using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Infrastructure.Ai.Prompts;
using FamilyOs.Infrastructure.Ai.Providers;
using Microsoft.Extensions.Logging;

namespace FamilyOs.Infrastructure.Ai.Tasks;

public sealed class OllamaQuestionAnswerer : IQuestionAnswerService
{
    private const string FallbackAnswer =
        "Nincs erre vonatkozó adat a rendelkezésre álló dokumentumokban.";

    private readonly IAiProviderFactory _providerFactory;
    private readonly ILogger<OllamaQuestionAnswerer> _logger;

    private static readonly Action<ILogger, string, Exception?> LogQaComplete =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(1, nameof(LogQaComplete)),
            "OllamaQuestionAnswerer: Q&A complete for question={Question}");

    private static readonly Action<ILogger, Exception?> LogQaParseError =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(2, nameof(LogQaParseError)),
            "OllamaQuestionAnswerer: failed to parse JSON answer, returning fallback.");

    public OllamaQuestionAnswerer(IAiProviderFactory providerFactory, ILogger<OllamaQuestionAnswerer> logger)
    {
        _providerFactory = providerFactory;
        _logger = logger;
    }

    public async Task<AnswerResult> AnswerAsync(
        string question,
        IReadOnlyList<(string chunkId, string content)> sources,
        CancellationToken ct = default)
    {
        var template = PromptTemplate.Load(PromptCatalog.QaMagyar);
        var sourcesText = FormatSources(sources);

        var userPrompt = PromptTemplate.Replace(template, new Dictionary<string, string>
        {
            ["question"] = question,
            ["sources"] = sourcesText,
        });

        var prompt = new AiPrompt(
            SystemPrompt: "You are a helpful Hungarian-language assistant that answers questions based strictly on provided document sources.",
            UserPrompt: userPrompt,
            PromptId: PromptCatalog.QaMagyar,
            PromptVersion: PromptCatalog.GetVersion(PromptCatalog.QaMagyar));

        var provider = _providerFactory.GetProvider();
        var completion = await provider.CompleteAsync(prompt, ct);

        LogQaComplete(_logger, question[..Math.Min(80, question.Length)], null);

        return ParseAnswer(completion.Content);
    }

    private static string FormatSources(IReadOnlyList<(string chunkId, string content)> sources)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < sources.Count; i++)
        {
            var (chunkId, content) = sources[i];
            sb.AppendLine(CultureInfo.InvariantCulture, $"[{i + 1}] (ID: {chunkId})");
            sb.AppendLine(content.Length > 500 ? content[..500] + "…" : content);
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private AnswerResult ParseAnswer(string json)
    {
        try
        {
            var trimmed = json.Trim();
            // Extract JSON object if wrapped in markdown code block
            var start = trimmed.IndexOf('{');
            var end = trimmed.LastIndexOf('}');
            if (start >= 0 && end > start)
                trimmed = trimmed[start..(end + 1)];

            using var doc = JsonDocument.Parse(trimmed);
            var root = doc.RootElement;

            var answer = root.TryGetProperty("answer", out var answerEl)
                ? answerEl.GetString() ?? FallbackAnswer
                : FallbackAnswer;

            var citedIds = Array.Empty<string>();
            if (root.TryGetProperty("citedSourceIds", out var citedEl) && citedEl.ValueKind == JsonValueKind.Array)
                citedIds = citedEl.EnumerateArray().Select(e => e.GetString() ?? string.Empty).ToArray();

            var confidence = root.TryGetProperty("confidence", out var confEl)
                ? confEl.GetDouble()
                : 0.5;

            return new AnswerResult(answer, citedIds, confidence);
        }
        catch (JsonException)
        {
            LogQaParseError(_logger, null);
            return new AnswerResult(FallbackAnswer, [], 0.0);
        }
    }
}
