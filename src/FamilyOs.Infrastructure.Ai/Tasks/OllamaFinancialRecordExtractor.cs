using System.Text.Json;
using FamilyOs.Application.Abstractions.Ai;
using FamilyOs.Infrastructure.Ai.Prompts;
using FamilyOs.Infrastructure.Ai.Providers;
using Microsoft.Extensions.Logging;

namespace FamilyOs.Infrastructure.Ai.Tasks;

public sealed class OllamaFinancialRecordExtractor : IFinancialRecordExtractor
{
    private readonly IAiProviderFactory _providerFactory;
    private readonly ILogger<OllamaFinancialRecordExtractor> _logger;

    private static readonly Action<ILogger, Exception?> LogExtractDone =
        LoggerMessage.Define(LogLevel.Debug, new EventId(1, nameof(LogExtractDone)),
            "OllamaFinancialRecordExtractor: extraction complete.");

    public OllamaFinancialRecordExtractor(IAiProviderFactory providerFactory, ILogger<OllamaFinancialRecordExtractor> logger)
    {
        _providerFactory = providerFactory;
        _logger = logger;
    }

    public async Task<FinancialRecordExtraction?> ExtractAsync(string text, CancellationToken ct = default)
    {
        var provider = _providerFactory.GetProvider();
        var template = PromptTemplate.Load(PromptCatalog.ExtractFinancial);
        var truncated = text.Length > 12000 ? text[..12000] : text;

        var userPrompt = PromptTemplate.Replace(template, new Dictionary<string, string>
        {
            ["document"] = truncated,
        });

        var prompt = new AiPrompt(
            SystemPrompt: "You are a helpful assistant that extracts financial information from documents.",
            UserPrompt: userPrompt,
            PromptId: PromptCatalog.ExtractFinancial,
            PromptVersion: PromptCatalog.GetVersion(PromptCatalog.ExtractFinancial));

        var completion = await provider.CompleteAsync(prompt, ct);

        LogExtractDone(_logger, null);
        return ParseResult(completion.Content);
    }

    private static FinancialRecordExtraction? ParseResult(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            decimal? amount = null;
            if (root.TryGetProperty("amount", out var amEl) && amEl.ValueKind == JsonValueKind.Number)
                amount = amEl.GetDecimal();

            var currency = root.TryGetProperty("currency", out var cur) ? cur.GetString() : null;
            var recordDateStr = root.TryGetProperty("recordDate", out var rd) ? rd.GetString() : null;
            var isPaid = root.TryGetProperty("isPaid", out var ip) && ip.ValueKind == JsonValueKind.True;
            var recurrencePeriod = root.TryGetProperty("recurrencePeriod", out var rp) ? rp.GetString() : null;
            var notes = root.TryGetProperty("notes", out var n) ? n.GetString() : null;

            DateOnly? recordDate = null;
            if (!string.IsNullOrWhiteSpace(recordDateStr) && recordDateStr != "null")
            {
                if (DateOnly.TryParseExact(recordDateStr, "yyyy-MM-dd", out var parsed))
                    recordDate = parsed;
            }

            if (recurrencePeriod is "null" or "")
                recurrencePeriod = null;

            return new FinancialRecordExtraction(amount, currency, recordDate, isPaid, recurrencePeriod, notes);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
