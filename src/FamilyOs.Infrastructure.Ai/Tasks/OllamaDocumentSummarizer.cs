using System.Text;
using System.Text.Json;
using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Infrastructure.Ai.Prompts;
using FamilyOs.Infrastructure.Ai.Providers;
using Microsoft.Extensions.Logging;

namespace FamilyOs.Infrastructure.Ai.Tasks;

public sealed class OllamaDocumentSummarizer : IDocumentSummarizer
{
    private const int MaxSinglePassChars = 12000;
    private const int ChunkSize = 4000;

    private readonly IAiProviderFactory _providerFactory;
    private readonly ILogger<OllamaDocumentSummarizer> _logger;

    private static readonly Action<ILogger, int, Exception?> LogSummarizeChunked =
        LoggerMessage.Define<int>(LogLevel.Debug, new EventId(1, nameof(LogSummarizeChunked)),
            "OllamaDocumentSummarizer: text is long ({Chars} chars), using chunked summarization.");

    private static readonly Action<ILogger, Exception?> LogSummarizeDone =
        LoggerMessage.Define(LogLevel.Debug, new EventId(2, nameof(LogSummarizeDone)),
            "OllamaDocumentSummarizer: summarization complete.");

    public OllamaDocumentSummarizer(IAiProviderFactory providerFactory, ILogger<OllamaDocumentSummarizer> logger)
    {
        _providerFactory = providerFactory;
        _logger = logger;
    }

    public async Task<SummaryResult> SummarizeAsync(string documentText, string language, CancellationToken ct = default)
    {
        var provider = _providerFactory.GetProvider();
        var template = PromptTemplate.Load(PromptCatalog.Summarize);
        var promptVersion = PromptCatalog.GetVersion(PromptCatalog.Summarize);

        string finalSummary;
        string modelName;

        if (documentText.Length <= MaxSinglePassChars)
        {
            // Single-pass summarization
            var truncated = documentText[..Math.Min(documentText.Length, MaxSinglePassChars)];
            var prompt = BuildPrompt(template, truncated);
            var completion = await provider.CompleteAsync(prompt, ct);
            modelName = completion.ModelName;
            finalSummary = ParseSummary(completion.Content);
        }
        else
        {
            // Chunked approach for long documents
            LogSummarizeChunked(_logger, documentText.Length, null);
            var chunks = SplitIntoChunks(documentText);
            var chunkSummaries = new List<string>();

            foreach (var chunk in chunks)
            {
                ct.ThrowIfCancellationRequested();
                var prompt = BuildPrompt(template, chunk);
                var completion = await provider.CompleteAsync(prompt, ct);
                chunkSummaries.Add(ParseSummary(completion.Content));
            }

            // Meta-summarize
            var combined = string.Join("\n\n", chunkSummaries);
            var metaPrompt = BuildPrompt(template, combined);
            var metaCompletion = await provider.CompleteAsync(metaPrompt, ct);
            modelName = metaCompletion.ModelName;
            finalSummary = ParseSummary(metaCompletion.Content);
        }

        LogSummarizeDone(_logger, null);
        return new SummaryResult(finalSummary, modelName, promptVersion);
    }

    private static AiPrompt BuildPrompt(string template, string text)
    {
        var userPrompt = PromptTemplate.Replace(template, new Dictionary<string, string>
        {
            ["document"] = text,
        });
        return new AiPrompt(
            SystemPrompt: "You are a helpful assistant that summarizes documents.",
            UserPrompt: userPrompt,
            PromptId: PromptCatalog.Summarize,
            PromptVersion: PromptCatalog.GetVersion(PromptCatalog.Summarize));
    }

    private static string ParseSummary(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("summary", out var summaryEl))
                return summaryEl.GetString() ?? string.Empty;
        }
        catch (JsonException)
        {
            // If JSON parsing fails, return the raw content as the summary
            return json.Trim();
        }
        return json.Trim();
    }

    private static List<string> SplitIntoChunks(string text)
    {
        var chunks = new List<string>();
        var sb = new StringBuilder();
        var offset = 0;

        while (offset < text.Length)
        {
            var length = Math.Min(ChunkSize, text.Length - offset);
            sb.Clear();
            sb.Append(text, offset, length);
            chunks.Add(sb.ToString());
            offset += length;
        }

        return chunks;
    }
}
